using CyberServer.Domain;
using CyberServer.Models;
using CyberServer.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CyberServer.Controllers;

[ApiController]
[Route("api/admin/tariffs")]
[Authorize]
public class TariffsController(ITariffService tariffService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] bool includeInactive = false,
        [FromQuery] Guid? clubId = null,
        CancellationToken ct = default)
    {
        var plans = await tariffService.GetAllAsync(includeInactive, clubId, ct);
        return Ok(plans.Select(ToDto));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct = default)
    {
        var plan = await tariffService.GetByIdAsync(id, ct);
        return plan is null ? NotFound() : Ok(ToDto(plan));
    }

    [HttpPost]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> Create([FromBody] CreateTariffPlanRequest request, CancellationToken ct = default)
    {
        if (request.Type == TariffType.Hourly)
        {
            if (request.HourlyRateMdl <= 0)
                return BadRequest("HourlyRateMdl must be positive for Hourly plans.");
        }
        else if (request.Type == TariffType.Monthly)
        {
            if (request.DurationDays is null or <= 0)
                return BadRequest("DurationDays is required and must be positive for Monthly plans.");
        }

        var plan = await tariffService.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = plan.Id }, ToDto(plan));
    }

    [HttpPatch("{id:guid}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateTariffPlanRequest request, CancellationToken ct = default)
    {
        var plan = await tariffService.UpdateAsync(id, request, ct);
        return plan is null ? NotFound() : Ok(ToDto(plan));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct = default)
    {
        var deleted = await tariffService.DeleteAsync(id, ct);
        return deleted ? NoContent() : NotFound();
    }

    private static TariffPlanDto ToDto(TariffPlan p) => new(
        p.Id, p.ClubId, p.Name, p.Type.ToString(), p.HourlyRateMdl, p.DurationMinutes, p.DurationDays, p.Price, p.IsActive, p.SortOrder);
}
