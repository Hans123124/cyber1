namespace CyberServer.Domain;

public class Session
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid WorkstationId { get; set; }
    public Workstation Workstation { get; set; } = null!;
    public Guid? CustomerId { get; set; }
    public Customer? Customer { get; set; }
    public Guid TariffPlanId { get; set; }
    public TariffPlan TariffPlan { get; set; } = null!;
    public Guid SaleId { get; set; }
    public Sale Sale { get; set; } = null!;
    public string? GuestName { get; set; }
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime EndsAt { get; set; }
    public SessionStatus Status { get; set; } = SessionStatus.Active;
    public DateTime? EndedAt { get; set; }
}

public enum SessionStatus
{
    Active,
    Ended,
    Cancelled
}
