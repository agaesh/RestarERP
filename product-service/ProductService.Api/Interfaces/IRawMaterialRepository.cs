using RestarProduct.Models;

namespace RestarProduct.Interfaces;

public interface IRawMaterialRepository
{
    Task<IReadOnlyList<RawMaterial>> GetPagedAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RawMaterial>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<RawMaterial?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<RawMaterial?> FindTrackedAsync(int id, CancellationToken cancellationToken = default);

    Task<RawMaterial> AddAsync(RawMaterial rawMaterial, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);

    void Remove(RawMaterial rawMaterial);
}
