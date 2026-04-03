using CyberServer.Domain;
using CyberServer.Models;
using CyberServer.Services;
using Microsoft.AspNetCore.Mvc;

namespace CyberServer.Controllers;

/// <summary>
/// Admin endpoints for managing workstations and sending commands.
/// Protected by X-Admin-Key header (see AdminKeyMiddleware).
/// </summary>
[ApiController]
[Route("api/admin/workstations")]
public class WorkstationsController(IWorkstationService workstationService, ICommandService commandService) : ControllerBase
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

    private static WorkstationDto ToDto(Domain.Workstation w) => new(
        w.Id, w.Name, w.State, w.IsOnline, w.LastSeenAt, w.AgentVersion, w.IpAddress);

    private static CommandLogDto ToLogDto(CommandLog c) => new(
        c.Id, c.WorkstationId, c.Command.ToString(), c.IssuedBy, c.Status.ToString(), c.Notes, c.IssuedAt, c.DeliveredAt);
}
