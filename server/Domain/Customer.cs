namespace CyberServer.Domain;

public class Customer
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string? Username { get; set; }
    public string? Phone { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;

    public ICollection<Session> Sessions { get; set; } = [];
    public ICollection<Subscription> Subscriptions { get; set; } = [];
}
