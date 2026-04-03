namespace CyberServer.Domain;

/// <summary>
/// Singleton club configuration persisted in the database.
/// Always has exactly one row (Id = 1).
/// </summary>
public class ClubSettings
{
    public int Id { get; set; } = 1;

    /// <summary>Number of idle seconds before auto-shutdown (0 = disabled).</summary>
    public int ShutdownIdlePcSeconds { get; set; } = 0;

    /// <summary>Seconds to wait before auto-restarting a PC after a session ends (0 = disabled).</summary>
    public int AutoRestartAfterSessionSeconds { get; set; } = 0;

    /// <summary>Whether the auto-restart feature is enabled.</summary>
    public bool AutoRestartEnabled { get; set; } = false;

    /// <summary>Show the gamer's name on the map tile while a session is active.</summary>
    public bool ShowGamerNameOnMap { get; set; } = true;

    /// <summary>How to surface per-PC actions: "ContextMenu" or "Buttons".</summary>
    public string ActionMenuMode { get; set; } = "ContextMenu";

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
