namespace CyberServer.Domain;

public class CommandLog
{
    public long Id { get; set; }
    public Guid WorkstationId { get; set; }
    public Workstation Workstation { get; set; } = null!;
    public CommandType Command { get; set; }
    public string IssuedBy { get; set; } = "admin";
    public CommandStatus Status { get; set; } = CommandStatus.Pending;
    public string? Notes { get; set; }
    public DateTime IssuedAt { get; set; } = DateTime.UtcNow;
    public DateTime? DeliveredAt { get; set; }
}

public enum CommandType
{
    Lock,
    Unlock,
    Reboot,
    Shutdown,
    Message
}

public enum CommandStatus
{
    Pending,
    Delivered,
    Failed
}
