using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RestarProduct.Data;
using RestarProduct.DTOs;
using RestarProduct.Enums;

namespace ProductService.Api.Tests;

public sealed class ProductEndpointsTests : IDisposable
{
    private readonly ProductApiFactory factory;
    private readonly HttpClient client;

    public ProductEndpointsTests()
    {
        factory = new ProductApiFactory();
        client = factory.CreateClient();
        factory.InitializeDatabase();
    }

    [Fact]
    public async Task GetProducts_ReturnsOkAndProducts()
    {
        var response = await client.GetAsync("/products");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var products = await response.Content.ReadFromJsonAsync<List<ProductDTO>>();
        Assert.NotNull(products);
    }

    [Fact]
    public async Task PostProduct_ReturnsCreatedProductAndLocation()
    {
        var response = await client.PostAsJsonAsync("/products", new CreateProductDTO
        {
            product_name = "Endpoint Coffee",
            product_type = ProductType.Beverage,
            tax_id = 4
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);

        var product = await response.Content.ReadFromJsonAsync<ProductDTO>();
        Assert.NotNull(product);
        Assert.Equal("Endpoint Coffee", product.product_name);
        Assert.Equal(4, product.tax_id);
    }

    [Fact]
    public async Task GetProduct_WhenProductDoesNotExist_ReturnsNotFound()
    {
        var response = await client.GetAsync("/products/99999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task PutProduct_UpdatesProductAndReturnsNoContent()
    {
        var createdResponse = await client.PostAsJsonAsync("/products", new CreateProductDTO
        {
            product_name = "Before update"
        });
        var createdProduct = await createdResponse.Content.ReadFromJsonAsync<ProductDTO>();

        var response = await client.PutAsJsonAsync($"/products/{createdProduct!.id}", new UpdateProductDTO
        {
            product_code = createdProduct.product_code,
            product_name = "After update",
            product_type = ProductType.Food,
            tax_id = 8
        });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var getResponse = await client.GetAsync($"/products/{createdProduct.id}");
        var updatedProduct = await getResponse.Content.ReadFromJsonAsync<ProductDTO>();
        Assert.Equal("After update", updatedProduct!.product_name);
        Assert.Equal(8, updatedProduct.tax_id);
    }

    [Fact]
    public async Task DeleteProduct_ReturnsNoContentAndThenNotFound()
    {
        var createdResponse = await client.PostAsJsonAsync("/products", new CreateProductDTO
        {
            product_name = "To delete"
        });
        var createdProduct = await createdResponse.Content.ReadFromJsonAsync<ProductDTO>();

        var deleteResponse = await client.DeleteAsync($"/products/{createdProduct!.id}");
        var getResponse = await client.GetAsync($"/products/{createdProduct.id}");

        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    public void Dispose()
    {
        client.Dispose();
        factory.Dispose();
    }
}

internal sealed class ProductApiFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection connection = new("Data Source=:memory:");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        connection.Open();

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<ProductDbContext>>();
            services.RemoveAll<Microsoft.EntityFrameworkCore.Infrastructure.IDbContextOptionsConfiguration<ProductDbContext>>();
            services.RemoveAll<ProductDbContext>();
            services.AddDbContext<ProductDbContext>(options => options.UseSqlite(connection));
        });
    }

    public void InitializeDatabase()
    {
        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ProductDbContext>();
        dbContext.Database.EnsureCreated();
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        connection.Dispose();
    }
}
