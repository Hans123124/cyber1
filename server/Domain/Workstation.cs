namespace CyberServer.Domain;

public class Workstation
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string MachineFingerprint { get; set; } = string.Empty;
    public string SecretHash { get; set; } = string.Empty;
    public string AgentVersion { get; set; } = string.Empty;
    public string OsVersion { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public WorkstationState State { get; set; } = WorkstationState.Locked;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastSeenAt { get; set; } = DateTime.UtcNow;

    // Club assignment
    public Guid? ClubId { get; set; }
    public Club? Club { get; set; }

    // Integration fields
    public string? MeshCentralDeviceId { get; set; }
    public string? FogHostId { get; set; }
    public string? ImageGroup { get; set; }

    public bool IsOnline =>
        (DateTime.UtcNow - LastSeenAt).TotalSeconds < 60;

    public ICollection<AgentHeartbeat> Heartbeats { get; set; } = [];
    public ICollection<CommandLog> Commands { get; set; } = [];
}

public enum WorkstationState
{
    Locked,
    Unlocked,
    Maintenance
}
