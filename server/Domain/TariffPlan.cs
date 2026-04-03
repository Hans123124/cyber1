namespace CyberServer.Domain;

public class TariffPlan
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? ClubId { get; set; }
    public Club? Club { get; set; }
    public string Name { get; set; } = string.Empty;
    public TariffType Type { get; set; }
    /// <summary>Hourly rate in MDL. Used for Hourly plans to compute price as Hours × HourlyRateMdl.</summary>
    public decimal HourlyRateMdl { get; set; }
    /// <summary>Duration in minutes for Hourly plans (e.g. 60 = 1 hour). Must be a multiple of 60.</summary>
    public int? DurationMinutes { get; set; }
    /// <summary>Duration in days for Monthly plans (e.g. 30 = 1 month).</summary>
    public int? DurationDays { get; set; }
    /// <summary>Legacy price field kept for backward compatibility. For Hourly plans, use HourlyRateMdl instead.</summary>
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
