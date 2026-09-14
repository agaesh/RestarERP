using Microsoft.EntityFrameworkCore;
using RestarProduct.Data;
using RestarProduct.Interfaces;
using RestarProduct.Models;

namespace RestarProduct.Repositories;

public class ProductRepository(ProductDbContext dbContext) : IProductRepository
{
    public async Task<IReadOnlyList<Product>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        return await dbContext.Products
            .AsNoTracking()
            .OrderBy(product => product.product_name)
            .ToListAsync(cancellationToken);
    }

    public Task<Product?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        return dbContext.Products
            .AsNoTracking()
            .SingleOrDefaultAsync(product => product.id == id, cancellationToken);
    }

    public Task<Product?> FindTrackedAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        return dbContext.Products
            .SingleOrDefaultAsync(product => product.id == id, cancellationToken);
    }

    public async Task<Product> AddAsync(
        Product product,
        CancellationToken cancellationToken = default)
    {
        await dbContext.Products.AddAsync(product, cancellationToken);
        return product;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }

    public void Remove(Product product)
    {
        dbContext.Products.Remove(product);
    }
}
