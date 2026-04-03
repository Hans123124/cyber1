using CyberServer.Data;
using CyberServer.Domain;
using CyberServer.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using CyberServer.Hubs;

namespace CyberServer.Services;

public interface ISessionService
{
    Task<Session> StartAsync(StartSessionRequest request, CancellationToken ct = default);
    Task<Session> ExtendAsync(Guid sessionId, ExtendSessionRequest request, CancellationToken ct = default);
    Task EndAsync(Guid sessionId, bool reboot, string issuedBy, CancellationToken ct = default);
    Task<IReadOnlyList<Session>> GetAsync(Guid? workstationId = null, DateOnly? date = null, CancellationToken ct = default);
    Task<Session?> GetByIdAsync(Guid id, CancellationToken ct = default);
}

public class SessionService(
    AppDbContext db,
    ICommandService commandService,
    IHubContext<AgentHub> hub) : ISessionService
{
    public async Task<Session> StartAsync(StartSessionRequest request, CancellationToken ct = default)
    {
        if (request.DurationHours <= 0)
            throw new InvalidOperationException("DurationHours must be a positive whole number.");

        var tariff = await db.TariffPlans.FindAsync([request.TariffPlanId], ct)
            ?? throw new InvalidOperationException("Tariff plan not found.");

        if (!tariff.IsActive)
            throw new InvalidOperationException("Tariff plan is not active.");

        if (tariff.Type == TariffType.Hourly && tariff.HourlyRateMdl <= 0)
            throw new InvalidOperationException("Tariff plan has no hourly rate configured.");

        // Ensure no active session is already running on this workstation
        var existing = await db.Sessions
            .Where(s => s.WorkstationId == request.WorkstationId && s.Status == SessionStatus.Active)
            .FirstOrDefaultAsync(ct);
        if (existing is not null)
            throw new InvalidOperationException("Workstation already has an active session.");

        // Compute amount strictly as hours × hourly rate (MDL)
        var amount = tariff.Type == TariffType.Hourly
            ? request.DurationHours * tariff.HourlyRateMdl
            : tariff.Price;

        // Create payment record
        var sale = new Sale
        {
            Amount = amount,
            Currency = "MDL",
            Method = request.PaymentMethod,
            OperatorName = request.OperatorName
        };
        db.Sales.Add(sale);

        // Calculate session end time based on whole hours
        var endsAt = tariff.Type == TariffType.Hourly
            ? DateTime.UtcNow.AddHours(request.DurationHours)
            : CalculateEndsAt(tariff, DateTime.UtcNow);

        // Create session
        var session = new Session
        {
            WorkstationId = request.WorkstationId,
            CustomerId = request.CustomerId,
            TariffPlanId = request.TariffPlanId,
            Sale = sale,
            GuestName = request.GuestName,
            StartedAt = DateTime.UtcNow,
            EndsAt = endsAt,
            Status = SessionStatus.Active
        };
        db.Sessions.Add(session);
        await db.SaveChangesAsync(ct);

        // Send unlock command to agent + session started notification
        await commandService.SendCommandAsync(
            request.WorkstationId,
            CommandType.Unlock,
            request.OperatorName,
            notes: $"SessionStarted:{session.Id}:{endsAt:O}",
            ct: ct);

        await hub.Clients
            .Group(request.WorkstationId.ToString())
            .SendAsync("ReceiveSessionUpdate", new
            {
                Type = "SessionStarted",
                SessionId = session.Id,
                EndsAt = endsAt
            }, ct);

        return session;
    }

    public async Task<Session> ExtendAsync(Guid sessionId, ExtendSessionRequest request, CancellationToken ct = default)
    {
        if (request.DurationHours <= 0)
            throw new InvalidOperationException("DurationHours must be a positive whole number.");

        var session = await db.Sessions
            .Include(s => s.TariffPlan)
            .FirstOrDefaultAsync(s => s.Id == sessionId, ct)
            ?? throw new InvalidOperationException("Session not found.");

        if (session.Status != SessionStatus.Active)
            throw new InvalidOperationException("Session is not active.");

        var tariff = await db.TariffPlans.FindAsync([request.TariffPlanId], ct)
            ?? throw new InvalidOperationException("Tariff plan not found.");

        if (tariff.Type == TariffType.Hourly && tariff.HourlyRateMdl <= 0)
            throw new InvalidOperationException("Tariff plan has no hourly rate configured.");

        // Compute amount strictly as hours × hourly rate (MDL)
        var amount = tariff.Type == TariffType.Hourly
            ? request.DurationHours * tariff.HourlyRateMdl
            : tariff.Price;

        // Create new sale for the extension
        var sale = new Sale
        {
            Amount = amount,
            Currency = "MDL",
            Method = request.PaymentMethod,
            OperatorName = request.OperatorName
        };
        db.Sales.Add(sale);

        // Extend from current EndsAt by whole hours
        var newEndsAt = tariff.Type == TariffType.Hourly
            ? session.EndsAt.AddHours(request.DurationHours)
            : CalculateEndsAt(tariff, session.EndsAt);
        session.EndsAt = newEndsAt;
        session.TariffPlanId = tariff.Id;

        await db.SaveChangesAsync(ct);

        // Notify agent of updated end time
        await hub.Clients
            .Group(session.WorkstationId.ToString())
            .SendAsync("ReceiveSessionUpdate", new
            {
                Type = "SessionExtended",
                SessionId = session.Id,
                EndsAt = newEndsAt
            }, ct);

        return session;
    }

    public async Task EndAsync(Guid sessionId, bool reboot, string issuedBy, CancellationToken ct = default)
    {
        var session = await db.Sessions.FindAsync([sessionId], ct)
            ?? throw new InvalidOperationException("Session not found.");

        if (session.Status != SessionStatus.Active)
            throw new InvalidOperationException("Session is not active.");

        session.Status = SessionStatus.Ended;
        session.EndedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        // Notify agent session ended
        await hub.Clients
            .Group(session.WorkstationId.ToString())
            .SendAsync("ReceiveSessionUpdate", new
            {
                Type = "SessionEnded",
                SessionId = session.Id
            }, ct);

        // Lock first, then optionally reboot
        await commandService.SendCommandAsync(session.WorkstationId, CommandType.Lock, issuedBy, ct: ct);

        if (reboot)
            await commandService.SendCommandAsync(session.WorkstationId, CommandType.Reboot, issuedBy, ct: ct);
    }

    public async Task<IReadOnlyList<Session>> GetAsync(Guid? workstationId = null, DateOnly? date = null, CancellationToken ct = default)
    {
        var q = db.Sessions
            .Include(s => s.TariffPlan)
            .Include(s => s.Sale)
            .AsQueryable();

        if (workstationId.HasValue)
            q = q.Where(s => s.WorkstationId == workstationId.Value);

        if (date.HasValue)
        {
            var start = date.Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            var end = start.AddDays(1);
            q = q.Where(s => s.StartedAt >= start && s.StartedAt < end);
        }

        return await q.OrderByDescending(s => s.StartedAt).ToListAsync(ct);
    }

    public Task<Session?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        db.Sessions
            .Include(s => s.TariffPlan)
            .Include(s => s.Sale)
            .FirstOrDefaultAsync(s => s.Id == id, ct);

    private static DateTime CalculateEndsAt(TariffPlan tariff, DateTime from) =>
        tariff.Type switch
        {
            TariffType.Hourly => from.AddMinutes(tariff.DurationMinutes ?? 60),
            TariffType.Monthly => from.AddDays(tariff.DurationDays ?? 30),
            _ => throw new InvalidOperationException($"Unsupported tariff type: {tariff.Type}")
        };
}
