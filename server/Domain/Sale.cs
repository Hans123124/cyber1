namespace CyberServer.Domain;

public class Sale
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "MDL";
    public PaymentMethod Method { get; set; } = PaymentMethod.Cash;
    public string? OperatorName { get; set; }

    public ICollection<Session> Sessions { get; set; } = [];
}

public enum PaymentMethod
{
    Cash,
    Card,
    Other
}
