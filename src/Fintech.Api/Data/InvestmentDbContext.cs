using Fintech.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace Fintech.Api.Data;

public sealed class InvestmentDbContext(DbContextOptions<InvestmentDbContext> options) : DbContext(options)
{
    public DbSet<InvestmentRequest> InvestmentRequests => Set<InvestmentRequest>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<InvestmentRequest>(entity =>
        {
            entity.ToTable("investment_requests");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.ClientName).HasMaxLength(120).IsRequired();
            entity.Property(x => x.Instrument).HasMaxLength(32).IsRequired();
            entity.Property(x => x.Amount).HasPrecision(18, 2);
            entity.Property(x => x.Currency).HasMaxLength(3).IsRequired();
            entity.Property(x => x.OperationType).HasConversion<string>().HasMaxLength(16);
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(24);
            entity.Property(x => x.CreatedBy).HasMaxLength(128).IsRequired();
            entity.Property(x => x.CreatedByUserId).HasMaxLength(128).IsRequired();
            entity.Property(x => x.IdempotencyKey).HasMaxLength(128).IsRequired();
            entity.Property(x => x.Version).IsConcurrencyToken();
            entity.HasIndex(x => x.IdempotencyKey).IsUnique();
            entity.HasIndex(x => x.CreatedAt);
            entity.HasIndex(x => x.Status);
        });

        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.ToTable("audit_logs");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.ActorUserId).HasMaxLength(128).IsRequired();
            entity.Property(x => x.ActorUserName).HasMaxLength(128).IsRequired();
            entity.Property(x => x.Action).HasMaxLength(64).IsRequired();
            entity.Property(x => x.EntityType).HasMaxLength(64).IsRequired();
            entity.Property(x => x.MetadataJson).HasColumnType("jsonb");
            entity.Property(x => x.PreviousHash).HasMaxLength(64).IsRequired();
            entity.Property(x => x.PayloadHash).HasMaxLength(64).IsRequired();
            entity.Property(x => x.CurrentHash).HasMaxLength(64).IsRequired();
            entity.HasIndex(x => x.CreatedAt);
            entity.HasIndex(x => x.CurrentHash);
            entity.HasIndex(x => new { x.EntityType, x.EntityId });
        });
    }
}
