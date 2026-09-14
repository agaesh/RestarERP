using Microsoft.EntityFrameworkCore;
using RestarProduct.Enums;
using RestarProduct.Models;

namespace RestarProduct.Data;

public class ProductDbContext(DbContextOptions<ProductDbContext> options) : DbContext(options)
{
    public DbSet<Product> Products => Set<Product>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Product>()
            .Property(product => product.product_type)
            .HasConversion<int>();
    }
}
