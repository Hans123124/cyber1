using CyberServer.Data;
using CyberServer.Domain;
using CyberServer.Hubs;
using CyberServer.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ── Database ─────────────────────────────────────────────────────────────────
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not set.");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(connectionString,
        new MySqlServerVersion(new Version(8, 0, 36)),
        mysqlOptions => mysqlOptions.EnableRetryOnFailure(3)));

// ── Identity ──────────────────────────────────────────────────────────────────
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = false;
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireLowercase = false;
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/login";
    options.LogoutPath = "/account/logout";
    options.AccessDeniedPath = "/login";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.SlidingExpiration = true;
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.Events.OnRedirectToLogin = ctx =>
    {
        if (ctx.Request.Path.StartsWithSegments("/api"))
        {
            ctx.Response.StatusCode = 401;
        }
        else
        {
            ctx.Response.Redirect(ctx.RedirectUri);
        }
        return Task.CompletedTask;
    };
    options.Events.OnRedirectToAccessDenied = ctx =>
    {
        if (ctx.Request.Path.StartsWithSegments("/api"))
        {
            ctx.Response.StatusCode = 403;
        }
        else
        {
            ctx.Response.Redirect(ctx.RedirectUri);
        }
        return Task.CompletedTask;
    };
});

// ── Services ──────────────────────────────────────────────────────────────────
builder.Services.AddScoped<IWorkstationService, WorkstationService>();
builder.Services.AddScoped<ICommandService, CommandService>();
builder.Services.AddScoped<ITariffService, TariffService>();
builder.Services.AddScoped<ICustomerService, CustomerService>();
builder.Services.AddScoped<ISessionService, SessionService>();
builder.Services.AddHostedService<SessionExpiryService>();

// ── SignalR ───────────────────────────────────────────────────────────────────
builder.Services.AddSignalR();

// ── API ───────────────────────────────────────────────────────────────────────
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "CyberServer API", Version = "v1" });
});

var app = builder.Build();

// ── Auto-migrate on startup ───────────────────────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();

    // Seed roles
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    foreach (var role in new[] { "SuperAdmin", "Admin", "Cashier" })
    {
        if (!await roleManager.RoleExistsAsync(role))
            await roleManager.CreateAsync(new IdentityRole(role));
    }

    // Seed SuperAdmin from environment / configuration
    var cfg = scope.ServiceProvider.GetRequiredService<IConfiguration>();
    var saEmail = cfg["SUPERADMIN_EMAIL"] ?? Environment.GetEnvironmentVariable("SUPERADMIN_EMAIL");
    var saPassword = cfg["SUPERADMIN_PASSWORD"] ?? Environment.GetEnvironmentVariable("SUPERADMIN_PASSWORD");
    var saUsername = cfg["SUPERADMIN_USERNAME"] ?? Environment.GetEnvironmentVariable("SUPERADMIN_USERNAME") ?? "superadmin";

    if (!string.IsNullOrWhiteSpace(saEmail) && !string.IsNullOrWhiteSpace(saPassword))
    {
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var existing = await userManager.FindByEmailAsync(saEmail);
        if (existing is null)
        {
            var superAdmin = new ApplicationUser
            {
                UserName = saUsername,
                Email = saEmail,
                DisplayName = "Super Admin",
                EmailConfirmed = true
            };
            var result = await userManager.CreateAsync(superAdmin, saPassword);
            if (result.Succeeded)
                await userManager.AddToRoleAsync(superAdmin, "SuperAdmin");
        }
    }

    // Seed default club + layout if none exist
    if (!db.Clubs.Any())
    {
        var defaultClub = new Club { Name = "Main Club" };
        db.Clubs.Add(defaultClub);
        db.ClubSettings.Add(new ClubSettings { ClubId = defaultClub.Id });
        db.MapLayouts.Add(new MapLayout { ClubId = defaultClub.Id, Name = "Main Hall" });
        db.SaveChanges();
    }
    else
    {
        var clubsWithoutLayout = db.Clubs
            .Where(c => !db.MapLayouts.Any(l => l.ClubId == c.Id))
            .ToList();
        foreach (var club in clubsWithoutLayout)
            db.MapLayouts.Add(new MapLayout { ClubId = club.Id, Name = "Main Hall" });

        var clubsWithoutSettings = db.Clubs
            .Where(c => !db.ClubSettings.Any(s => s.ClubId == c.Id))
            .ToList();
        foreach (var club in clubsWithoutSettings)
            db.ClubSettings.Add(new ClubSettings { ClubId = club.Id });

        if (clubsWithoutLayout.Count > 0 || clubsWithoutSettings.Count > 0)
            db.SaveChanges();
    }
}

// ── Middleware pipeline ───────────────────────────────────────────────────────
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseDefaultFiles();
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<AgentHub>("/hubs/agent");

app.Run();
