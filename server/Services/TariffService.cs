using CyberServer.Data;
using CyberServer.Domain;
using CyberServer.Models;
using Microsoft.EntityFrameworkCore;

namespace CyberServer.Services;

public interface ITariffService
{
    Task<IReadOnlyList<TariffPlan>> GetAllAsync(bool includeInactive = false, CancellationToken ct = default);
    Task<TariffPlan?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<TariffPlan> CreateAsync(CreateTariffPlanRequest request, CancellationToken ct = default);
    Task<TariffPlan?> UpdateAsync(Guid id, UpdateTariffPlanRequest request, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
}

public class TariffService(AppDbContext db) : ITariffService
{
    public async Task<IReadOnlyList<TariffPlan>> GetAllAsync(bool includeInactive = false, CancellationToken ct = default)
    {
        var q = db.TariffPlans.AsQueryable();
        if (!includeInactive) q = q.Where(t => t.IsActive);
        return await q.OrderBy(t => t.SortOrder).ThenBy(t => t.Name).ToListAsync(ct);
    }

    public Task<TariffPlan?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        db.TariffPlans.FirstOrDefaultAsync(t => t.Id == id, ct);

    public async Task<TariffPlan> CreateAsync(CreateTariffPlanRequest request, CancellationToken ct = default)
    {
        var plan = new TariffPlan
        {
            Name = request.Name,
            Type = request.Type,
            DurationMinutes = request.DurationMinutes,
            DurationDays = request.DurationDays,
            Price = request.Price,
            IsActive = request.IsActive,
            SortOrder = request.SortOrder
        };
        db.TariffPlans.Add(plan);
        await db.SaveChangesAsync(ct);
        return plan;
    }

    public async Task<TariffPlan?> UpdateAsync(Guid id, UpdateTariffPlanRequest request, CancellationToken ct = default)
    {
        var plan = await db.TariffPlans.FindAsync([id], ct);
        if (plan is null) return null;

        if (request.Name is not null) plan.Name = request.Name;
        if (request.DurationMinutes.HasValue) plan.DurationMinutes = request.DurationMinutes;
        if (request.DurationDays.HasValue) plan.DurationDays = request.DurationDays;
        if (request.Price.HasValue) plan.Price = request.Price.Value;
        if (request.IsActive.HasValue) plan.IsActive = request.IsActive.Value;
        if (request.SortOrder.HasValue) plan.SortOrder = request.SortOrder.Value;

        await db.SaveChangesAsync(ct);
        return plan;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var plan = await db.TariffPlans.FindAsync([id], ct);
        if (plan is null) return false;
        plan.IsActive = false;
        await db.SaveChangesAsync(ct);
        return true;
    }
}
