using CyberServer.Domain;
using Microsoft.EntityFrameworkCore;

namespace CyberServer.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Workstation> Workstations => Set<Workstation>();
    public DbSet<AgentHeartbeat> AgentHeartbeats => Set<AgentHeartbeat>();
    public DbSet<CommandLog> CommandLogs => Set<CommandLog>();
    public DbSet<ExternalReceipt> ExternalReceipts => Set<ExternalReceipt>();

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
    }
}
