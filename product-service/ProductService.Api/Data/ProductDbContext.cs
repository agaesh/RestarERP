using Microsoft.EntityFrameworkCore;
using RestarProduct.Models;

namespace RestarProduct.Data;

public class ProductDbContext(DbContextOptions<ProductDbContext> options) : DbContext(options)
{
    public DbSet<Product> Products => Set<Product>();

}
