using RestarProduct.Models;

namespace RestarProduct.Interfaces;

public interface IUOMRepository
{
    Task<IReadOnlyList<UOM>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<UOM?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<UOM?> FindTrackedAsync(int id, CancellationToken cancellationToken = default);

    Task<UOM> AddAsync(UOM uom, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);

    void Remove(UOM uom);
}
