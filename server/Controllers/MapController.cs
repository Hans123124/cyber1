using CyberServer.Data;
using CyberServer.Domain;
using CyberServer.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CyberServer.Controllers;

/// <summary>
/// Admin endpoints for the floor-plan map (layouts, items, zones).
/// Protected by X-Admin-Key header (see AdminKeyMiddleware).
/// </summary>
[ApiController]
[Route("api/admin/map")]
public class MapController(AppDbContext db) : ControllerBase
{
    // ── Layout ────────────────────────────────────────────────────────────────

    /// <summary>GET /api/admin/map — returns the primary layout (creates one if missing).</summary>
    [HttpGet]
    public async Task<ActionResult<MapLayoutDto>> GetLayout(CancellationToken ct)
    {
        var layout = await EnsureLayoutAsync(ct);
        return Ok(ToLayoutDto(layout));
    }

    /// <summary>PUT /api/admin/map — updates layout metadata (name, dimensions).</summary>
    [HttpPut]
    public async Task<ActionResult<MapLayoutDto>> PutLayout(
        [FromBody] UpdateMapLayoutRequest req,
        CancellationToken ct)
    {
        var layout = await EnsureLayoutAsync(ct);
        layout.Name = req.Name;
        layout.Width = req.Width;
        layout.Height = req.Height;
        layout.GridSize = req.GridSize;
        layout.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return Ok(ToLayoutDto(layout));
    }

    // ── Items ─────────────────────────────────────────────────────────────────

    /// <summary>GET /api/admin/map/items — returns all items for the primary layout.</summary>
    [HttpGet("items")]
    public async Task<ActionResult<IReadOnlyList<MapItemDto>>> GetItems(CancellationToken ct)
    {
        var layout = await EnsureLayoutAsync(ct);
        return Ok(layout.Items.Select(ToItemDto).ToList());
    }

    /// <summary>POST /api/admin/map/items — adds a new item to a layout.</summary>
    [HttpPost("items")]
    public async Task<ActionResult<MapItemDto>> CreateItem(
        [FromBody] CreateMapItemRequest req,
        CancellationToken ct)
    {
        if (!await db.MapLayouts.AnyAsync(l => l.Id == req.LayoutId, ct))
            return NotFound($"Layout {req.LayoutId} not found.");

        var item = new MapItem
        {
            LayoutId = req.LayoutId,
            Type = req.Type,
            X = req.X,
            Y = req.Y,
            W = req.W,
            H = req.H,
            Rotation = req.Rotation,
            Label = req.Label,
            WorkstationId = req.WorkstationId,
            ZoneId = req.ZoneId,
            MetaJson = req.MetaJson
        };

        db.MapItems.Add(item);
        await db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(GetItems), null, ToItemDto(item));
    }

    /// <summary>PUT /api/admin/map/items/{id} — updates an existing item.</summary>
    [HttpPut("items/{id:guid}")]
    public async Task<ActionResult<MapItemDto>> UpdateItem(
        Guid id,
        [FromBody] UpdateMapItemRequest req,
        CancellationToken ct)
    {
        var item = await db.MapItems.FindAsync([id], ct);
        if (item is null) return NotFound();

        item.Type = req.Type;
        item.X = req.X;
        item.Y = req.Y;
        item.W = req.W;
        item.H = req.H;
        item.Rotation = req.Rotation;
        item.Label = req.Label;
        item.WorkstationId = req.WorkstationId;
        item.ZoneId = req.ZoneId;
        item.MetaJson = req.MetaJson;
        item.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
        return Ok(ToItemDto(item));
    }

    /// <summary>DELETE /api/admin/map/items/{id} — removes an item from the layout.</summary>
    [HttpDelete("items/{id:guid}")]
    public async Task<IActionResult> DeleteItem(Guid id, CancellationToken ct)
    {
        var item = await db.MapItems.FindAsync([id], ct);
        if (item is null) return NotFound();
        db.MapItems.Remove(item);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    // ── Zones ─────────────────────────────────────────────────────────────────

    /// <summary>GET /api/admin/map/zones — returns all zones for the primary layout.</summary>
    [HttpGet("zones")]
    public async Task<ActionResult<IReadOnlyList<ZoneDto>>> GetZones(CancellationToken ct)
    {
        var layout = await EnsureLayoutAsync(ct);
        return Ok(layout.Zones.Select(ToZoneDto).ToList());
    }

    /// <summary>POST /api/admin/map/zones — creates a new zone.</summary>
    [HttpPost("zones")]
    public async Task<ActionResult<ZoneDto>> CreateZone(
        [FromBody] CreateZoneRequest req,
        CancellationToken ct)
    {
        if (!await db.MapLayouts.AnyAsync(l => l.Id == req.LayoutId, ct))
            return NotFound($"Layout {req.LayoutId} not found.");

        var zone = new Zone
        {
            LayoutId = req.LayoutId,
            Name = req.Name,
            Color = req.Color,
            X = req.X,
            Y = req.Y,
            W = req.W,
            H = req.H,
            MetaJson = req.MetaJson
        };

        db.Zones.Add(zone);
        await db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(GetZones), null, ToZoneDto(zone));
    }

    /// <summary>PUT /api/admin/map/zones/{id} — updates an existing zone.</summary>
    [HttpPut("zones/{id:guid}")]
    public async Task<ActionResult<ZoneDto>> UpdateZone(
        Guid id,
        [FromBody] UpdateZoneRequest req,
        CancellationToken ct)
    {
        var zone = await db.Zones.FindAsync([id], ct);
        if (zone is null) return NotFound();

        zone.Name = req.Name;
        zone.Color = req.Color;
        zone.X = req.X;
        zone.Y = req.Y;
        zone.W = req.W;
        zone.H = req.H;
        zone.MetaJson = req.MetaJson;
        zone.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
        return Ok(ToZoneDto(zone));
    }

    /// <summary>DELETE /api/admin/map/zones/{id} — removes a zone.</summary>
    [HttpDelete("zones/{id:guid}")]
    public async Task<IActionResult> DeleteZone(Guid id, CancellationToken ct)
    {
        var zone = await db.Zones.FindAsync([id], ct);
        if (zone is null) return NotFound();
        db.Zones.Remove(zone);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task<MapLayout> EnsureLayoutAsync(CancellationToken ct)
    {
        var layout = await db.MapLayouts
            .Include(l => l.Items)
            .Include(l => l.Zones)
            .OrderBy(l => l.CreatedAt)
            .FirstOrDefaultAsync(ct);

        if (layout is null)
        {
            layout = new MapLayout();
            db.MapLayouts.Add(layout);
            await db.SaveChangesAsync(ct);
        }

        return layout;
    }

    private static MapLayoutDto ToLayoutDto(MapLayout l) => new(
        l.Id,
        l.Name,
        l.Width,
        l.Height,
        l.GridSize,
        l.Items.Select(ToItemDto).ToList(),
        l.Zones.Select(ToZoneDto).ToList()
    );

    private static MapItemDto ToItemDto(MapItem i) => new(
        i.Id,
        i.LayoutId,
        i.Type,
        i.X,
        i.Y,
        i.W,
        i.H,
        i.Rotation,
        i.Label,
        i.WorkstationId,
        i.ZoneId,
        i.MetaJson
    );

    private static ZoneDto ToZoneDto(Zone z) => new(
        z.Id,
        z.LayoutId,
        z.Name,
        z.Color,
        z.X,
        z.Y,
        z.W,
        z.H,
        z.MetaJson
    );
}
