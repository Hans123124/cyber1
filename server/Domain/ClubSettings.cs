namespace CyberServer.Domain;

public class ClubSettings
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ClubId { get; set; }
    public Club Club { get; set; } = null!;

    public int? ShutdownIdlePcSeconds { get; set; }
    public int? AutoRestartAfterSessionSeconds { get; set; }
    public bool ShowGamerNameOnMap { get; set; } = true;
    public SinglePcActionMenuMode SinglePcActionMenuMode { get; set; } = SinglePcActionMenuMode.ContextMenu;
}

public enum SinglePcActionMenuMode
{
    ContextMenu,
    Buttons
}
