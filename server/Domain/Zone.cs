namespace CyberServer.Domain;

public class Zone
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid LayoutId { get; set; }
    public MapLayout Layout { get; set; } = null!;

    public string Name { get; set; } = string.Empty;
    public string Color { get; set; } = "#4A90D9";
    public int X { get; set; }
    public int Y { get; set; }
    public int W { get; set; } = 4;
    public int H { get; set; } = 3;

    public ICollection<MapItem> Items { get; set; } = [];
}
