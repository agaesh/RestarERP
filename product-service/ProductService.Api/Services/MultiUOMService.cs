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
    /// <summary>
    /// Retrieves the alternate UOMs for an owner, or <see langword="null"/> if the owner does not exist.
    /// </summary>
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

    /// <summary>
    /// Retrieves one alternate UOM by record ID and owner, or <see langword="null"/> if no matching record exists.
    /// </summary>
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
    /// <summary>
    /// Resolves the conversion factor between two UOMs through the owner's primary UOM.
    /// </summary>
    public async Task<decimal?> GetConversionFactorAsync(
        MultiUOMRefType refType,
        int referenceId,
        int fromUOMId,
        int toUOMId,
        CancellationToken cancellationToken = default)
    {
        var refId = NormalizeRefId(referenceId);
        if (!await repository.ReferenceExistsAsync(refType, refId, cancellationToken)
            || !await repository.UOMExistsAsync(fromUOMId, cancellationToken)
            || !await repository.UOMExistsAsync(toUOMId, cancellationToken))
        {
            return null;
        }

        var primaryUOMId = await repository.GetOwnerBaseUOMIdAsync(
            refType,
            referenceId,
            cancellationToken);
        if (primaryUOMId is null)
        {
            throw new MultiUOMInvalidConfigurationException(
                "The owner must have a valid primary UOM configured before conversions can be resolved.");
        }

        var multiUOMs = await repository.GetByReferenceAsync(refType, refId, cancellationToken);
        var conversions = multiUOMs.ToDictionary(multiUOM => multiUOM.altUOMId);
        var fromFactor = GetFactorToPrimary(fromUOMId, primaryUOMId.Value, conversions);
        var toFactor = GetFactorToPrimary(toUOMId, primaryUOMId.Value, conversions);
        if (fromFactor is null || toFactor is null)
        {
            return null;
        }

        return fromFactor.Value / toFactor.Value;
    }

    /// <summary>
    /// Validates and creates an alternate UOM conversion for the specified owner.
    /// </summary>
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
        await ValidateConversionPathAsync(
            refType,
            refId,
            request.alt_uom_id,
            request.base_uom_id,
            request.conversion_factor,
            cancellationToken: cancellationToken);
        
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

    /// <summary>
    /// Validates and updates an existing alternate UOM conversion for the specified owner.
    /// </summary>
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
        await ValidateConversionPathAsync(
            refType,
            refId,
            request.alt_uom_id,
            request.base_uom_id,
            request.conversion_factor,
            id,
            cancellationToken);
        
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
    
    /// <summary>
    /// Deletes an alternate UOM conversion and returns whether a matching record was found.
    /// </summary>
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

    /// <summary>
    /// Converts a numeric owner ID to its culture-independent string representation.
    /// </summary>
    private static string NormalizeRefId(int refId) => refId.ToString(CultureInfo.InvariantCulture);

    /// <summary>
    /// Ensures the request owner matches the owner identified by the route.
    /// </summary>
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
 
    /// <summary>
    /// Ensures both the alternate and base UOM IDs refer to configured UOMs.
    /// </summary>
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

    /// <summary>
    /// Verifies that the proposed alternate UOM reaches the owner's primary UOM without a cycle.
    /// </summary>
    private async Task ValidateConversionPathAsync(
        MultiUOMRefType refType,
        string refId,
        int altUOMId,
        int baseUOMId,
        decimal? conversionFactor,
        int? excludingId = null,
        CancellationToken cancellationToken = default)
    {
        if (altUOMId == baseUOMId)
        {
            throw new MultiUOMInvalidConfigurationException(
                "The base UOM and alternate UOM must be different.");
        }

        if (conversionFactor is not > 0)
        {
            throw new MultiUOMInvalidConfigurationException(
                "The conversion factor must be greater than zero.");
        }

        if (!int.TryParse(refId, NumberStyles.None, CultureInfo.InvariantCulture, out var referenceId))
        {
            throw new MultiUOMOwnerNotFoundException(refType.ToString(), refId);
        }

        var primaryUOMId = await repository.GetOwnerBaseUOMIdAsync(
            refType,
            referenceId,
            cancellationToken);
        if (primaryUOMId is null)
        {
            throw new MultiUOMInvalidConfigurationException(
                "The owner must have a valid primary UOM configured before conversions can be added.");
        }

        if (altUOMId == primaryUOMId.Value)
        {
            throw new MultiUOMInvalidConfigurationException(
                "The owner's primary UOM cannot be used as an alternate UOM.");
        }

        var existingMultiUOMs = await repository.GetByReferenceAsync(
            refType,
            refId,
            cancellationToken);
        var conversions = existingMultiUOMs
            .Where(multiUOM => multiUOM.id != excludingId)
            .ToDictionary(multiUOM => multiUOM.altUOMId);
        conversions[altUOMId] = new MultiUOM
        {
            altUOMId = altUOMId,
            baseUOMId = baseUOMId,
            conversionFactor = conversionFactor
        };

        if (GetFactorToPrimary(altUOMId, primaryUOMId.Value, conversions) is null)
        {
            throw new MultiUOMInvalidConfigurationException(
                "The conversion must lead to the owner's primary UOM without creating a cycle.");
        }
    }

    /// <summary>
    /// Calculates a UOM's cumulative factor to the primary UOM, or returns null for a missing or cyclic path.
    /// </summary>
    private static decimal? GetFactorToPrimary(
        int uomId,
        int primaryUOMId,
        IReadOnlyDictionary<int, MultiUOM> conversions)
    {
        var factor = 1m;
        var currentUOMId = uomId;
        var visited = new HashSet<int>();

        while (currentUOMId != primaryUOMId)
        {
            if (!visited.Add(currentUOMId)
                || !conversions.TryGetValue(currentUOMId, out var conversion))
            {
                return null;
            }

            if (conversion.conversionFactor is not > 0)
            {
                throw new MultiUOMInvalidConfigurationException(
                    $"The saved conversion for alternate UOM {currentUOMId} has an invalid conversion factor.");
            }

            factor *= conversion.conversionFactor.Value;
            currentUOMId = conversion.baseUOMId;
        }

        return factor;
    }

    /// <summary>
    /// Ensures the owner does not already have the alternate UOM, optionally excluding the record being updated.
    /// </summary>
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
    /// <summary>
    /// Retrieves the base UOM ID for a given reference type and reference ID, or throws
    /// </summary>
    /// <param name="refType">The reference type (e.g., Product, RawMaterial).</param>
    /// <param name="refId">The reference ID of the owner.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The base UOM ID as an integer.</returns>
    /// <exception cref="MultiUOMOwnerNotFoundException">Thrown if the owner does
    /// not exist.</exception>
    /// <exception cref="ArgumentException">Thrown if the provided reference ID is invalid.</exception>
    /// <remarks>
    public async Task<int?> GetBaseUOMIdAsync(
        MultiUOMRefType refType,
        int refId,
        CancellationToken cancellationToken)
    {
        if(!int.TryParse(refId.ToString(), out var referenceId))
        {
            
            throw new ArgumentException("Invalid reference ID", nameof(refId));
        }

        var baseUOMId = await repository.GetOwnerBaseUOMIdAsync(refType, referenceId, cancellationToken);
       
        if (!baseUOMId.HasValue)
        {
            throw new MultiUOMOwnerNotFoundException(refType.ToString(), refId.ToString(CultureInfo.InvariantCulture));
        }

        return baseUOMId.Value;
    }
    /// <summary>
    /// Retrieves the base UOM code for a given reference type and reference ID, or throws
    /// an exception if the owner does not exist.
    /// </summary>
    /// <param name="refType">The reference type (e.g., Product, RawMaterial).</param>
    /// <param name="refId">The reference ID of the owner.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The base UOM code as a string.</returns>
    /// <exception cref="MultiUOMOwnerNotFoundException">Thrown if the owner does
    /// not exist.</exception>
    /// <exception cref="ArgumentException">Thrown if the provided reference ID is invalid.</exception>
    /// <remarks>
    /// This method retrieves the base UOM code for a given reference type and reference ID.
    /// If the owner does not exist, a <see cref="MultiUOMOwnerNotFoundException"/> is thrown. If the provided reference ID is invalid, an <see cref="ArgumentException"/> is thrown.
    /// </remarks>
    
    public async Task<string?> GetBaseUOMCodeAsync(
        MultiUOMRefType refType,
        int refId,
        CancellationToken cancellationToken = default)
    {
        if(!int.TryParse(refId.ToString(), out var referenceId))
        {
            throw new ArgumentException("Invalid reference ID", nameof(refId));
        }
        
        var baseUOMCode = await repository.GetOwnerBaseUOMCodeAsync(refType, referenceId, cancellationToken);
        if (baseUOMCode is null)
        {
            throw new MultiUOMOwnerNotFoundException(refType.ToString(), refId.ToString(CultureInfo.InvariantCulture));
        }

        return baseUOMCode;
    }

    /// <summary>
    /// Retrieves the UOM code for a given UOM ID, or throws an exception if
    /// the UOM does not exist.
    /// </summary>
    /// <param name="uomID">The ID of the UOM.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The UOM code as a string.</returns>
    /// <exception cref="MultiUOMSetupNotFoundException">Thrown if the UOM
    /// does not exist.</exception>
    /// <exception cref="ArgumentException">Thrown if the provided UOM ID is invalid.</exception>
    /// <remarks>
    /// This method retrieves the UOM code for a given UOM ID. If the U
    /// OM does not exist, a <see cref="MultiUOMSetupNotFoundException"/> is thrown. If the provided UOM ID is invalid, an <see cref="ArgumentException"/> is thrown.
    /// </remarks>
    public async Task<string?> GetMultiUOMCodeAsync(int uomID, CancellationToken cancellationToken = default)
    {
        if(!int.TryParse(uomID.ToString(), out var uomId))
        {
            throw new ArgumentException("Invalid UOM ID", nameof(uomID));
        }
        var multiUOMCode = await repository.GetMultiUOMCodeAsync(uomID, cancellationToken);
        if (multiUOMCode is null)
        {
            throw new MultiUOMSetupNotFoundException(uomID);
        }

        return multiUOMCode;
    }

    /// <summary>
    /// Retrieves the conversion factor for a given alternate UOM ID, or <see langword
    /// ="null"/> if the alternate UOM does not exist.
    /// </summary>
    /// <param name="id">The ID of the alternate UOM.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The conversion factor, or <see langword="null"/> if the alternate
    /// UOM does not exist.</returns>
    /// <exception cref="MultiUOMSetupNotFoundException">Thrown if the alternate UOM does not exist.</exception>
    /// <exception cref="ArgumentException">Thrown if the provided ID is invalid.</exception>
    /// <remarks>
    /// This method retrieves the conversion factor for a given alternate UOM ID. If the alternate
    /// UOM does not exist, a <see cref="MultiUOMSetupNotFoundException"/> is thrown. If the provided ID is invalid, an <see cref="ArgumentException"/> is thrown.
    /// </remarks>

    public async Task<int?> GetMultiUOMIdAsync(string code, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException("Invalid UOM code", nameof(code));
        }

        var multiUOMId = await repository.GetMultiUOMId(code, cancellationToken);
        
        if (multiUOMId is null)
        {
            throw new MultIUOMSetupNotFoundExceptionString(code);
        }

        return multiUOMId;
    }

    /// <summary>
    /// Maps a persisted alternate UOM conversion to its API data transfer object.
    /// </summary>
    /// <param name="multiUOM">The persisted alternate UOM conversion.</param>
    /// <returns>The API data transfer object representing the alternate UOM conversion.</returns>
    /// <remarks>
    /// This method maps a persisted alternate UOM conversion to its corresponding API data transfer object (
    
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
