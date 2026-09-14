using RestarProduct.DTOs;
using RestarProduct.Helpers;
using RestarProduct.Interfaces;
using RestarProduct.Models;
using RestarProduct.Repositories;
using Microsoft.Extensions.Logging;

namespace RestarProduct.Services;

public class ProductService(
    IProductRepository productRepository,
    ILogger<ProductService> logger) : IProductService
{
    public async Task<IReadOnlyList<ProductDTO>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var products = (await productRepository.GetAllAsync(cancellationToken)).Select(ToDTO).ToList();
        logger.LogInformation("Retrieved {ProductCount} products", products.Count);
        return products;
    }

    public async Task<ProductDTO?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var product = await productRepository.GetByIdAsync(id, cancellationToken);
        logger.LogInformation("Product lookup completed for product {ProductId}. Found: {Found}", id, product is not null);
        return product is null ? null : ToDTO(product);
    }

    public async Task<ProductDTO> CreateAsync(
        CreateProductDTO product,
        CancellationToken cancellationToken = default)
    {

        var generatedProductCode = ProductCodeGenerator.Generate(product.product_name);
        var entity = new Product
        {
            product_code = generatedProductCode,
            product_name = product.product_name,
            description = product.description,
            image_url = product.image_url,
            product_type = product.product_type,
            is_active = product.is_active,
            is_available = product.is_available,
            uom = product.uom,
            tax_id = product.tax_id,
            barcode = product.barcode,
            vendor_barcode_no = product.vendor_barcode_no,
            create_date = DateTime.UtcNow
        };

        await productRepository.AddAsync(entity, cancellationToken);
        await productRepository.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Created product {ProductId} with code {ProductCode}", entity.id, entity.product_code);
        return ToDTO(entity);
    }

    public async Task<bool> UpdateAsync(
        int id,
        UpdateProductDTO product,
        CancellationToken cancellationToken = default)
    {
        var existingProduct = await productRepository.FindTrackedAsync(id, cancellationToken);

        if (existingProduct is null)
        {
            logger.LogWarning("Product update skipped because product {ProductId} was not found", id);
            return false;
        }

        existingProduct.update_date = DateTime.UtcNow;
        existingProduct.product_code = product.product_code;
        existingProduct.product_name = product.product_name;
        existingProduct.description = product.description;
        existingProduct.image_url = product.image_url;
        existingProduct.product_type = product.product_type;
        existingProduct.is_active = product.is_active;
        existingProduct.is_available = product.is_available;
        existingProduct.uom = product.uom;
        existingProduct.tax_id = product.tax_id;
        existingProduct.barcode = product.barcode;
        existingProduct.vendor_barcode_no = product.vendor_barcode_no;
        await productRepository.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Updated product {ProductId}", id);
        return true;
    }

    public async Task<bool> DeleteAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var product = await productRepository.FindTrackedAsync(id, cancellationToken);

        if (product is null)
        {
            logger.LogWarning("Product deletion skipped because product {ProductId} was not found", id);
            return false;
        }

        productRepository.Remove(product);
        await productRepository.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Deleted product {ProductId}", id);
        return true;
    }

    private static ProductDTO ToDTO(Product product) => new()
    {
        id = product.id,
        product_code = product.product_code,
        product_name = product.product_name,
        description = product.description,
        image_url = product.image_url,
        product_type = product.product_type,
        is_active = product.is_active,
        is_available = product.is_available,
        uom = product.uom,
        tax_id = product.tax_id,
        barcode = product.barcode,
        vendor_barcode_no = product.vendor_barcode_no,
        create_date = product.create_date,
        update_date = product.update_date
    };
}
