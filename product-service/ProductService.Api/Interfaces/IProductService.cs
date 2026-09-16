using RestarProduct.DTOs;

namespace RestarProduct.Interfaces;

public interface IProductService
{
    Task<IReadOnlyList<ProductDTO>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<ProductDTO?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<ProductDTO> CreateAsync(CreateProductDTO product, CancellationToken cancellationToken = default);

    Task<bool> UpdateAsync(int id, UpdateProductDTO product, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
}