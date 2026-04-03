using CyberServer.Data;
using CyberServer.Domain;
using CyberServer.Models;
using Microsoft.EntityFrameworkCore;

namespace CyberServer.Services;

public interface ICustomerService
{
    Task<IReadOnlyList<Customer>> GetAllAsync(CancellationToken ct = default);
    Task<Customer?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Customer>> LookupAsync(string? username, string? phone, CancellationToken ct = default);
    Task<Customer> CreateAsync(CreateCustomerRequest request, CancellationToken ct = default);
}

public class CustomerService(AppDbContext db) : ICustomerService
{
    public async Task<IReadOnlyList<Customer>> GetAllAsync(CancellationToken ct = default) =>
        await db.Customers.Where(c => c.IsActive).OrderBy(c => c.CreatedAt).ToListAsync(ct);

    public Task<Customer?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        db.Customers.FirstOrDefaultAsync(c => c.Id == id, ct);

    public async Task<IReadOnlyList<Customer>> LookupAsync(string? username, string? phone, CancellationToken ct = default)
    {
        var q = db.Customers.Where(c => c.IsActive);
        if (!string.IsNullOrWhiteSpace(username))
            q = q.Where(c => c.Username != null && c.Username.Contains(username));
        if (!string.IsNullOrWhiteSpace(phone))
            q = q.Where(c => c.Phone != null && c.Phone.Contains(phone));
        return await q.ToListAsync(ct);
    }

    public async Task<Customer> CreateAsync(CreateCustomerRequest request, CancellationToken ct = default)
    {
        var customer = new Customer
        {
            Username = request.Username,
            Phone = request.Phone
        };
        db.Customers.Add(customer);
        await db.SaveChangesAsync(ct);
        return customer;
    }
}
