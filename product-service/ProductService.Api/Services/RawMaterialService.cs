using RestarProduct.DTOs;
using RestarProduct.Helpers;
using RestarProduct.Interfaces;
using RestarProduct.Models;

namespace RestarProduct.Services;

public class RawMaterialService(
    IRawMaterialRepository repository,
    ILogger<RawMaterialService> logger) : IRawMaterialService
{
    public async Task<IReadOnlyList<RawMaterialDTO>> GetAllAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        var normalizedPageNumber = pageNumber <= 0 ? 1 : pageNumber;
        var normalizedPageSize = pageSize <= 0 ? 10 : pageSize;

        var materials = (await repository.GetPagedAsync(normalizedPageNumber, normalizedPageSize, cancellationToken))
            .Select(ToDTO)
            .ToList();

        logger.LogInformation("Retrieved {RawMaterialCount} raw materials for page {PageNumber} with page size {PageSize}", materials.Count, normalizedPageNumber, normalizedPageSize);
        return materials;
    }

    public async Task<RawMaterialDTO?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var material = await repository.GetByIdAsync(id, cancellationToken);

        logger.LogInformation("Raw material lookup completed for raw material {RawMaterialId}. Found: {Found}", id, material is not null);
        return material is null ? null : ToDTO(material);
    }

    public async Task<RawMaterialDTO> CreateAsync(CreateRawMaterialDTO rawMaterial, CancellationToken cancellationToken = default)
    {
        Validate(rawMaterial.material_code, rawMaterial.material_name, rawMaterial.uom, allowGeneratedCode: true);

        var entity = new RawMaterial
        {
            material_code = string.IsNullOrWhiteSpace(rawMaterial.material_code)
                ? $"RM-PENDING-{Guid.NewGuid():N}"
                : rawMaterial.material_code.Trim(),
            material_name = rawMaterial.material_name.Trim(),
            material_desc = string.IsNullOrWhiteSpace(rawMaterial.material_desc) ? null : rawMaterial.material_desc.Trim(),
            uom = string.IsNullOrWhiteSpace(rawMaterial.uom) ? null : rawMaterial.uom.Trim(),
            is_active = rawMaterial.is_active,
            create_date = DateTime.UtcNow
        };

        await repository.AddAsync(entity, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(rawMaterial.material_code))
        {
            entity.material_code = RawMaterialCodeGenerator.Generate(entity.id);
            await repository.SaveChangesAsync(cancellationToken);
        }

        logger.LogInformation("Created raw material {RawMaterialId} with code {MaterialCode}", entity.id, entity.material_code);
        return ToDTO(entity);
    }

    public async Task<bool> UpdateAsync(int id, UpdateRawMaterialDTO rawMaterial, CancellationToken cancellationToken = default)
    {
        var existingMaterial = await repository.FindTrackedAsync(id, cancellationToken);

        if (existingMaterial is null)
        {
            logger.LogWarning("Raw material update skipped because raw material {RawMaterialId} was not found", id);
            return false;
        }

        Validate(rawMaterial.material_code, rawMaterial.material_name, rawMaterial.uom);

        existingMaterial.material_code = rawMaterial.material_code.Trim();
        existingMaterial.material_name = rawMaterial.material_name.Trim();
        existingMaterial.material_desc = string.IsNullOrWhiteSpace(rawMaterial.material_desc) ? null : rawMaterial.material_desc.Trim();
        existingMaterial.uom = string.IsNullOrWhiteSpace(rawMaterial.uom) ? null : rawMaterial.uom.Trim();
        existingMaterial.is_active = rawMaterial.is_active;
        existingMaterial.update_date = DateTime.UtcNow;

        await repository.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Updated raw material {RawMaterialId}", id);
        return true;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var material = await repository.FindTrackedAsync(id, cancellationToken);

        if (material is null)
        {
            logger.LogWarning("Raw material deletion skipped because raw material {RawMaterialId} was not found", id);
            return false;
        }

        repository.Remove(material);
        await repository.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Deleted raw material {RawMaterialId}", id);
        return true;
    }

    private static void Validate(string? materialCode, string? materialName, string? uom, bool allowGeneratedCode = false)
    {
        if (!allowGeneratedCode && string.IsNullOrWhiteSpace(materialCode))
        {
            throw new ArgumentException("Material code is required.", nameof(materialCode));
        }

        if (string.IsNullOrWhiteSpace(materialName))
        {
            throw new ArgumentException("Material name is required.", nameof(materialName));
        }

        if (string.IsNullOrWhiteSpace(uom))
        {
            throw new ArgumentException("Unit of measure is required.", nameof(uom));
        }
    }

    private static RawMaterialDTO ToDTO(RawMaterial material) => new()
    {
        id = material.id,
        material_code = material.material_code,
        material_name = material.material_name,
        material_desc = material.material_desc,
        uom = material.uom,
        is_active = material.is_active,
        create_date = material.create_date,
        update_date = material.update_date
    };
}
