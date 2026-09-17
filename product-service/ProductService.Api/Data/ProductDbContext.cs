using Microsoft.EntityFrameworkCore;
using RestarProduct.Enums;
using RestarProduct.Models;

namespace RestarProduct.Data;

public class ProductDbContext(DbContextOptions<ProductDbContext> options) : DbContext(options)
{
    public DbSet<Product> Products => Set<Product>();
    public DbSet<RawMaterial> RawMaterials => Set<RawMaterial>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Product>()
            .Property(product => product.product_type)
            .HasConversion<string>();

        modelBuilder.Entity<RawMaterial>()
            .HasIndex(material => material.material_code)
            .IsUnique();

        modelBuilder.Entity<RawMaterial>()
            .HasIndex(material => new { material.is_active, material.material_name });

        modelBuilder.Entity<RawMaterial>()
            .Property(material => material.material_code)
            .HasMaxLength(50);

        modelBuilder.Entity<RawMaterial>()
            .Property(material => material.material_name)
            .HasMaxLength(200);

        modelBuilder.Entity<RawMaterial>()
            .Property(material => material.material_desc)
            .HasMaxLength(1000);

        modelBuilder.Entity<RawMaterial>()
            .Property(material => material.uom)
            .HasMaxLength(50);
    }
}
