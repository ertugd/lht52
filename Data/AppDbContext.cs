using lht52.Models;
using Microsoft.EntityFrameworkCore;

namespace lht52.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
    public DbSet<TelemetryEntry> Telemetry { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TelemetryEntry>().HasIndex(t => t.Time);
    }
}