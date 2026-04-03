namespace CyberServer.Domain;

public class AgentHeartbeat
{
    public long Id { get; set; }
    public Guid WorkstationId { get; set; }
    public Workstation Workstation { get; set; } = null!;
    public string AgentVersion { get; set; } = string.Empty;
    public WorkstationState State { get; set; }
    public double CpuUsage { get; set; }
    public double RamUsageMb { get; set; }
    public string IpAddress { get; set; } = string.Empty;
    public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;
}
