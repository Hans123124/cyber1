namespace CyberServer.Domain;

/// <summary>
/// Represents a floor-plan/grid layout for the club.
/// A club may have multiple layouts (e.g. per room / per floor).
/// </summary>
public class MapLayout
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "Main Hall";

    /// <summary>Grid canvas width in pixels.</summary>
    public int Width { get; set; } = 1200;

    /// <summary>Grid canvas height in pixels.</summary>
    public int Height { get; set; } = 800;

    /// <summary>Grid cell size in pixels (e.g. 40 = 40×40 px per cell).</summary>
    public int GridSize { get; set; } = 40;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public List<MapItem> Items { get; set; } = [];
    public List<Zone> Zones { get; set; } = [];
}
