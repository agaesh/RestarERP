using RestarProduct.DTOs;
using RestarProduct.Enums;

namespace RestarProduct.Interfaces;

public interface IMultiUOMService
{
    Task<IReadOnlyList<MultiUOMDTO>?> GetByReferenceAsync(
        MultiUOMRefType refType,
        int referenceId,
        CancellationToken cancellationToken = default);

    Task<MultiUOMDTO?> GetByIdAsync(
        int id,
        MultiUOMRefType refType,
        int referenceId,
        CancellationToken cancellationToken = default);

    Task<MultiUOMDTO> CreateAsync(
        MultiUOMRefType refType,
        int referenceId,
        CreateMultiUOMDTO request,
        CancellationToken cancellationToken = default);

    Task<bool> UpdateAsync(
        int id,
        MultiUOMRefType refType,
        int referenceId,
        UpdateMultiUOMDTO request,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(
        int id,
        MultiUOMRefType refType,
        int referenceId,
        CancellationToken cancellationToken = default);
}
