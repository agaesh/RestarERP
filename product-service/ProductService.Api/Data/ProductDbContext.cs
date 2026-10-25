using Microsoft.EntityFrameworkCore;
using RestarProduct.Enums;
using RestarProduct.Models;

namespace RestarProduct.Data;

public class ProductDbContext(DbContextOptions<ProductDbContext> options) : DbContext(options)
{
    public DbSet<Product> Products => Set<Product>();
    public DbSet<RawMaterial> RawMaterials => Set<RawMaterial>();

    public DbSet<UOM> UOMs => Set<UOM>();

    public DbSet<MultiUOM> MultiUOMs => Set<MultiUOM>();
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

        modelBuilder.Entity<UOM>()
            .HasIndex(uom => uom.Code)
            .IsUnique();

        modelBuilder.Entity<UOM>()
            .Property(uom => uom.Code)
            .HasMaxLength(20);

        modelBuilder.Entity<UOM>()
            .Property(uom => uom.Name)
            .HasMaxLength(100);

        modelBuilder.Entity<UOM>()
            .Property(uom => uom.Description)
            .HasMaxLength(500);

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

        modelBuilder.Entity<MultiUOM>()
            .Property(multiUOM => multiUOM.conversionFactor)
            .HasColumnType("decimal(10, 6)");

        modelBuilder.Entity<MultiUOM>()
            .HasIndex(multiUOM => multiUOM.altUOMId);

        modelBuilder.Entity<MultiUOM>()
            .HasIndex(multiUOM => new
            {
                multiUOM.refType,
                multiUOM.refId,
                multiUOM.altUOMId
            })
            .IsUnique()
            .HasDatabaseName("IX_MultiUOM_refType_refId_altUOMId");

        modelBuilder.Entity<MultiUOM>()
            .HasOne(multiUOM => multiUOM.altUOM)
            .WithMany()
            .HasForeignKey(multiUOM => multiUOM.altUOMId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_MultiUOM_UOMSetup_alt_uom_id");

        modelBuilder.Entity<MultiUOM>()
            .HasOne(multiUOM => multiUOM.baseUOM)
            .WithMany()
            .HasForeignKey(multiUOM => multiUOM.baseUOMId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_MultiUOM_UOMSetup_base_uom_id");
    }
}
