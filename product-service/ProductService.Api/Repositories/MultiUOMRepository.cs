using Microsoft.EntityFrameworkCore;
using RestarProduct.Data;
using RestarProduct.Enums;
using RestarProduct.Interfaces;
using RestarProduct.Models;

namespace RestarProduct.Repositories;

public sealed class MultiUOMRepository(ProductDbContext dbContext) : IMultiUOMRepository
{
    public async Task<IReadOnlyList<MultiUOM>> GetByReferenceAsync(
        MultiUOMRefType refType,
        string refId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.MultiUOMs
            .AsNoTracking()
            .Where(multiUOM => multiUOM.refType == refType && multiUOM.refId == refId)
            .OrderBy(multiUOM => multiUOM.altUOMId)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ReferenceExistsAsync(
        MultiUOMRefType refType,
        string refId,
        CancellationToken cancellationToken = default)
    {
        if (!int.TryParse(refId, out var referenceId))
        {
            return false;
        }

        return refType switch
        {
            MultiUOMRefType.Product =>
                await dbContext.Products.AnyAsync(product => product.id == referenceId, cancellationToken),
            MultiUOMRefType.RawMaterial =>
                await dbContext.RawMaterials.AnyAsync(material => material.id == referenceId, cancellationToken),
            _ => false
        };
    }

    public Task<MultiUOM?> GetByIdAsync(
        int id,
        MultiUOMRefType refType,
        string refId,
        CancellationToken cancellationToken = default)
    {
        return dbContext.MultiUOMs
            .AsNoTracking()
            .SingleOrDefaultAsync(
                multiUOM => multiUOM.id == id
                    && multiUOM.refType == refType
                    && multiUOM.refId == refId,
                cancellationToken);
    }

    public Task<MultiUOM?> FindTrackedAsync(
        int id,
        MultiUOMRefType refType,
        string refId,
        CancellationToken cancellationToken = default)
    {
        return dbContext.MultiUOMs
            .SingleOrDefaultAsync(
                multiUOM => multiUOM.id == id
                    && multiUOM.refType == refType
                    && multiUOM.refId == refId,
                cancellationToken);
    }

    public Task<bool> ExistsForReferenceAsync(
        MultiUOMRefType refType,
        string refId,
        int altUOMId,
        int? excludingId = null,
        CancellationToken cancellationToken = default)
    {
        return dbContext.MultiUOMs.AnyAsync(
            multiUOM => multiUOM.refType == refType
                && multiUOM.refId == refId
                && multiUOM.altUOMId == altUOMId
                && (!excludingId.HasValue || multiUOM.id != excludingId.Value),
            cancellationToken);
    }

    public Task<bool> UOMExistsAsync(
        int uomId,
        CancellationToken cancellationToken = default)
    {
        return dbContext.UOMs.AnyAsync(uom => uom.Id == uomId, cancellationToken);
    }

    public async Task<MultiUOM> AddAsync(
        MultiUOM multiUOM,
        CancellationToken cancellationToken = default)
    {
        await dbContext.MultiUOMs.AddAsync(multiUOM, cancellationToken);
        return multiUOM;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }

    public void Remove(MultiUOM multiUOM)
    {
        dbContext.MultiUOMs.Remove(multiUOM);
    }
}
