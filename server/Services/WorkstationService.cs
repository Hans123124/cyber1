using CyberServer.Data;
using CyberServer.Domain;
using Microsoft.EntityFrameworkCore;

namespace CyberServer.Services;

public interface IWorkstationService
{
    Task<(Workstation workstation, string plainSecret)> RegisterAsync(
        string machineFingerprint, string name, string? agentVersion, string? osVersion, string? ipAddress, CancellationToken ct = default);

    Task<Workstation?> AuthenticateAsync(Guid workstationId, string secret, CancellationToken ct = default);

    Task HeartbeatAsync(Workstation workstation, string agentVersion, WorkstationState state,
        double cpuUsage, double ramUsageMb, string ipAddress, CancellationToken ct = default);

    Task<IReadOnlyList<Workstation>> GetAllAsync(CancellationToken ct = default);
    Task<Workstation?> GetByIdAsync(Guid id, CancellationToken ct = default);
}

public class WorkstationService(AppDbContext db) : IWorkstationService
{
    public async Task<(Workstation workstation, string plainSecret)> RegisterAsync(
        string machineFingerprint, string name, string? agentVersion, string? osVersion, string? ipAddress, CancellationToken ct = default)
    {
        var existing = await db.Workstations
            .FirstOrDefaultAsync(w => w.MachineFingerprint == machineFingerprint, ct);

        if (existing is not null)
        {
            // Re-issue a new secret on re-registration
            var newSecret = GenerateSecret();
            existing.SecretHash = BCrypt.Net.BCrypt.HashPassword(newSecret);
            existing.Name = name;
            existing.AgentVersion = agentVersion ?? existing.AgentVersion;
            existing.OsVersion = osVersion ?? existing.OsVersion;
            existing.IpAddress = ipAddress ?? existing.IpAddress;
            existing.LastSeenAt = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);
            return (existing, newSecret);
        }

        var secret = GenerateSecret();
        var workstation = new Workstation
        {
            Name = name,
            MachineFingerprint = machineFingerprint,
            SecretHash = BCrypt.Net.BCrypt.HashPassword(secret),
            AgentVersion = agentVersion ?? string.Empty,
            OsVersion = osVersion ?? string.Empty,
            IpAddress = ipAddress ?? string.Empty
        };

        db.Workstations.Add(workstation);
        await db.SaveChangesAsync(ct);
        return (workstation, secret);
    }

    public async Task<Workstation?> AuthenticateAsync(Guid workstationId, string secret, CancellationToken ct = default)
    {
        var workstation = await db.Workstations.FindAsync([workstationId], ct);
        if (workstation is null) return null;
        if (!BCrypt.Net.BCrypt.Verify(secret, workstation.SecretHash)) return null;
        return workstation;
    }

    public async Task HeartbeatAsync(Workstation workstation, string agentVersion, WorkstationState state,
        double cpuUsage, double ramUsageMb, string ipAddress, CancellationToken ct = default)
    {
        workstation.LastSeenAt = DateTime.UtcNow;
        workstation.AgentVersion = agentVersion;
        workstation.State = state;
        workstation.IpAddress = ipAddress;

        db.AgentHeartbeats.Add(new AgentHeartbeat
        {
            WorkstationId = workstation.Id,
            AgentVersion = agentVersion,
            State = state,
            CpuUsage = cpuUsage,
            RamUsageMb = ramUsageMb,
            IpAddress = ipAddress
        });

        await db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<Workstation>> GetAllAsync(CancellationToken ct = default)
        => await db.Workstations.OrderBy(w => w.Name).ToListAsync(ct);

    public async Task<Workstation?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await db.Workstations.FindAsync([id], ct);

    private static string GenerateSecret()
        => Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));
}
