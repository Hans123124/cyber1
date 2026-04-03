namespace CyberServer.Domain;

public class MapLayout
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ClubId { get; set; }
    public Club Club { get; set; } = null!;
    public string Name { get; set; } = "Main Hall";
    public int GridWidth { get; set; } = 30;
    public int GridHeight { get; set; } = 20;
    public int GridCellSizePx { get; set; } = 40;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<MapItem> Items { get; set; } = [];
    public ICollection<Zone> Zones { get; set; } = [];
}
