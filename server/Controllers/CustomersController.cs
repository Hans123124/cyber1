using CyberServer.Domain;
using CyberServer.Models;
using CyberServer.Services;
using Microsoft.AspNetCore.Mvc;

namespace CyberServer.Controllers;

[ApiController]
[Route("api/admin/customers")]
public class CustomersController(ICustomerService customerService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct = default)
    {
        var customers = await customerService.GetAllAsync(ct);
        return Ok(customers.Select(ToDto));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct = default)
    {
        var customer = await customerService.GetByIdAsync(id, ct);
        return customer is null ? NotFound() : Ok(ToDto(customer));
    }

    [HttpGet("lookup")]
    public async Task<IActionResult> Lookup([FromQuery] string? username, [FromQuery] string? phone, CancellationToken ct = default)
    {
        var customers = await customerService.LookupAsync(username, phone, ct);
        return Ok(customers.Select(ToDto));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCustomerRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Username) && string.IsNullOrWhiteSpace(request.Phone))
            return BadRequest("At least one of Username or Phone is required.");

        var customer = await customerService.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = customer.Id }, ToDto(customer));
    }

    private static CustomerDto ToDto(Customer c) =>
        new(c.Id, c.Username, c.Phone, c.CreatedAt, c.IsActive);
}
