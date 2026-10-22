using RestarProduct.Enums;
using RestarProduct.Models;

namespace RestarProduct.Interfaces;

public interface IMultiUOMRepository
{
    Task<IReadOnlyList<MultiUOM>> GetByReferenceAsync(
        MultiUOMRefType refType,
        string refId,
        CancellationToken cancellationToken = default);

    Task<bool> ReferenceExistsAsync(
        MultiUOMRefType refType,
        string refId,
        CancellationToken cancellationToken = default);

    Task<MultiUOM?> GetByIdAsync(
        int id,
        MultiUOMRefType refType,
        string refId,
        CancellationToken cancellationToken = default);

    Task<MultiUOM?> FindTrackedAsync(
        int id,
        MultiUOMRefType refType,
        string refId,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsForReferenceAsync(
        MultiUOMRefType refType,
        string refId,
        int altUOMId,
        int? excludingId = null,
        CancellationToken cancellationToken = default);

    Task<bool> UOMExistsAsync(int uomId, CancellationToken cancellationToken = default);

    Task<MultiUOM> AddAsync(MultiUOM multiUOM, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);

    void Remove(MultiUOM multiUOM);
}
