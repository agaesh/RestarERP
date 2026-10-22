using RestarProduct.DTOs;
using RestarProduct.Enums;
using RestarProduct.Exceptions;
using RestarProduct.Interfaces;
using RestarProduct.Models;
using System.Globalization;

namespace RestarProduct.Services;

public sealed class MultiUOMService(
    IMultiUOMRepository repository,
    ILogger<MultiUOMService> logger) : IMultiUOMService
{
    //to Understand: This method retrieves a list of MultiUOMDTO objects associated with a specific reference type and reference ID.
    // It first normalizes the reference ID, checks if the reference exists, and then fetches the corresponding MultiUOM records from the repository. The results are logged and returned as a list of DTOs.
    public async Task<IReadOnlyList<MultiUOMDTO>?> GetByReferenceAsync(
        MultiUOMRefType refType,
        int referenceId,
        CancellationToken cancellationToken = default)
    {
        var refId = NormalizeRefId(referenceId);
        if (!await repository.ReferenceExistsAsync(refType, refId, cancellationToken))
        {
            logger.LogInformation(
                "MultiUOM not exist for this reference {ReferenceType} {ReferenceId}",
                refType,
                refId);

            return null;
        }

        var multiUOMs = (await repository.GetByReferenceAsync(
                refType,
                refId,
                cancellationToken))
            .Select(ToDTO)
            .ToList();

        logger.LogInformation(
            "Retrieved {MultiUOMCount} alternate UOMs for {ReferenceType} {ReferenceId}",
            multiUOMs.Count,
            refType,
            refId);
        return multiUOMs;
    }

    //to Understand: This method retrieves a specific MultiUOMDTO by its ID, reference type, and reference ID. It normalizes the reference ID and fetches the corresponding MultiUOM record from the repository. If found, it converts the record to a DTO and returns it; otherwise, it returns null./
    public async Task<MultiUOMDTO?> GetByIdAsync(
        int id,
        MultiUOMRefType refType,
        int referenceId,
        CancellationToken cancellationToken = default)
    {
        var multiUOM = await repository.GetByIdAsync(
            id,
            refType,
            NormalizeRefId(referenceId),
            cancellationToken);
        return multiUOM is null ? null : ToDTO(multiUOM);
    }

    public async Task<MultiUOMDTO> CreateAsync(
        MultiUOMRefType refType,
        int referenceId,
        CreateMultiUOMDTO request,
        CancellationToken cancellationToken = default)
    {  

        //Normalizing the id to ensure consistent formatting for comparison and validation.
        var refId = NormalizeRefId(referenceId);
        //validate that route has been called with correct reference type which are Product or RawMaterial and also validate that the reference id in the route and request body are same.
        ValidateRequestReference(refType, refId, request.ref_type, request.ref_id);
        
        //check if the reference exists in the database, if not throw MultiUOMOwnerNotFoundException.
        if (!await repository.ReferenceExistsAsync(refType, refId, cancellationToken))
        {
            throw new MultiUOMOwnerNotFoundException(refType.ToString(), refId);
        }

        //validate that the provided alternate UOM ID and base UOM ID exist in the database.
        await ValidateUOMIdsAsync(request.alt_uom_id, request.base_uom_id, cancellationToken);
        
        //ensure that the alternate UOM is unique for the given reference type and reference ID, throwing an exception if a duplicate is found.
        await EnsureAlternateUOMIsUniqueAsync(
            refType,
            refId,
            request.alt_uom_id,
            cancellationToken: cancellationToken);

        //Create a new MultiUOM entity with the provided details and add it to the repository, then save changes to the database.
        var multiUOM = new MultiUOM
        {
            refType = refType,
            refId = refId,
            altUOMId = request.alt_uom_id,
            baseUOMId = request.base_uom_id,
            conversionFactor = request.conversion_factor
        };

        //Add the new MultiUOM entity to the repository and save changes to the database.
        await repository.AddAsync(multiUOM, cancellationToken);
        //save changes to the database and log the creation of the new alternate UOM, returning the created MultiUOM as a DTO.
        await repository.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Created alternate UOM {MultiUOMId} for {ReferenceType} {ReferenceId}",
            multiUOM.id,
            multiUOM.refType,
            multiUOM.refId);
        return ToDTO(multiUOM);
    }

    public async Task<bool> UpdateAsync(
        int id,
        MultiUOMRefType refType,
        int referenceId,
        UpdateMultiUOMDTO request,
        CancellationToken cancellationToken = default)
    {

        //Normalizing the reference ID to ensure consistent formatting for comparison and validation.
        var refId = NormalizeRefId(referenceId);

        //validate that route has been called with correct reference type which are Product or RawMaterial and also validate that the reference id in the route and request body are same.
        ValidateRequestReference(refType, refId, request.ref_type, request.ref_id);
        var multiUOM = await repository.FindTrackedAsync(id, refType, refId, cancellationToken);
        if (multiUOM is null)
        {
            logger.LogWarning("Alternate UOM update skipped because record {MultiUOMId} was not found", id);
            return false;
        }
        //check if the reference exists in the database, if not throw MultiUOMOwnerNotFoundException.
        await ValidateUOMIdsAsync(request.alt_uom_id, request.base_uom_id, cancellationToken);
        
        //below method that alternate UOM Is unique during creation and
        // help to avoid prevention for update operation avoid choosing own id that result like duplicate.
        await EnsureAlternateUOMIsUniqueAsync(
            refType,
            refId,
            request.alt_uom_id,
            id,
            cancellationToken);

        multiUOM.altUOMId = request.alt_uom_id;
        multiUOM.baseUOMId = request.base_uom_id;
        multiUOM.conversionFactor = request.conversion_factor;
        multiUOM.UpdatedAt = DateTime.UtcNow;

        await repository.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Updated alternate UOM {MultiUOMId}", id);
        return true;
    }
    
    public async Task<bool> DeleteAsync(
        int id,
        MultiUOMRefType refType,
        int referenceId,
        CancellationToken cancellationToken = default)
    {
        var multiUOM = await repository.FindTrackedAsync(
            id,
            refType,
            NormalizeRefId(referenceId),
            cancellationToken);
        if (multiUOM is null)
        {
            logger.LogWarning("Alternate UOM deletion skipped because record {MultiUOMId} was not found", id);
            return false;
        }

        repository.Remove(multiUOM);
        await repository.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Deleted alternate UOM {MultiUOMId}", id);
        return true;
    }

    //to Understand: This method normalizes the reference ID by converting it to a string using the invariant culture. This ensures that the reference ID is consistently formatted, regardless of the system's locale settings. It helps maintain uniformity in how reference IDs are represented and compared within the application.
    private static string NormalizeRefId(int refId) => refId.ToString(CultureInfo.InvariantCulture);

    //to Understand: This method validates that the reference type and reference ID in the request match those in the route. If they do not match, it throws a MultiUOMReferenceMismatchException. This ensures that the request is consistent with the expected reference context.
    private static void ValidateRequestReference(
        MultiUOMRefType routeRefType,
        string routeRefId,
        MultiUOMRefType requestRefType,
        string requestRefId)
    {
        if (!Enum.IsDefined(requestRefType)
            || requestRefType != routeRefType
            || !string.Equals(requestRefId.Trim(), routeRefId, StringComparison.Ordinal))
        {
            throw new MultiUOMReferenceMismatchException();
        }
    }
 
    //to Understand: This method checks if the provided alternate UOM ID and base UOM ID exist in the database. If either of them does not exist, it throws a MultiUOMSetupNotFoundException. This ensures that the UOM IDs being used in the request are valid and present in the system.
    private async Task ValidateUOMIdsAsync(
        int altUOMId,
        int baseUOMId,
        CancellationToken cancellationToken)
    {
        if (!await repository.UOMExistsAsync(altUOMId, cancellationToken))
        {
            throw new MultiUOMSetupNotFoundException(altUOMId);
        }

        if (!await repository.UOMExistsAsync(baseUOMId, cancellationToken))
        {
            throw new MultiUOMSetupNotFoundException(baseUOMId);
        }
    }

    private async Task EnsureAlternateUOMIsUniqueAsync(
        MultiUOMRefType refType,
        string refId,
        int altUOMId,
        int? excludingId = null,
        CancellationToken cancellationToken = default)
    {
        if (await repository.ExistsForReferenceAsync(
                refType,
                refId,
                altUOMId,
                excludingId,
                cancellationToken))
        {
            throw new MultiUOMAlreadyExistsException(
                altUOMId.ToString(CultureInfo.InvariantCulture),
                refType.ToString(),
                refId);
        }
    }

    private static MultiUOMDTO ToDTO(MultiUOM multiUOM) => new()
    {
        id = multiUOM.id,
        ref_type = multiUOM.refType,
        ref_id = multiUOM.refId,
        alt_uom_id = multiUOM.altUOMId,
        base_uom_id = multiUOM.baseUOMId,
        conversion_factor = multiUOM.conversionFactor
    };
}
