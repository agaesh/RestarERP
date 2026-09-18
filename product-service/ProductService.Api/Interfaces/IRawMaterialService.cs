using RestarProduct.DTOs;

namespace RestarProduct.Interfaces;

public interface IRawMaterialService
{
    Task<IReadOnlyList<RawMaterialDTO>> GetAllAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default);

    Task<RawMaterialDTO?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<RawMaterialDTO> CreateAsync(CreateRawMaterialDTO rawMaterial, CancellationToken cancellationToken = default);

    Task<bool> UpdateAsync(int id, UpdateRawMaterialDTO rawMaterial, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
