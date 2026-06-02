using Microsoft.EntityFrameworkCore;
using uis_bachelor_sustainability_webapp.Models;

namespace uis_bachelor_sustainability_webapp.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<ClothingBrand> ClothingBrands => Set<ClothingBrand>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ClothingBrand>(entity =>
        {
            entity.Property(e => e.BrandName).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Category).HasMaxLength(120);
            entity.Property(e => e.MaterialSustainabilityScore).HasPrecision(4, 1);
            entity.Property(e => e.LaborPracticesScore).HasPrecision(4, 1);
            entity.Property(e => e.CarbonFootprintScore).HasPrecision(4, 1);
            entity.Property(e => e.ProductLongevityScore).HasPrecision(4, 1);
            entity.Property(e => e.EvidenceSourceCount).HasDefaultValue(0);
            entity.Property(e => e.SustainabilityScore).HasPrecision(5, 2);
            entity.Property(e => e.TransparencyScore).HasPrecision(4, 1);
            entity.Property(e => e.UpdatedAtUtc).HasDefaultValueSql("NOW()");
            entity.HasIndex(e => e.BrandName);
        });
    }
}
