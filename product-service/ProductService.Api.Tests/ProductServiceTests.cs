using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using RestarProduct.Data;
using RestarProduct.DTOs;
using RestarProduct.Enums;
using RestarProduct.Repositories;
using RestarProduct.Services;
using ProductServiceImplementation = RestarProduct.Services.ProductService;

namespace ProductService.Api.Tests;

public sealed class ProductServiceTests : IDisposable
{
    private readonly SqliteConnection connection;
    private readonly ProductDbContext dbContext;
    private readonly ProductServiceImplementation productService;

    public ProductServiceTests()
    {
        connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<ProductDbContext>()
            .UseSqlite(connection)
            .Options;

        dbContext = new ProductDbContext(options);
        dbContext.Database.EnsureCreated();

        var repository = new ProductRepository(dbContext);
        productService = new ProductServiceImplementation(
            repository,
            NullLogger<ProductServiceImplementation>.Instance);
    }

    [Fact]
    public async Task CreateAsync_PersistsProductAndTaxId()
    {
        var result = await productService.CreateAsync(new CreateProductDTO
        {
            product_name = "Coffee",
            product_type = ProductType.Beverage,
            tax_id = 7
        });

        var persistedProduct = await dbContext.Products.SingleAsync();

        Assert.Equal(persistedProduct.id, result.id);
        Assert.Equal("Coffee", result.product_name);
        Assert.Equal(7, result.tax_id);
        Assert.Equal(7, persistedProduct.tax_id);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsProductsOrderedByName()
    {
        await productService.CreateAsync(new CreateProductDTO { product_name = "Zest" });
        await productService.CreateAsync(new CreateProductDTO { product_name = "Apple" });

        var products = await productService.GetAllAsync();

        Assert.Equal(["Apple", "Zest"], products.Select(product => product.product_name));
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsPersistedProduct()
    {
        var createdProduct = await productService.CreateAsync(new CreateProductDTO
        {
            product_name = "Tea",
            tax_id = 3
        });

        var result = await productService.GetByIdAsync(createdProduct.id);

        Assert.NotNull(result);
        Assert.Equal(createdProduct.id, result.id);
        Assert.Equal(3, result.tax_id);
    }

    [Fact]
    public async Task UpdateAsync_UpdatesProductAndTaxId()
    {
        var createdProduct = await productService.CreateAsync(new CreateProductDTO
        {
            product_name = "Old name",
            tax_id = 1
        });

        var updated = await productService.UpdateAsync(createdProduct.id, new UpdateProductDTO
        {
            product_code = createdProduct.product_code,
            product_name = "New name",
            product_type = ProductType.Food,
            tax_id = 9
        });

        var result = await productService.GetByIdAsync(createdProduct.id);

        Assert.True(updated);
        Assert.NotNull(result);
        Assert.Equal("New name", result.product_name);
        Assert.Equal(ProductType.Food, result.product_type);
        Assert.Equal(9, result.tax_id);
    }

    [Fact]
    public async Task DeleteAsync_RemovesProductAndReturnsFalseForMissingProduct()
    {
        var createdProduct = await productService.CreateAsync(new CreateProductDTO
        {
            product_name = "To delete"
        });

        var deleted = await productService.DeleteAsync(createdProduct.id);
        var missing = await productService.GetByIdAsync(createdProduct.id);
        var deletedAgain = await productService.DeleteAsync(createdProduct.id);

        Assert.True(deleted);
        Assert.Null(missing);
        Assert.False(deletedAgain);
    }

    public void Dispose()
    {
        dbContext.Dispose();
        connection.Dispose();
    }
}
