namespace CyberServer.Domain;

/// <summary>
/// A named area/region drawn on the map (VIP zone, gaming zone, etc.).
/// Stored as a rectangle for MVP; MetaJson holds polygon data for future use.
/// </summary>
public class Zone
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid LayoutId { get; set; }
    public MapLayout Layout { get; set; } = null!;

    public string Name { get; set; } = "Zone";

    /// <summary>CSS/hex color, e.g. "#3b82f6" or "rgba(59,130,246,0.3)".</summary>
    public string Color { get; set; } = "#3b82f6";

    /// <summary>Left edge in grid units.</summary>
    public int X { get; set; }

    /// <summary>Top edge in grid units.</summary>
    public int Y { get; set; }

    /// <summary>Width in grid units.</summary>
    public int W { get; set; } = 4;

    /// <summary>Height in grid units.</summary>
    public int H { get; set; } = 4;

    /// <summary>Optional polygon / extra data (JSON).</summary>
    public string? MetaJson { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
