using CyberServer.Data;
using CyberServer.Domain;
using CyberServer.Models;
using CyberServer.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CyberServer.Controllers;

/// <summary>
/// Admin endpoints for managing workstations and sending commands.
/// Protected by X-Admin-Key header (see AdminKeyMiddleware).
/// </summary>
[ApiController]
[Route("api/admin/workstations")]
public class WorkstationsController(
    IWorkstationService workstationService,
    ICommandService commandService,
    AppDbContext db,
    IConfiguration config) : ControllerBase
{
    /// <summary>
    /// GET /api/admin/workstations
    /// Returns all workstations with their online status.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<WorkstationDto>>> GetAll(CancellationToken ct)
    {
        var workstations = await workstationService.GetAllAsync(ct);
        return Ok(workstations.Select(ToDto));
    }

    /// <summary>
    /// GET /api/admin/workstations/{id}
    /// Returns a single workstation.
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<WorkstationDto>> GetById(Guid id, CancellationToken ct)
    {
        var workstation = await workstationService.GetByIdAsync(id, ct);
        return workstation is null ? NotFound() : Ok(ToDto(workstation));
    }

    /// <summary>
    /// POST /api/admin/workstations/{id}/commands
    /// Sends a command (Lock/Unlock/Reboot/Shutdown/Message) to a workstation via SignalR.
    /// </summary>
    [HttpPost("{id:guid}/commands")]
    public async Task<IActionResult> SendCommand(Guid id, [FromBody] SendCommandRequest request, CancellationToken ct)
    {
        var workstation = await workstationService.GetByIdAsync(id, ct);
        if (workstation is null) return NotFound();

        await commandService.SendCommandAsync(id, request.Command, request.IssuedBy, request.Notes, ct);
        return Accepted();
    }

    /// <summary>
    /// GET /api/admin/workstations/{id}/commands
    /// Returns the command/audit log for a workstation.
    /// </summary>
    [HttpGet("{id:guid}/commands")]
    public async Task<ActionResult<IReadOnlyList<CommandLogDto>>> GetCommandLog(Guid id, CancellationToken ct)
    {
        var logs = await commandService.GetLogsAsync(id, 200, ct);
        return Ok(logs.Select(ToLogDto));
    }

    /// <summary>
    /// GET /api/admin/commands
    /// Returns the global audit log.
    /// </summary>
    [HttpGet("/api/admin/commands")]
    public async Task<ActionResult<IReadOnlyList<CommandLogDto>>> GetAllLogs(CancellationToken ct)
    {
        var logs = await commandService.GetLogsAsync(take: 500, ct: ct);
        return Ok(logs.Select(ToLogDto));
    }

    /// <summary>
    /// PATCH /api/admin/workstations/{id}/integration
    /// Sets MeshCentralDeviceId, FogHostId, and/or ImageGroup for a workstation.
    /// </summary>
    [HttpPatch("{id:guid}/integration")]
    public async Task<ActionResult<WorkstationDto>> UpdateIntegration(
        Guid id,
        [FromBody] UpdateIntegrationRequest request,
        CancellationToken ct)
    {
        var workstation = await db.Workstations.FindAsync([id], ct);
        if (workstation is null) return NotFound();

        workstation.MeshCentralDeviceId = request.MeshCentralDeviceId;
        workstation.FogHostId = request.FogHostId;
        workstation.ImageGroup = request.ImageGroup;
        await db.SaveChangesAsync(ct);

        return Ok(ToDto(workstation));
    }

    /// <summary>
    /// GET /api/admin/workstations/{id}/remote-link
    /// Returns a URL to open the workstation in MeshCentral.
    /// The URL template is configured via Integrations:MeshCentral:RemoteLinkTemplate in appsettings.json.
    /// Default template: {BaseUrl}/?viewid=50&amp;id={DeviceId}
    /// </summary>
    [HttpGet("{id:guid}/remote-link")]
    public async Task<ActionResult<RemoteLinkResponse>> GetRemoteLink(Guid id, CancellationToken ct)
    {
        var workstation = await db.Workstations.FindAsync([id], ct);
        if (workstation is null) return NotFound();

        var baseUrl = config["Integrations:MeshCentral:BaseUrl"] ?? string.Empty;
        var template = config["Integrations:MeshCentral:RemoteLinkTemplate"]
                       ?? "{BaseUrl}/?viewid=50&id={DeviceId}";

        string? remoteUrl = null;
        string note;

        if (string.IsNullOrWhiteSpace(workstation.MeshCentralDeviceId))
        {
            note = "MeshCentralDeviceId is not set for this workstation. " +
                   "Use PATCH /integration to assign it.";
        }
        else if (string.IsNullOrWhiteSpace(baseUrl))
        {
            note = "Integrations:MeshCentral:BaseUrl is not configured in appsettings.json.";
        }
        else
        {
            remoteUrl = template
                .Replace("{BaseUrl}", baseUrl.TrimEnd('/'))
                .Replace("{DeviceId}", workstation.MeshCentralDeviceId);
            note = "MeshCentral uses TCP over HTTPS/WebSocket (port 443). " +
                   "For raw TCP port mapping, configure MeshCentral Router on the server.";
        }

        return Ok(new RemoteLinkResponse(workstation.Id, workstation.MeshCentralDeviceId, remoteUrl, note));
    }

    /// <summary>
    /// POST /api/admin/workstations/{id}/mark-for-reimage
    /// Marks a workstation for reimaging. Writes an audit log entry; no FOG API calls are made.
    /// </summary>
    [HttpPost("{id:guid}/mark-for-reimage")]
    public async Task<IActionResult> MarkForReimage(
        Guid id,
        [FromBody] MarkForReimageRequest request,
        CancellationToken ct)
    {
        var workstation = await workstationService.GetByIdAsync(id, ct);
        if (workstation is null) return NotFound();

        var notes = $"[REIMAGE] {request.Notes}".TrimEnd();
        await commandService.SendCommandAsync(id, CommandType.Reimage, request.IssuedBy, notes, ct);
        return Accepted(new { message = "Workstation marked for reimage. Audit log entry created.", workstationId = id });
    }

    private static WorkstationDto ToDto(Domain.Workstation w) => new(
        w.Id, w.Name, w.State, w.IsOnline, w.LastSeenAt, w.AgentVersion, w.IpAddress,
        w.MeshCentralDeviceId, w.FogHostId, w.ImageGroup);

    private static CommandLogDto ToLogDto(CommandLog c) => new(
        c.Id, c.WorkstationId, c.Command.ToString(), c.IssuedBy, c.Status.ToString(), c.Notes, c.IssuedAt, c.DeliveredAt);
}
