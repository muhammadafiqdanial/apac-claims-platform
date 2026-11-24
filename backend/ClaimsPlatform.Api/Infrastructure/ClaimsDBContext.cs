using ClaimsPlatform.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace ClaimsPlatform.Api.Infrastructure;

public class ClaimsDbContext : DbContext
{
    public ClaimsDbContext(DbContextOptions<ClaimsDbContext> options) : base(options) { }

    public DbSet<Claim> Claims => Set<Claim>();
    public DbSet<ClaimAuditEntry> AuditEntries => Set<ClaimAuditEntry>();
    public DbSet<ExposureSummary> ExposureSummaries => Set<ExposureSummary>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Claim>(b =>
        {
            b.HasKey(c => c.Id);
            b.HasIndex(c => c.ClaimNumber).IsUnique();
            b.Property(c => c.Market).HasConversion<string>();
            b.Property(c => c.Type).HasConversion<string>();
            b.Property(c => c.Status).HasConversion<string>();
            b.HasMany(c => c.Timeline).WithOne().HasForeignKey(a => a.ClaimId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ClaimAuditEntry>(b =>
        {
            b.HasKey(a => a.Id);
            b.Property(a => a.Status).HasConversion<string>();
        });

        modelBuilder.Entity<ExposureSummary>(b =>
        {
            b.HasKey(e => new { e.Market, e.Type });
            b.Property(e => e.Market).HasConversion<string>();
            b.Property(e => e.Type).HasConversion<string>();
        });
    }
}