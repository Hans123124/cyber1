using CyberServer.Data;
using CyberServer.Domain;
using CyberServer.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CyberServer.Controllers;

/// <summary>
/// Admin endpoints for map layouts, items, and zones.
/// </summary>
[ApiController]
[Authorize]
public class MapController(AppDbContext db) : ControllerBase
{
    // ── Layouts ───────────────────────────────────────────────────────────────

    /// <summary>GET /api/admin/clubs/{clubId}/layouts</summary>
    [HttpGet("api/admin/clubs/{clubId:guid}/layouts")]
    public async Task<ActionResult<IReadOnlyList<MapLayoutDto>>> GetLayouts(Guid clubId, CancellationToken ct)
    {
        var layouts = await db.MapLayouts
            .Where(l => l.ClubId == clubId)
            .OrderBy(l => l.Name)
            .ToListAsync(ct);
        return Ok(layouts.Select(ToLayoutDto));
    }

    /// <summary>POST /api/admin/clubs/{clubId}/layouts</summary>
    [HttpPost("api/admin/clubs/{clubId:guid}/layouts")]
    public async Task<ActionResult<MapLayoutDto>> CreateLayout(
        Guid clubId, [FromBody] CreateMapLayoutRequest req, CancellationToken ct)
    {
        var clubExists = await db.Clubs.AnyAsync(c => c.Id == clubId, ct);
        if (!clubExists) return NotFound("Club not found.");

        var layout = new MapLayout
        {
            ClubId = clubId,
            Name = req.Name,
            GridWidth = req.GridWidth,
            GridHeight = req.GridHeight,
            GridCellSizePx = req.GridCellSizePx
        };
        db.MapLayouts.Add(layout);
        await db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(GetLayout), new { layoutId = layout.Id }, ToLayoutDto(layout));
    }

    /// <summary>GET /api/admin/layouts/{layoutId}</summary>
    [HttpGet("api/admin/layouts/{layoutId:guid}")]
    public async Task<ActionResult<MapLayoutDto>> GetLayout(Guid layoutId, CancellationToken ct)
    {
        var layout = await db.MapLayouts.FindAsync([layoutId], ct);
        return layout is null ? NotFound() : Ok(ToLayoutDto(layout));
    }

    /// <summary>PUT /api/admin/layouts/{layoutId}</summary>
    [HttpPut("api/admin/layouts/{layoutId:guid}")]
    public async Task<ActionResult<MapLayoutDto>> UpdateLayout(
        Guid layoutId, [FromBody] UpdateMapLayoutRequest req, CancellationToken ct)
    {
        var layout = await db.MapLayouts.FindAsync([layoutId], ct);
        if (layout is null) return NotFound();

        if (req.Name is not null) layout.Name = req.Name;
        if (req.GridWidth.HasValue) layout.GridWidth = req.GridWidth.Value;
        if (req.GridHeight.HasValue) layout.GridHeight = req.GridHeight.Value;
        if (req.GridCellSizePx.HasValue) layout.GridCellSizePx = req.GridCellSizePx.Value;

        await db.SaveChangesAsync(ct);
        return Ok(ToLayoutDto(layout));
    }

    // ── Items ─────────────────────────────────────────────────────────────────

    /// <summary>GET /api/admin/layouts/{layoutId}/items</summary>
    [HttpGet("api/admin/layouts/{layoutId:guid}/items")]
    public async Task<ActionResult<IReadOnlyList<MapItemDto>>> GetItems(Guid layoutId, CancellationToken ct)
    {
        var items = await db.MapItems
            .Where(i => i.LayoutId == layoutId)
            .ToListAsync(ct);
        return Ok(items.Select(ToItemDto));
    }

    /// <summary>POST /api/admin/layouts/{layoutId}/items</summary>
    [HttpPost("api/admin/layouts/{layoutId:guid}/items")]
    public async Task<ActionResult<MapItemDto>> CreateItem(
        Guid layoutId, [FromBody] CreateMapItemRequest req, CancellationToken ct)
    {
        var layoutExists = await db.MapLayouts.AnyAsync(l => l.Id == layoutId, ct);
        if (!layoutExists) return NotFound("Layout not found.");

        if (!Enum.TryParse<MapItemType>(req.Type, out var itemType))
            return BadRequest($"Unknown item type: {req.Type}");

        var item = new MapItem
        {
            LayoutId = layoutId,
            Type = itemType,
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
        return CreatedAtAction(nameof(GetItems), new { layoutId }, ToItemDto(item));
    }

    /// <summary>PUT /api/admin/items/{id}</summary>
    [HttpPut("api/admin/items/{id:guid}")]
    public async Task<ActionResult<MapItemDto>> UpdateItem(
        Guid id, [FromBody] UpdateMapItemRequest req, CancellationToken ct)
    {
        var item = await db.MapItems.FindAsync([id], ct);
        if (item is null) return NotFound();

        if (req.Type is not null)
        {
            if (!Enum.TryParse<MapItemType>(req.Type, out var t)) return BadRequest($"Unknown item type: {req.Type}");
            item.Type = t;
        }
        if (req.X.HasValue) item.X = req.X.Value;
        if (req.Y.HasValue) item.Y = req.Y.Value;
        if (req.W.HasValue) item.W = req.W.Value;
        if (req.H.HasValue) item.H = req.H.Value;
        if (req.Rotation.HasValue) item.Rotation = req.Rotation.Value;
        if (req.Label is not null) item.Label = req.Label;
        item.WorkstationId = req.WorkstationId;
        item.ZoneId = req.ZoneId;
        if (req.MetaJson is not null) item.MetaJson = req.MetaJson;

        await db.SaveChangesAsync(ct);
        return Ok(ToItemDto(item));
    }

    /// <summary>DELETE /api/admin/items/{id}</summary>
    [HttpDelete("api/admin/items/{id:guid}")]
    public async Task<IActionResult> DeleteItem(Guid id, CancellationToken ct)
    {
        var item = await db.MapItems.FindAsync([id], ct);
        if (item is null) return NotFound();
        db.MapItems.Remove(item);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    // ── Zones ─────────────────────────────────────────────────────────────────

    /// <summary>GET /api/admin/layouts/{layoutId}/zones</summary>
    [HttpGet("api/admin/layouts/{layoutId:guid}/zones")]
    public async Task<ActionResult<IReadOnlyList<ZoneDto>>> GetZones(Guid layoutId, CancellationToken ct)
    {
        var zones = await db.Zones
            .Where(z => z.LayoutId == layoutId)
            .ToListAsync(ct);
        return Ok(zones.Select(ToZoneDto));
    }

    /// <summary>POST /api/admin/layouts/{layoutId}/zones</summary>
    [HttpPost("api/admin/layouts/{layoutId:guid}/zones")]
    public async Task<ActionResult<ZoneDto>> CreateZone(
        Guid layoutId, [FromBody] CreateZoneRequest req, CancellationToken ct)
    {
        var layoutExists = await db.MapLayouts.AnyAsync(l => l.Id == layoutId, ct);
        if (!layoutExists) return NotFound("Layout not found.");

        var zone = new Zone
        {
            LayoutId = layoutId,
            Name = req.Name,
            Color = req.Color,
            X = req.X,
            Y = req.Y,
            W = req.W,
            H = req.H
        };
        db.Zones.Add(zone);
        await db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(GetZones), new { layoutId }, ToZoneDto(zone));
    }

    /// <summary>PUT /api/admin/zones/{id}</summary>
    [HttpPut("api/admin/zones/{id:guid}")]
    public async Task<ActionResult<ZoneDto>> UpdateZone(
        Guid id, [FromBody] UpdateZoneRequest req, CancellationToken ct)
    {
        var zone = await db.Zones.FindAsync([id], ct);
        if (zone is null) return NotFound();

        if (req.Name is not null) zone.Name = req.Name;
        if (req.Color is not null) zone.Color = req.Color;
        if (req.X.HasValue) zone.X = req.X.Value;
        if (req.Y.HasValue) zone.Y = req.Y.Value;
        if (req.W.HasValue) zone.W = req.W.Value;
        if (req.H.HasValue) zone.H = req.H.Value;

        await db.SaveChangesAsync(ct);
        return Ok(ToZoneDto(zone));
    }

    /// <summary>DELETE /api/admin/zones/{id}</summary>
    [HttpDelete("api/admin/zones/{id:guid}")]
    public async Task<IActionResult> DeleteZone(Guid id, CancellationToken ct)
    {
        var zone = await db.Zones.FindAsync([id], ct);
        if (zone is null) return NotFound();
        db.Zones.Remove(zone);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    // ── Mapping helpers ───────────────────────────────────────────────────────

    private static MapLayoutDto ToLayoutDto(MapLayout l) => new(
        l.Id, l.ClubId, l.Name, l.GridWidth, l.GridHeight, l.GridCellSizePx, l.CreatedAt);

    private static MapItemDto ToItemDto(MapItem i) => new(
        i.Id, i.LayoutId, i.Type.ToString(), i.X, i.Y, i.W, i.H, i.Rotation,
        i.Label, i.WorkstationId, i.ZoneId, i.MetaJson);

    private static ZoneDto ToZoneDto(Zone z) => new(
        z.Id, z.LayoutId, z.Name, z.Color, z.X, z.Y, z.W, z.H);
}
