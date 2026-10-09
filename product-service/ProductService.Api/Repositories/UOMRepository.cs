using Microsoft.EntityFrameworkCore;
using RestarProduct.Data;
using RestarProduct.Interfaces;
using RestarProduct.Models;

namespace RestarProduct.Repositories;

public sealed class UOMRepository(ProductDbContext dbContext) : IUOMRepository
{
    public async Task<IReadOnlyList<UOM>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.UOMs
            .AsNoTracking()
            .OrderBy(uom => uom.uom_code)
            .ToListAsync(cancellationToken);
    }

    public Task<UOM?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return dbContext.UOMs
            .AsNoTracking()
            .SingleOrDefaultAsync(uom => uom.id == id, cancellationToken);
    }

    public Task<UOM?> FindTrackedAsync(int id, CancellationToken cancellationToken = default)
    {
        return dbContext.UOMs.SingleOrDefaultAsync(uom => uom.id == id, cancellationToken);
    }

    public async Task<UOM> AddAsync(UOM uom, CancellationToken cancellationToken = default)
    {
        await dbContext.UOMs.AddAsync(uom, cancellationToken);
        return uom;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }

    public void Remove(UOM uom)
    {
        dbContext.UOMs.Remove(uom);
    }
}
