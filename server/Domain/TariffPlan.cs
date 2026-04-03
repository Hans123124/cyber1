namespace CyberServer.Domain;

public class TariffPlan
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public TariffType Type { get; set; }
    /// <summary>Duration in minutes for Hourly plans (e.g. 60 = 1 hour).</summary>
    public int? DurationMinutes { get; set; }
    /// <summary>Duration in days for Monthly plans (e.g. 30 = 1 month).</summary>
    public int? DurationDays { get; set; }
    public decimal Price { get; set; }
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public enum TariffType
{
    Hourly,
    Monthly
}
