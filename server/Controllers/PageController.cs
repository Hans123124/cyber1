using Microsoft.AspNetCore.Mvc;

namespace CyberServer.Controllers;

/// <summary>
/// Serves the login page for the /login route.
/// </summary>
public class PageController : Controller
{
    [HttpGet("/login")]
    public IActionResult Login() => PhysicalFile(
        Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "login.html"),
        "text/html");
}
