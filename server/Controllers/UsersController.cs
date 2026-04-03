using CyberServer.Data;
using CyberServer.Domain;
using CyberServer.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CyberServer.Controllers;

[ApiController]
[Route("api/admin/users")]
[Authorize(Roles = "SuperAdmin")]
public class UsersController(
    UserManager<ApplicationUser> userManager,
    RoleManager<IdentityRole> roleManager,
    AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var users = await userManager.Users
            .Include(u => u.ClubAccess)
            .ThenInclude(a => a.Club)
            .ToListAsync(ct);

        var result = new List<object>();
        foreach (var user in users)
        {
            var roles = await userManager.GetRolesAsync(user);
            result.Add(new
            {
                id = user.Id,
                email = user.Email,
                username = user.UserName,
                displayName = user.DisplayName,
                roles,
                clubs = user.ClubAccess.Select(a => new { a.ClubId, a.Club.Name })
            });
        }
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateUserRequest req)
    {
        if (req.Role == "SuperAdmin" && !User.IsInRole("SuperAdmin"))
            return Forbid();

        var user = new ApplicationUser
        {
            UserName = req.Username ?? req.Email,
            Email = req.Email,
            DisplayName = req.DisplayName,
            EmailConfirmed = true
        };

        var result = await userManager.CreateAsync(user, req.Password);
        if (!result.Succeeded)
            return BadRequest(new { errors = result.Errors.Select(e => e.Description) });

        if (!string.IsNullOrWhiteSpace(req.Role))
        {
            if (await roleManager.RoleExistsAsync(req.Role))
                await userManager.AddToRoleAsync(user, req.Role);
        }

        if (req.ClubIds is { Count: > 0 })
        {
            foreach (var clubId in req.ClubIds)
            {
                db.UserClubAccess.Add(new UserClubAccess { UserId = user.Id, ClubId = clubId });
            }
            await db.SaveChangesAsync();
        }

        var roles = await userManager.GetRolesAsync(user);
        return CreatedAtAction(nameof(GetById), new { id = user.Id }, new
        {
            id = user.Id,
            email = user.Email,
            username = user.UserName,
            displayName = user.DisplayName,
            roles,
            clubs = Array.Empty<object>()
        });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id, CancellationToken ct)
    {
        var user = await userManager.Users
            .Include(u => u.ClubAccess)
            .ThenInclude(a => a.Club)
            .FirstOrDefaultAsync(u => u.Id == id, ct);
        if (user is null) return NotFound();

        var roles = await userManager.GetRolesAsync(user);
        return Ok(new
        {
            id = user.Id,
            email = user.Email,
            username = user.UserName,
            displayName = user.DisplayName,
            roles,
            clubs = user.ClubAccess.Select(a => new { a.ClubId, a.Club.Name })
        });
    }

    [HttpPut("{id}/roles")]
    public async Task<IActionResult> SetRoles(string id, [FromBody] SetRolesRequest req)
    {
        var user = await userManager.FindByIdAsync(id);
        if (user is null) return NotFound();

        var currentRoles = await userManager.GetRolesAsync(user);
        await userManager.RemoveFromRolesAsync(user, currentRoles);

        foreach (var role in req.Roles)
        {
            if (await roleManager.RoleExistsAsync(role))
                await userManager.AddToRoleAsync(user, role);
        }
        return Ok();
    }

    [HttpPut("{id}/clubs")]
    public async Task<IActionResult> SetClubs(string id, [FromBody] SetClubsRequest req, CancellationToken ct)
    {
        var user = await userManager.FindByIdAsync(id);
        if (user is null) return NotFound();

        var existing = await db.UserClubAccess.Where(a => a.UserId == id).ToListAsync(ct);
        db.UserClubAccess.RemoveRange(existing);

        foreach (var clubId in req.ClubIds)
        {
            db.UserClubAccess.Add(new UserClubAccess { UserId = id, ClubId = clubId });
        }
        await db.SaveChangesAsync(ct);
        return Ok();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        var user = await userManager.FindByIdAsync(id);
        if (user is null) return NotFound();

        var result = await userManager.DeleteAsync(user);
        if (!result.Succeeded)
            return BadRequest(new { errors = result.Errors.Select(e => e.Description) });

        return NoContent();
    }

    [HttpGet("roles")]
    public IActionResult GetRoles()
    {
        return Ok(new[] { "SuperAdmin", "Admin", "Cashier" });
    }
}
