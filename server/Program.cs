using CyberServer.Data;
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
}

// ── Middleware pipeline ───────────────────────────────────────────────────────
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<AdminKeyMiddleware>();
app.MapControllers();
app.MapHub<AgentHub>("/hubs/agent");

app.Run();
