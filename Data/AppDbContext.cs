using Microsoft.EntityFrameworkCore;
using uis_bachelor_sustainability_webapp.Models;

namespace uis_bachelor_sustainability_webapp.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<AdminUser> AdminUsers => Set<AdminUser>();
    public DbSet<ClothingBrand> ClothingBrands => Set<ClothingBrand>();
    public DbSet<BrandEvidenceSource> BrandEvidenceSources => Set<BrandEvidenceSource>();
    public DbSet<BrandCriterionItem> BrandCriterionItems => Set<BrandCriterionItem>();
    public DbSet<BrandCertification> BrandCertifications => Set<BrandCertification>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AdminUser>(entity =>
        {
            entity.Property(e => e.Username).HasMaxLength(120).IsRequired();
            entity.Property(e => e.NormalizedUsername).HasMaxLength(120).IsRequired();
            entity.Property(e => e.PasswordHash).HasMaxLength(1000).IsRequired();
            entity.Property(e => e.CreatedAtUtc).HasDefaultValueSql("NOW()");
            entity.Property(e => e.UpdatedAtUtc).HasDefaultValueSql("NOW()");
            entity.HasIndex(e => e.NormalizedUsername).IsUnique();
        });

        modelBuilder.Entity<ClothingBrand>(entity =>
        {
            entity.Property(e => e.BrandName).HasMaxLength(200).IsRequired();
            entity.Property(e => e.LogoPath).HasMaxLength(300);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.ProsSummary).HasMaxLength(1000);
            entity.Property(e => e.ConsSummary).HasMaxLength(1000);
            entity.Property(e => e.MaterialSustainabilityScore).HasPrecision(4, 1);
            entity.Property(e => e.LaborPracticesScore).HasPrecision(4, 1);
            entity.Property(e => e.CarbonFootprintScore).HasPrecision(4, 1);
            entity.Property(e => e.ProductLongevityScore).HasPrecision(4, 1);
            entity.Property(e => e.SustainabilityScore).HasPrecision(5, 2);
            entity.Property(e => e.TransparencyScore).HasPrecision(4, 1);
            entity.Property(e => e.UpdatedAtUtc).HasDefaultValueSql("NOW()");
            entity.HasIndex(e => e.BrandName);

            entity.HasMany(e => e.EvidenceSources)
                .WithOne(e => e.ClothingBrand)
                .HasForeignKey(e => e.ClothingBrandId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.CriteriaItems)
                .WithOne(e => e.ClothingBrand)
                .HasForeignKey(e => e.ClothingBrandId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.Certifications)
                .WithOne(e => e.ClothingBrand)
                .HasForeignKey(e => e.ClothingBrandId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<BrandEvidenceSource>(entity =>
        {
            entity.Property(e => e.SourceTitle).HasMaxLength(250).IsRequired();
            entity.Property(e => e.SourceUrl).HasMaxLength(1000).IsRequired();
            entity.Property(e => e.CreatedAtUtc).HasDefaultValueSql("NOW()");
            entity.HasIndex(e => e.ClothingBrandId);
        });

        modelBuilder.Entity<BrandCriterionItem>(entity =>
        {
            entity.Property(e => e.Category).HasMaxLength(80).IsRequired();
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Unit).HasMaxLength(50);
            entity.Property(e => e.Weight).HasPrecision(4, 2).HasDefaultValue(1m);
            entity.Property(e => e.Notes).HasMaxLength(1000);
            entity.Property(e => e.CreatedAtUtc).HasDefaultValueSql("NOW()");
            entity.HasIndex(e => e.ClothingBrandId);
            entity.HasIndex(e => new { e.ClothingBrandId, e.Category });
        });

        modelBuilder.Entity<BrandCertification>(entity =>
        {
            entity.Property(e => e.Name).HasMaxLength(120).IsRequired();
            entity.Property(e => e.CreatedAtUtc).HasDefaultValueSql("NOW()");
            entity.HasIndex(e => e.ClothingBrandId);
            entity.HasIndex(e => new { e.ClothingBrandId, e.Name }).IsUnique();
        });
    }
}
