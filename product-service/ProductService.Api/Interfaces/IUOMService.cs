using RestarProduct.DTOs;

namespace RestarProduct.Interfaces;

public interface IUOMService
{
    Task<IReadOnlyList<UOMDTO>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<UOMDTO?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<UOMDTO> CreateAsync(CreateUOMDTO request, CancellationToken cancellationToken = default);

    Task<bool> UpdateAsync(int id, UpdateUOMDTO request, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
