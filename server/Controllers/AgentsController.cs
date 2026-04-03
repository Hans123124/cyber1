using CyberServer.Models;
using CyberServer.Services;
using Microsoft.AspNetCore.Mvc;

namespace CyberServer.Controllers;

/// <summary>
/// Endpoints used by Windows agents (authentication with shared secret).
/// </summary>
[ApiController]
[Route("api/agents")]
public class AgentsController(IWorkstationService workstationService) : ControllerBase
{
    /// <summary>
    /// POST /api/agents/register
    /// Registers or re-registers a workstation. Returns workstationId + shared secret.
    /// </summary>
    [HttpPost("register")]
    public async Task<ActionResult<RegisterResponse>> Register(
        [FromBody] RegisterRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.MachineFingerprint))
            return BadRequest("MachineFingerprint is required.");
        if (string.IsNullOrWhiteSpace(request.WorkstationName))
            return BadRequest("WorkstationName is required.");

        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? string.Empty;
        var (workstation, secret) = await workstationService.RegisterAsync(
            request.MachineFingerprint,
            request.WorkstationName,
            request.AgentVersion,
            request.OsVersion,
            ip,
            ct);

        return Ok(new RegisterResponse(workstation.Id, secret, workstation.Name));
    }

    /// <summary>
    /// POST /api/agents/heartbeat
    /// Agent posts its status periodically. Authenticated via WorkstationId + Secret in body.
    /// </summary>
    [HttpPost("heartbeat")]
    public async Task<ActionResult<HeartbeatResponse>> Heartbeat(
        [FromBody] HeartbeatRequest request, CancellationToken ct)
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? string.Empty;
        var workstation = await workstationService.AuthenticateAsync(request.WorkstationId, request.Secret, ct);
        if (workstation is null)
            return Unauthorized(new { error = "Invalid workstationId or secret." });

        await workstationService.HeartbeatAsync(
            workstation,
            request.AgentVersion,
            request.State,
            request.CpuUsage,
            request.RamUsageMb,
            ip,
            ct);

        return Ok(new HeartbeatResponse(true, DateTime.UtcNow));
    }
}
