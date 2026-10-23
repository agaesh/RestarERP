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

    /// <summary>
    /// Finds an alternate UOM record by its ID and owner, without tracking it for changes.
    /// Returns <see langword="null"/> if no record matches all three values.
    /// </summary>
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

    // <summary>
    /// Gets the base UOM code for the given reference type and reference ID, or null if not found.
    /// </summary>
    /// <param name="refType">The reference type (Product or RawMaterial).</param>
    /// <param name="refId">The reference ID as a string.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The base UOM code as a string, or null if not found.</returns>
    
    public async Task<int?> GetOwnerBaseUOMIdAsync(
        MultiUOMRefType refType,
        int refId,
        CancellationToken cancellationToken = default)
    {
        //getting base uom code based on reference type and reference id, then getting base uom id based on base uom code
        var baseUomCode = refType switch
        {
            MultiUOMRefType.Product => await dbContext.Products
                .Where(product => product.id == refId)
                .Select(product => product.uom)
                .SingleOrDefaultAsync(cancellationToken),
            MultiUOMRefType.RawMaterial => await dbContext.RawMaterials
                .Where(material => material.id == refId)
                .Select(material => material.uom)
                .SingleOrDefaultAsync(cancellationToken),
            _ => null
        };

        if (baseUomCode == null) return null;
       //getting base uom id based on base uom code
       var baseUomId = await dbContext.UOMs
            .Where(uom => uom.Code == baseUomCode)
            .Select(uom => (int?)uom.Id)
            .SingleOrDefaultAsync(cancellationToken);

        return baseUomId;
    }

    ///<summary>
    /// Gets the UOM code for the given UOM ID, or null if not found
    ///     </summary>
    /// <param name="uomID">The UOM ID.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The UOM code, or null if not found.</returns>

    public async Task<string?> GetOwnerBaseUOMCodeAsync(
        MultiUOMRefType refType,
        int refId,
        CancellationToken cancellationToken = default
    )
    {
        // Get the base UOM code based on the reference type and reference ID
        var baseUomCode = refType switch
        {
            MultiUOMRefType.Product => await dbContext.Products
                .Where(product => product.id == refId)
                .Select(product => product.uom)
                .SingleOrDefaultAsync(cancellationToken),
            MultiUOMRefType.RawMaterial => await dbContext.RawMaterials
                .Where(material => material.id == refId)
                .Select(material => material.uom)
                .SingleOrDefaultAsync(cancellationToken),
            _ => null
        };
        
        return baseUomCode;
    }

        /// <summary>
    /// Gets the MultiUOM ID for the given UOM code, or null if not
    /// not found.  
    /// </summary>
    /// <param name="code"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    
    public async Task<int?> GetMultiUOMId(string code, CancellationToken cancellationToken = default)
    {
        // Get the MultiUOM ID based on the UOM code
        var multiuom = await dbContext.MultiUOMs.AsNoTracking()
            .Where(m => m.altUOMId == dbContext.UOMs
                .Where(u => u.Code == code)
                .Select(u => u.Id)
                .SingleOrDefault())
            .FirstOrDefaultAsync(cancellationToken);

        return multiuom?.id;
    }
    
    /// <summary>
    /// Gets the UOM entity for the given UOM ID, or null if not found
    /// </summary>
    /// <param name="uomId">The UOM ID.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The UOM entity, or null if not found.</returns>
    
    public async Task<string?> GetMultiUOMCodeAsync(int uomID, CancellationToken cancellationToken = default)
    {
      
        var multiuom = await dbContext.MultiUOMs.AsNoTracking()
            .SingleOrDefaultAsync(u => u.altUOMId == uomID, cancellationToken);
        
        if (multiuom is null) return null;

        var uom = await dbContext.UOMs.AsNoTracking()
            .Where(u => u.Id == multiuom.altUOMId)
            .Select(u => u.Code)
            .SingleOrDefaultAsync(cancellationToken);
        
        return uom;
    }

    /// <summary>
    /// Gets the conversion factor for the given MultiUOM ID, or null if not found.
    /// </summary>
    /// <param name="id"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>

    public async Task<Decimal?> GetConversionFactor(int id, CancellationToken cancellationToken = default)
    {
        var multiuom = await dbContext.MultiUOMs.AsNoTracking()
            .Where(m => m.id == id)
            .SingleOrDefaultAsync(cancellationToken);

       if ((multiuom?.conversionFactor ?? 0) <= 0) return null;

        return multiuom?.conversionFactor;
    }
}
