using CyberServer.Domain;
using CyberServer.Models;
using CyberServer.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CyberServer.Controllers;

[ApiController]
[Route("api/admin/sessions")]
[Authorize]
public class SessionsController(ISessionService sessionService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetSessions(
        [FromQuery] Guid? workstationId,
        [FromQuery] DateOnly? date,
        CancellationToken ct = default)
    {
        var sessions = await sessionService.GetAsync(workstationId, date, ct);
        return Ok(sessions.Select(ToDto));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct = default)
    {
        var session = await sessionService.GetByIdAsync(id, ct);
        return session is null ? NotFound() : Ok(ToDto(session));
    }

    [HttpPost("start")]
    public async Task<IActionResult> Start([FromBody] StartSessionRequest request, CancellationToken ct = default)
    {
        try
        {
            var session = await sessionService.StartAsync(request, ct);
            return CreatedAtAction(nameof(GetById), new { id = session.Id }, ToDto(session));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("{id:guid}/extend")]
    public async Task<IActionResult> Extend(Guid id, [FromBody] ExtendSessionRequest request, CancellationToken ct = default)
    {
        try
        {
            var session = await sessionService.ExtendAsync(id, request, ct);
            return Ok(ToDto(session));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("{id:guid}/end")]
    public async Task<IActionResult> End(Guid id, [FromBody] EndSessionRequest request, CancellationToken ct = default)
    {
        try
        {
            await sessionService.EndAsync(id, request.Reboot, "admin", ct);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    private static SessionDto ToDto(Session s) => new(
        s.Id,
        s.WorkstationId,
        s.CustomerId,
        s.GuestName,
        s.TariffPlanId,
        s.TariffPlan?.Name ?? string.Empty,
        s.SaleId,
        s.StartedAt,
        s.EndsAt,
        s.Status.ToString(),
        s.EndedAt);
}
