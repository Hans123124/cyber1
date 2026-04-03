using CyberServer.Data;
using CyberServer.Domain;
using CyberServer.Hubs;
using CyberServer.Middleware;
using CyberServer.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ── Database ─────────────────────────────────────────────────────────────────
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not set.");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(connectionString,
        new MySqlServerVersion(new Version(8, 0, 36)),
        mysqlOptions => mysqlOptions.EnableRetryOnFailure(3)));

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
    c.AddSecurityDefinition("AdminKey", new()
    {
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Name = "X-Admin-Key",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Description = "Admin API key for protected endpoints."
    });
});

var app = builder.Build();

// ── Auto-migrate on startup ───────────────────────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();

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
        // Ensure every club has at least one layout
        var clubsWithoutLayout = db.Clubs
            .Where(c => !db.MapLayouts.Any(l => l.ClubId == c.Id))
            .ToList();
        foreach (var club in clubsWithoutLayout)
            db.MapLayouts.Add(new MapLayout { ClubId = club.Id, Name = "Main Hall" });

        // Ensure every club has settings
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

app.UseMiddleware<AdminKeyMiddleware>();
app.MapControllers();
app.MapHub<AgentHub>("/hubs/agent");

app.Run();

