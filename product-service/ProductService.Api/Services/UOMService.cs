using RestarProduct.DTOs;
using RestarProduct.Exceptions;
using RestarProduct.Interfaces;
using RestarProduct.Models;

namespace RestarProduct.Services;

public sealed class UOMService(
    IUOMRepository repository,
    ILogger<UOMService> logger) : IUOMService
{
    public async Task<IReadOnlyList<UOMDTO>> GetAllAsync(
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var normalizedPageNumber = pageNumber <= 0 ? 1 : pageNumber;
        var normalizedPageSize = pageSize <= 0 ? 10 : pageSize;

        var uoms = (await repository.GetPagedAsync(normalizedPageNumber, normalizedPageSize, cancellationToken))
            .Select(ToDTO)
            .ToList();

        logger.LogInformation(
            "Retrieved {UOMCount} units of measure for page {PageNumber} with page size {PageSize}",
            uoms.Count,
            normalizedPageNumber,
            normalizedPageSize);
        return uoms;
    }

    public async Task<UOMDTO?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var uom = await repository.GetByIdAsync(id, cancellationToken);
        return uom is null ? null : ToDTO(uom);
    }

    public async Task<UOMDTO> CreateAsync(CreateUOMDTO request, CancellationToken cancellationToken = default)
    {
        var code = NormalizeCode(request.uom_code);
        if (await repository.CodeExistsAsync(code, cancellationToken: cancellationToken))
        {
            throw new UOMCodeAlreadyExistsException(code);
        }

        var uom = new UOM
        {
            Code = code,
            Name = request.uom_name.Trim(),
            Description = NormalizeDescription(request.description),
            IsActive = request.is_active,
            CreateDate = DateTime.UtcNow
        };

        await repository.AddAsync(uom, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Created unit of measure {UOMId} with code {UOMCode}", uom.Id, uom.Code);
        return ToDTO(uom);
    }

    public async Task<bool> UpdateAsync(int id, UpdateUOMDTO request, CancellationToken cancellationToken = default)
    {
        var uom = await repository.FindTrackedAsync(id, cancellationToken);

        if (uom is null)
        {
            logger.LogWarning("Unit of measure update skipped because UOM {UOMId} was not found", id);
            return false;
        }

        var code = NormalizeCode(request.uom_code);
        if (await repository.CodeExistsAsync(code, id, cancellationToken))
        {
            throw new UOMCodeAlreadyExistsException(code);
        }

        uom.Code = code;
        uom.Name = request.uom_name.Trim();
        uom.Description = NormalizeDescription(request.description);
        uom.IsActive = request.is_active;
        uom.UpdateDate = DateTime.UtcNow;

        await repository.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Updated unit of measure {UOMId}", id);
        return true;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var uom = await repository.FindTrackedAsync(id, cancellationToken);
        if (uom is null)
        {
            logger.LogWarning("Unit of measure deletion skipped because UOM {UOMId} was not found", id);
            return false;
        }

        repository.Remove(uom);
        await repository.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Deleted unit of measure {UOMId}", id);
        return true;
    }

    private static string? NormalizeDescription(string? description) =>
        string.IsNullOrWhiteSpace(description) ? null : description.Trim();

    private static string NormalizeCode(string code) => code.Trim().ToUpperInvariant();

    private static UOMDTO ToDTO(UOM uom) => new(
        uom.Id,
        uom.Code,
        uom.Name,
        uom.Description,
        uom.IsActive,
        new DateTimeOffset(DateTime.SpecifyKind(uom.CreateDate, DateTimeKind.Utc)),
        uom.UpdateDate is null
            ? null
            : new DateTimeOffset(DateTime.SpecifyKind(uom.UpdateDate.Value, DateTimeKind.Utc)));
}
