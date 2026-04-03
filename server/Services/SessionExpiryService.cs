using CyberServer.Data;
using CyberServer.Domain;
using CyberServer.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace CyberServer.Services;

/// <summary>
/// Background service that scans for expired sessions and issues Reboot
/// to the workstation, then marks the session as Ended.
/// </summary>
public class SessionExpiryService(
    IServiceScopeFactory scopeFactory,
    ILogger<SessionExpiryService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(30));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await CheckExpiredSessionsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error checking expired sessions.");
            }
        }
    }

    private async Task CheckExpiredSessionsAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var commandService = scope.ServiceProvider.GetRequiredService<ICommandService>();
        var hub = scope.ServiceProvider.GetRequiredService<IHubContext<AgentHub>>();

        var now = DateTime.UtcNow;
        var expiredSessions = await db.Sessions
            .Where(s => s.Status == SessionStatus.Active && s.EndsAt <= now)
            .ToListAsync(ct);

        foreach (var session in expiredSessions)
        {
            logger.LogInformation(
                "Session {SessionId} on workstation {WorkstationId} expired. Ending and rebooting.",
                session.Id, session.WorkstationId);

            session.Status = SessionStatus.Ended;
            session.EndedAt = now;

            // Notify agent
            await hub.Clients
                .Group(session.WorkstationId.ToString())
                .SendAsync("ReceiveSessionUpdate", new
                {
                    Type = "SessionEnded",
                    SessionId = session.Id
                }, ct);

            // Lock + Reboot
            await commandService.SendCommandAsync(
                session.WorkstationId, CommandType.Lock, "system", ct: ct);
            await commandService.SendCommandAsync(
                session.WorkstationId, CommandType.Reboot, "system",
                notes: $"Auto-reboot after session {session.Id} expired.", ct: ct);
        }

        if (expiredSessions.Count > 0)
            await db.SaveChangesAsync(ct);
    }
}
