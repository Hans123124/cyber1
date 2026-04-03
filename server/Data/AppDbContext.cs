using CyberServer.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CyberServer.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<Workstation> Workstations => Set<Workstation>();
    public DbSet<AgentHeartbeat> AgentHeartbeats => Set<AgentHeartbeat>();
    public DbSet<CommandLog> CommandLogs => Set<CommandLog>();
    public DbSet<ExternalReceipt> ExternalReceipts => Set<ExternalReceipt>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<TariffPlan> TariffPlans => Set<TariffPlan>();
    public DbSet<Sale> Sales => Set<Sale>();
    public DbSet<Session> Sessions => Set<Session>();
    public DbSet<Subscription> Subscriptions => Set<Subscription>();
    public DbSet<Club> Clubs => Set<Club>();
    public DbSet<ClubSettings> ClubSettings => Set<ClubSettings>();
    public DbSet<MapLayout> MapLayouts => Set<MapLayout>();
    public DbSet<MapItem> MapItems => Set<MapItem>();
    public DbSet<Zone> Zones => Set<Zone>();
    public DbSet<UserClubAccess> UserClubAccess => Set<UserClubAccess>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Workstation>(b =>
        {
            b.HasKey(w => w.Id);
            b.HasIndex(w => w.MachineFingerprint).IsUnique();
            b.Property(w => w.Name).HasMaxLength(128);
            b.Property(w => w.AgentVersion).HasMaxLength(32);
            b.Property(w => w.OsVersion).HasMaxLength(128);
            b.Property(w => w.IpAddress).HasMaxLength(64);
            b.Property(w => w.SecretHash).HasMaxLength(256);
            b.Property(w => w.MeshCentralDeviceId).HasMaxLength(256);
            b.Property(w => w.FogHostId).HasMaxLength(128);
            b.Property(w => w.ImageGroup).HasMaxLength(128);
            b.HasOne(w => w.Club)
             .WithMany(c => c.Workstations)
             .HasForeignKey(w => w.ClubId)
             .OnDelete(DeleteBehavior.SetNull);
            b.HasIndex(w => w.ClubId);
        });

        modelBuilder.Entity<AgentHeartbeat>(b =>
        {
            b.HasKey(h => h.Id);
            b.HasOne(h => h.Workstation)
             .WithMany(w => w.Heartbeats)
             .HasForeignKey(h => h.WorkstationId)
             .OnDelete(DeleteBehavior.Cascade);
            b.Property(h => h.AgentVersion).HasMaxLength(32);
            b.Property(h => h.IpAddress).HasMaxLength(64);
        });

        modelBuilder.Entity<CommandLog>(b =>
        {
            b.HasKey(c => c.Id);
            b.HasOne(c => c.Workstation)
             .WithMany(w => w.Commands)
             .HasForeignKey(c => c.WorkstationId)
             .OnDelete(DeleteBehavior.Cascade);
            b.Property(c => c.IssuedBy).HasMaxLength(128);
            b.Property(c => c.Notes).HasMaxLength(512);
        });

        modelBuilder.Entity<ExternalReceipt>(b =>
        {
            b.HasKey(r => r.Id);
            b.Property(r => r.Source).HasMaxLength(64);
            b.Property(r => r.ReceiptNo).HasMaxLength(128);
            b.Property(r => r.Currency).HasMaxLength(8);
            b.Property(r => r.Amount).HasPrecision(18, 4);
            b.Property(r => r.SessionId).HasMaxLength(256);
            b.Property(r => r.RawJson).HasColumnType("longtext");
        });

        modelBuilder.Entity<Customer>(b =>
        {
            b.HasKey(c => c.Id);
            b.Property(c => c.Username).HasMaxLength(128);
            b.Property(c => c.Phone).HasMaxLength(32);
            b.HasIndex(c => c.Username);
            b.HasIndex(c => c.Phone);
        });

        modelBuilder.Entity<TariffPlan>(b =>
        {
            b.HasKey(t => t.Id);
            b.Property(t => t.Name).HasMaxLength(128);
            b.Property(t => t.Price).HasPrecision(18, 4);
            b.Property(t => t.HourlyRateMdl).HasPrecision(18, 4);
            b.HasOne(t => t.Club)
             .WithMany()
             .HasForeignKey(t => t.ClubId)
             .OnDelete(DeleteBehavior.SetNull);
            b.HasIndex(t => t.ClubId);
        });

        modelBuilder.Entity<Sale>(b =>
        {
            b.HasKey(s => s.Id);
            b.Property(s => s.Amount).HasPrecision(18, 4);
            b.Property(s => s.Currency).HasMaxLength(8);
            b.Property(s => s.OperatorName).HasMaxLength(128);
        });

        modelBuilder.Entity<Session>(b =>
        {
            b.HasKey(s => s.Id);
            b.HasOne(s => s.Workstation)
             .WithMany()
             .HasForeignKey(s => s.WorkstationId)
             .OnDelete(DeleteBehavior.Restrict);
            b.HasOne(s => s.Customer)
             .WithMany(c => c.Sessions)
             .HasForeignKey(s => s.CustomerId)
             .OnDelete(DeleteBehavior.SetNull);
            b.HasOne(s => s.TariffPlan)
             .WithMany()
             .HasForeignKey(s => s.TariffPlanId)
             .OnDelete(DeleteBehavior.Restrict);
            b.HasOne(s => s.Sale)
             .WithMany(p => p.Sessions)
             .HasForeignKey(s => s.SaleId)
             .OnDelete(DeleteBehavior.Restrict);
            b.Property(s => s.GuestName).HasMaxLength(128);
            b.HasIndex(s => s.WorkstationId);
            // Composite index for expiry service query (Status, EndsAt)
            b.HasIndex(s => new { s.Status, s.EndsAt });
        });

        modelBuilder.Entity<Subscription>(b =>
        {
            b.HasKey(s => s.Id);
            b.HasOne(s => s.Customer)
             .WithMany(c => c.Subscriptions)
             .HasForeignKey(s => s.CustomerId)
             .OnDelete(DeleteBehavior.Cascade);
            b.HasOne(s => s.TariffPlan)
             .WithMany()
             .HasForeignKey(s => s.TariffPlanId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ── Club entities ──────────────────────────────────────────────────────
        modelBuilder.Entity<Club>(b =>
        {
            b.HasKey(c => c.Id);
            b.Property(c => c.Name).HasMaxLength(128);
            b.HasIndex(c => c.Name);
        });

        modelBuilder.Entity<ClubSettings>(b =>
        {
            b.HasKey(s => s.Id);
            b.HasOne(s => s.Club)
             .WithOne(c => c.Settings)
             .HasForeignKey<ClubSettings>(s => s.ClubId)
             .OnDelete(DeleteBehavior.Cascade);
            b.HasIndex(s => s.ClubId).IsUnique();
        });

        modelBuilder.Entity<MapLayout>(b =>
        {
            b.HasKey(l => l.Id);
            b.HasOne(l => l.Club)
             .WithMany(c => c.Layouts)
             .HasForeignKey(l => l.ClubId)
             .OnDelete(DeleteBehavior.Cascade);
            b.Property(l => l.Name).HasMaxLength(128);
            b.HasIndex(l => l.ClubId);
        });

        modelBuilder.Entity<Zone>(b =>
        {
            b.HasKey(z => z.Id);
            b.HasOne(z => z.Layout)
             .WithMany(l => l.Zones)
             .HasForeignKey(z => z.LayoutId)
             .OnDelete(DeleteBehavior.Cascade);
            b.Property(z => z.Name).HasMaxLength(128);
            b.Property(z => z.Color).HasMaxLength(32);
            b.HasIndex(z => z.LayoutId);
        });

        modelBuilder.Entity<MapItem>(b =>
        {
            b.HasKey(i => i.Id);
            b.HasOne(i => i.Layout)
             .WithMany(l => l.Items)
             .HasForeignKey(i => i.LayoutId)
             .OnDelete(DeleteBehavior.Cascade);
            b.HasOne(i => i.Workstation)
             .WithMany()
             .HasForeignKey(i => i.WorkstationId)
             .OnDelete(DeleteBehavior.SetNull);
            b.HasOne(i => i.Zone)
             .WithMany(z => z.Items)
             .HasForeignKey(i => i.ZoneId)
             .OnDelete(DeleteBehavior.SetNull);
            b.Property(i => i.Label).HasMaxLength(128);
            b.Property(i => i.MetaJson).HasColumnType("longtext");
            b.HasIndex(i => i.LayoutId);
            b.HasIndex(i => i.WorkstationId);
        });

        modelBuilder.Entity<UserClubAccess>(b =>
        {
            b.HasKey(u => u.Id);
            b.HasOne(u => u.User)
             .WithMany(u => u.ClubAccess)
             .HasForeignKey(u => u.UserId)
             .OnDelete(DeleteBehavior.Cascade);
            b.HasOne(u => u.Club)
             .WithMany()
             .HasForeignKey(u => u.ClubId)
             .OnDelete(DeleteBehavior.Cascade);
            b.HasIndex(u => new { u.UserId, u.ClubId }).IsUnique();
        });
    }
}
