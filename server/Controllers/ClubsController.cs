using CyberServer.Data;
using CyberServer.Domain;
using CyberServer.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CyberServer.Controllers;

/// <summary>
/// Admin endpoints for managing clubs and their settings.
/// </summary>
[ApiController]
[Route("api/admin/clubs")]
[Authorize]
public class ClubsController(AppDbContext db, UserManager<ApplicationUser> userManager) : ControllerBase
{
    /// <summary>GET /api/admin/clubs — list clubs (filtered for non-SuperAdmin).</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ClubDto>>> GetAll(CancellationToken ct)
    {
        if (User.IsInRole("SuperAdmin"))
        {
            var clubs = await db.Clubs.OrderBy(c => c.Name).ToListAsync(ct);
            return Ok(clubs.Select(ToDto));
        }
        else
        {
            var user = await userManager.GetUserAsync(User);
            if (user is null) return Unauthorized();
            var assignedClubIds = await db.UserClubAccess
                .Where(a => a.UserId == user.Id)
                .Select(a => a.ClubId)
                .ToListAsync(ct);
            var clubs = await db.Clubs
                .Where(c => assignedClubIds.Contains(c.Id))
                .OrderBy(c => c.Name)
                .ToListAsync(ct);
            return Ok(clubs.Select(ToDto));
        }
    }

    /// <summary>GET /api/admin/clubs/{id}</summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ClubDto>> GetById(Guid id, CancellationToken ct)
    {
        var club = await db.Clubs.FindAsync([id], ct);
        return club is null ? NotFound() : Ok(ToDto(club));
    }

    /// <summary>POST /api/admin/clubs — create a new club.</summary>
    [HttpPost]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<ActionResult<ClubDto>> Create([FromBody] CreateClubRequest req, CancellationToken ct)
    {
        var club = new Club { Name = req.Name };
        db.Clubs.Add(club);

        // Create default settings
        db.ClubSettings.Add(new ClubSettings { ClubId = club.Id });

        // Create default layout
        db.MapLayouts.Add(new MapLayout { ClubId = club.Id, Name = "Main Hall" });

        await db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(GetById), new { id = club.Id }, ToDto(club));
    }

    /// <summary>PUT /api/admin/clubs/{id} — update club name.</summary>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ClubDto>> Update(Guid id, [FromBody] UpdateClubRequest req, CancellationToken ct)
    {
        var club = await db.Clubs.FindAsync([id], ct);
        if (club is null) return NotFound();

        club.Name = req.Name;
        await db.SaveChangesAsync(ct);
        return Ok(ToDto(club));
    }

    // ── Settings ──────────────────────────────────────────────────────────────

    /// <summary>GET /api/admin/clubs/{clubId}/settings</summary>
    [HttpGet("{clubId:guid}/settings")]
    public async Task<ActionResult<ClubSettingsDto>> GetSettings(Guid clubId, CancellationToken ct)
    {
        var settings = await db.ClubSettings.FirstOrDefaultAsync(s => s.ClubId == clubId, ct);
        if (settings is null)
        {
            // Auto-create if club exists
            var clubExists = await db.Clubs.AnyAsync(c => c.Id == clubId, ct);
            if (!clubExists) return NotFound();
            settings = new ClubSettings { ClubId = clubId };
            db.ClubSettings.Add(settings);
            await db.SaveChangesAsync(ct);
        }
        return Ok(ToSettingsDto(settings));
    }

    /// <summary>PUT /api/admin/clubs/{clubId}/settings</summary>
    [HttpPut("{clubId:guid}/settings")]
    public async Task<ActionResult<ClubSettingsDto>> UpdateSettings(
        Guid clubId, [FromBody] UpdateClubSettingsRequest req, CancellationToken ct)
    {
        var settings = await db.ClubSettings.FirstOrDefaultAsync(s => s.ClubId == clubId, ct);
        if (settings is null)
        {
            var clubExists = await db.Clubs.AnyAsync(c => c.Id == clubId, ct);
            if (!clubExists) return NotFound();
            settings = new ClubSettings { ClubId = clubId };
            db.ClubSettings.Add(settings);
        }

        settings.ShutdownIdlePcSeconds = req.ShutdownIdlePcSeconds;
        settings.AutoRestartAfterSessionSeconds = req.AutoRestartAfterSessionSeconds;
        settings.ShowGamerNameOnMap = req.ShowGamerNameOnMap;
        settings.SinglePcActionMenuMode = Enum.TryParse<SinglePcActionMenuMode>(req.SinglePcActionMenuMode, out var mode)
            ? mode : SinglePcActionMenuMode.ContextMenu;

        await db.SaveChangesAsync(ct);
        return Ok(ToSettingsDto(settings));
    }

    private static ClubDto ToDto(Club c) => new(c.Id, c.Name, c.CreatedAt);

    private static ClubSettingsDto ToSettingsDto(ClubSettings s) => new(
        s.ClubId,
        s.ShutdownIdlePcSeconds,
        s.AutoRestartAfterSessionSeconds,
        s.ShowGamerNameOnMap,
        s.SinglePcActionMenuMode.ToString());
}
