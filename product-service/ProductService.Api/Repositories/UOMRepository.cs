using Microsoft.EntityFrameworkCore;
using RestarProduct.Data;
using RestarProduct.Interfaces;
using RestarProduct.Models;

namespace RestarProduct.Repositories;

public sealed class UOMRepository(ProductDbContext dbContext) : IUOMRepository
{
    public async Task<IReadOnlyList<UOM>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.UOMs
            .AsNoTracking()
            .Where(uom => !uom.IsDelete)
            .OrderBy(uom => uom.Code)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<UOM>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.UOMs
            .AsNoTracking()
            .Where(uom => !uom.IsDelete)
            .OrderBy(uom => uom.Code)
            .ToListAsync(cancellationToken);
    }

    public Task<UOM?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return dbContext.UOMs
            .AsNoTracking()
            .SingleOrDefaultAsync(uom => uom.Id == id && !uom.IsDelete, cancellationToken);
    }

    public Task<UOM?> FindTrackedAsync(int id, CancellationToken cancellationToken = default)
    {
        return dbContext.UOMs
            .SingleOrDefaultAsync(uom => uom.Id == id && !uom.IsDelete, cancellationToken);
    }

    public Task<bool> CodeExistsAsync(
        string code,
        int? excludingId = null,
        CancellationToken cancellationToken = default)
    {
        return dbContext.UOMs.AnyAsync(
            uom => uom.Code.ToUpper() == code && (!excludingId.HasValue || uom.Id != excludingId.Value),
            cancellationToken);
    }

    public Task<bool> IsReferencedAsync(int id, CancellationToken cancellationToken = default)
    {
        return dbContext.MultiUOMs.AnyAsync(
            multiUOM => multiUOM.altUOMId == id || multiUOM.baseUOMId == id,
            cancellationToken);
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
}
