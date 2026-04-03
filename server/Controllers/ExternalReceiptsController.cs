using CyberServer.Data;
using CyberServer.Domain;
using CyberServer.Models;
using Microsoft.AspNetCore.Mvc;

namespace CyberServer.Controllers;

/// <summary>
/// Admin endpoints for linking external receipts (e.g. from ERPNext/POS) to workstations or sessions.
/// Protected by X-Admin-Key header (see AdminKeyMiddleware).
/// </summary>
[ApiController]
[Route("api/admin/external-receipts")]
public class ExternalReceiptsController(AppDbContext db) : ControllerBase
{
    /// <summary>
    /// POST /api/admin/external-receipts/link
    /// Creates an ExternalReceipt record and optionally links it to a workstation and/or session.
    /// </summary>
    [HttpPost("link")]
    public async Task<ActionResult<ExternalReceiptDto>> Link(
        [FromBody] LinkExternalReceiptRequest request,
        CancellationToken ct)
    {
        if (request.WorkstationId.HasValue)
        {
            var exists = await db.Workstations.FindAsync([request.WorkstationId.Value], ct);
            if (exists is null)
                return BadRequest(new { error = $"Workstation {request.WorkstationId} not found." });
        }

        var receipt = new ExternalReceipt
        {
            Source = request.Source,
            ReceiptNo = request.ReceiptNo,
            Amount = request.Amount,
            Currency = request.Currency,
            WorkstationId = request.WorkstationId,
            SessionId = request.SessionId,
            RawJson = request.RawJson,
            CreatedAt = DateTime.UtcNow
        };

        db.ExternalReceipts.Add(receipt);
        await db.SaveChangesAsync(ct);

        return Created($"/api/admin/external-receipts/{receipt.Id}", ToDto(receipt));
    }

    private static ExternalReceiptDto ToDto(ExternalReceipt r) => new(
        r.Id, r.Source, r.ReceiptNo, r.Amount, r.Currency, r.CreatedAt, r.WorkstationId, r.SessionId);
}
