namespace CyberServer.Domain;

/// <summary>
/// A single element placed on a MapLayout (PC, console, wall, etc.).
/// </summary>
public class MapItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid LayoutId { get; set; }
    public MapLayout Layout { get; set; } = null!;

    /// <summary>
    /// Element type token: Pc | Console | Wall | Corner | WallT | Triangle | Decoration.
    /// </summary>
    public string Type { get; set; } = "Pc";

    /// <summary>Column index (grid units).</summary>
    public int X { get; set; }

    /// <summary>Row index (grid units).</summary>
    public int Y { get; set; }

    /// <summary>Width in grid units.</summary>
    public int W { get; set; } = 1;

    /// <summary>Height in grid units.</summary>
    public int H { get; set; } = 1;

    /// <summary>Rotation in degrees (0 / 90 / 180 / 270).</summary>
    public int Rotation { get; set; } = 0;

    /// <summary>Display label (e.g. "PC-01").</summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>Linked workstation (nullable — non-PC elements have no workstation).</summary>
    public Guid? WorkstationId { get; set; }

    /// <summary>Zone this item belongs to (nullable).</summary>
    public Guid? ZoneId { get; set; }

    /// <summary>Free-form JSON for future extensibility.</summary>
    public string? MetaJson { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
