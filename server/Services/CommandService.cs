using CyberServer.Data;
using CyberServer.Domain;
using CyberServer.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace CyberServer.Services;

public interface ICommandService
{
    Task SendCommandAsync(Guid workstationId, CommandType command, string issuedBy, string? notes = null, CancellationToken ct = default);
    Task<IReadOnlyList<CommandLog>> GetLogsAsync(Guid? workstationId = null, int take = 100, CancellationToken ct = default);
    Task MarkDeliveredAsync(long commandLogId, CancellationToken ct = default);
}

public class CommandService(AppDbContext db, IHubContext<AgentHub> hub) : ICommandService
{
    public async Task SendCommandAsync(Guid workstationId, CommandType command, string issuedBy, string? notes = null, CancellationToken ct = default)
    {
        var log = new CommandLog
        {
            WorkstationId = workstationId,
            Command = command,
            IssuedBy = issuedBy,
            Notes = notes
        };
        db.CommandLogs.Add(log);
        await db.SaveChangesAsync(ct);

        await hub.Clients
            .Group(workstationId.ToString())
            .SendAsync("ReceiveCommand", new
            {
                CommandLogId = log.Id,
                Command = command.ToString(),
                Notes = notes
            }, ct);
    }

    public async Task<IReadOnlyList<CommandLog>> GetLogsAsync(Guid? workstationId = null, int take = 100, CancellationToken ct = default)
    {
        var q = db.CommandLogs.AsQueryable();
        if (workstationId.HasValue)
            q = q.Where(c => c.WorkstationId == workstationId.Value);
        return await q.OrderByDescending(c => c.IssuedAt).Take(take).ToListAsync(ct);
    }

    public async Task MarkDeliveredAsync(long commandLogId, CancellationToken ct = default)
    {
        var log = await db.CommandLogs.FindAsync([commandLogId], ct);
        if (log is not null)
        {
            log.Status = CommandStatus.Delivered;
            log.DeliveredAt = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);
        }
    }
}
