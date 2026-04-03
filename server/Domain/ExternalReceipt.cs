namespace CyberServer.Domain;

public class ExternalReceipt
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Source { get; set; } = string.Empty;
    public string ReceiptNo { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "KZT";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Nullable link to a Core session or workstation context.</summary>
    public Guid? WorkstationId { get; set; }

    /// <summary>External session reference (string placeholder for future session tracking).</summary>
    public string? SessionId { get; set; }

    /// <summary>Raw JSON payload from the external system.</summary>
    public string? RawJson { get; set; }
}
