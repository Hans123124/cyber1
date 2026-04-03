namespace CyberServer.Domain;

public class Club
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ClubSettings? Settings { get; set; }
    public ICollection<MapLayout> Layouts { get; set; } = [];
    public ICollection<Workstation> Workstations { get; set; } = [];
}
