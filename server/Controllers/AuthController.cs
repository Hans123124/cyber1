using CyberServer.Domain;
using CyberServer.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace CyberServer.Controllers;

[ApiController]
[Route("api/account")]
public class AuthController(
    SignInManager<ApplicationUser> signInManager,
    UserManager<ApplicationUser> userManager) : ControllerBase
{
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest req)
    {
        var user = await userManager.FindByEmailAsync(req.Email)
                ?? await userManager.FindByNameAsync(req.Email);
        if (user is null)
            return Unauthorized(new { error = "Invalid credentials." });

        var result = await signInManager.PasswordSignInAsync(user, req.Password, req.RememberMe, false);
        if (!result.Succeeded)
            return Unauthorized(new { error = "Invalid credentials." });

        var roles = await userManager.GetRolesAsync(user);
        return Ok(new
        {
            id = user.Id,
            email = user.Email,
            username = user.UserName,
            displayName = user.DisplayName,
            roles
        });
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        await signInManager.SignOutAsync();
        return Ok();
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> Me()
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null) return Unauthorized();
        var roles = await userManager.GetRolesAsync(user);
        return Ok(new
        {
            id = user.Id,
            email = user.Email,
            username = user.UserName,
            displayName = user.DisplayName,
            roles
        });
    }
}
