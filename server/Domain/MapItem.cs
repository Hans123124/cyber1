namespace CyberServer.Domain;

public class MapItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid LayoutId { get; set; }
    public MapLayout Layout { get; set; } = null!;

    public MapItemType Type { get; set; } = MapItemType.Pc;
    public int X { get; set; }
    public int Y { get; set; }
    public int W { get; set; } = 1;
    public int H { get; set; } = 1;
    public int Rotation { get; set; } // 0, 90, 180, 270

    public string? Label { get; set; }
    public Guid? WorkstationId { get; set; }
    public Workstation? Workstation { get; set; }
    public Guid? ZoneId { get; set; }
    public Zone? Zone { get; set; }
    public string? MetaJson { get; set; }
}

public enum MapItemType
{
    Pc,
    Console,
    Wall,
    Corner,
    Triangle,
    WallT,
    Reception,
    Bar,
    Sofa,
    EntranceExit,
    Other
}
