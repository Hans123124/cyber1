using CyberServer.Data;
using CyberServer.Domain;
using CyberServer.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CyberServer.Controllers;

/// <summary>
/// Admin endpoints for club-wide settings.
/// Protected by X-Admin-Key header (see AdminKeyMiddleware).
/// </summary>
[ApiController]
[Route("api/admin/settings")]
public class SettingsController(AppDbContext db) : ControllerBase
{
    /// <summary>GET /api/admin/settings — returns current settings (creates default if missing).</summary>
    [HttpGet]
    public async Task<ActionResult<ClubSettingsDto>> Get(CancellationToken ct)
    {
        var settings = await EnsureSettingsAsync(ct);
        return Ok(ToDto(settings));
    }

    /// <summary>PUT /api/admin/settings — replaces all settings.</summary>
    [HttpPut]
    public async Task<ActionResult<ClubSettingsDto>> Put(
        [FromBody] UpdateClubSettingsRequest req,
        CancellationToken ct)
    {
        var settings = await EnsureSettingsAsync(ct);

        settings.ShutdownIdlePcSeconds = req.ShutdownIdlePcSeconds;
        settings.AutoRestartAfterSessionSeconds = req.AutoRestartAfterSessionSeconds;
        settings.AutoRestartEnabled = req.AutoRestartEnabled;
        settings.ShowGamerNameOnMap = req.ShowGamerNameOnMap;
        settings.ActionMenuMode = req.ActionMenuMode;
        settings.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
        return Ok(ToDto(settings));
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task<ClubSettings> EnsureSettingsAsync(CancellationToken ct)
    {
        var settings = await db.ClubSettings.FindAsync([1], ct);
        if (settings is null)
        {
            settings = new ClubSettings { Id = 1 };
            db.ClubSettings.Add(settings);
            await db.SaveChangesAsync(ct);
        }
        return settings;
    }

    private static ClubSettingsDto ToDto(ClubSettings s) => new(
        s.ShutdownIdlePcSeconds,
        s.AutoRestartAfterSessionSeconds,
        s.AutoRestartEnabled,
        s.ShowGamerNameOnMap,
        s.ActionMenuMode,
        s.UpdatedAt
    );
}
