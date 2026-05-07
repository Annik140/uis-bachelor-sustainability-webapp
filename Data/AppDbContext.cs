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
            entity.Property(e => e.SustainabilityScore).HasPrecision(5, 2);
            entity.HasIndex(e => e.BrandName);
        });
    }
}
