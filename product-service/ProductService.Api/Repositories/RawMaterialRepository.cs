using Microsoft.EntityFrameworkCore;
using RestarProduct.Data;
using RestarProduct.Interfaces;
using RestarProduct.Models;

namespace RestarProduct.Repositories;

public class RawMaterialRepository(ProductDbContext dbContext) : IRawMaterialRepository
{
    public async Task<IReadOnlyList<RawMaterial>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.RawMaterials
            .AsNoTracking()
            .OrderBy(material => material.material_name)
            .ToListAsync(cancellationToken);
    }

    public Task<RawMaterial?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return dbContext.RawMaterials
            .AsNoTracking()
            .SingleOrDefaultAsync(material => material.id == id, cancellationToken);
    }

    public Task<RawMaterial?> FindTrackedAsync(int id, CancellationToken cancellationToken = default)
    {
        return dbContext.RawMaterials
            .SingleOrDefaultAsync(material => material.id == id, cancellationToken);
    }

    public async Task<RawMaterial> AddAsync(RawMaterial rawMaterial, CancellationToken cancellationToken = default)
    {
        await dbContext.RawMaterials.AddAsync(rawMaterial, cancellationToken);
        return rawMaterial;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }

    public void Remove(RawMaterial rawMaterial)
    {
        dbContext.RawMaterials.Remove(rawMaterial);
    }
}
