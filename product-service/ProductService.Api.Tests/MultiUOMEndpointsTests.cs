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

public sealed class MultiUOMEndpointsTests : IDisposable
{
    private readonly MultiUOMApiFactory factory = new();
    private readonly HttpClient client;

    public MultiUOMEndpointsTests()
    {
        client = factory.CreateClient();
        factory.InitializeDatabase();
    }

    [Fact]
    public async Task PostMultiUOM_CreatesAndListsSetup_AndDuplicateReturnsConflict()
    {
        var uoms = await CreateUOMsAsync();
        var request = CreateRequest(uoms);

        var createResponse = await client.PostAsJsonAsync("/products/1/multi-uoms", request);

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        Assert.NotNull(createResponse.Headers.Location);
        var created = await createResponse.Content.ReadFromJsonAsync<MultiUOMDTO>();
        Assert.NotNull(created);
        Assert.Equal(uoms["BOX"], created.alt_uom_id);
        Assert.Equal(uoms["UNIT"], created.base_uom_id);
        Assert.Equal(12m, created.conversion_factor);

        var listResponse = await client.GetAsync("/products/1/multi-uoms");
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
        var listed = await listResponse.Content.ReadFromJsonAsync<List<MultiUOMDTO>>();
        Assert.Equal(created.id, Assert.Single(listed!).id);

        var duplicateResponse = await client.PostAsJsonAsync("/products/1/multi-uoms", request);
        Assert.Equal(HttpStatusCode.Conflict, duplicateResponse.StatusCode);
    }

    [Fact]
    public async Task GetMultiUOM_WhenMissing_ReturnsNotFound()
    {
        var response = await client.GetAsync("/products/1/multi-uoms/99999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task PutAndDeleteMultiUOM_ReturnExpectedStatuses()
    {
        var uoms = await CreateUOMsAsync();
        var createResponse = await client.PostAsJsonAsync("/products/1/multi-uoms", CreateRequest(uoms));
        var created = await createResponse.Content.ReadFromJsonAsync<MultiUOMDTO>();

        var updateResponse = await client.PutAsJsonAsync(
            $"/products/1/multi-uoms/{created!.id}",
            new UpdateMultiUOMDTO
            {
                ref_type = MultiUOMRefType.Product,
                ref_id = "1",
                alt_uom_id = uoms["PACK"],
                base_uom_id = uoms["UNIT"],
                conversion_factor = 6
            });

        Assert.Equal(HttpStatusCode.NoContent, updateResponse.StatusCode);

        var getResponse = await client.GetAsync($"/products/1/multi-uoms/{created.id}");
        var updated = await getResponse.Content.ReadFromJsonAsync<MultiUOMDTO>();
        Assert.Equal(uoms["PACK"], updated!.alt_uom_id);
        Assert.Equal(6m, updated.conversion_factor);

        var deleteResponse = await client.DeleteAsync($"/products/1/multi-uoms/{created.id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/products/1/multi-uoms/{created.id}")).StatusCode);
    }

    [Fact]
    public async Task PostMultiUOM_WhenUOMCodeDoesNotExist_ReturnsBadRequest()
    {
        var request = CreateRequest(new Dictionary<string, int>
        {
            ["BOX"] = 999,
            ["UNIT"] = 1000
        });
        var response = await client.PostAsJsonAsync("/products/1/multi-uoms", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PostMultiUOM_WhenRequiredFieldIsMissing_ReturnsBadRequest()
    {
        var response = await client.PostAsJsonAsync(
            "/products/1/multi-uoms",
            new
            {
                ref_type = "PRODUCT",
                ref_id = "1"
            });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PostMultiUOM_WhenOwnerDoesNotExist_ReturnsNotFound()
    {
        var uoms = await CreateUOMsAsync();
        var response = await client.PostAsJsonAsync(
            "/products/999/multi-uoms",
            CreateRequest(uoms, MultiUOMRefType.Product, "999"));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task PostMultiUOM_WhenBodyReferenceDoesNotMatchRoute_ReturnsBadRequest()
    {
        var uoms = await CreateUOMsAsync();
        var request = CreateRequest(uoms);
        request = request with
        {
            ref_type = MultiUOMRefType.RawMaterial,
            ref_id = "1"
        };

        var response = await client.PostAsJsonAsync("/products/1/multi-uoms", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task MultiUOMEndpoints_AreScopedToOwner()
    {
        var uoms = await CreateUOMsAsync();
        var createResponse = await client.PostAsJsonAsync("/products/1/multi-uoms", CreateRequest(uoms));
        var created = await createResponse.Content.ReadFromJsonAsync<MultiUOMDTO>();

        var response = await client.GetAsync($"/raw-materials/1/multi-uoms/{created!.id}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task RawMaterialMultiUOM_CreatesAndListsSetup()
    {
        var uoms = await CreateUOMsAsync();

        var createResponse = await client.PostAsJsonAsync(
            "/raw-materials/1/multi-uoms",
            CreateRequest(uoms, MultiUOMRefType.RawMaterial, "1"));

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<MultiUOMDTO>();
        Assert.NotNull(created);
        Assert.Equal("RAWMATERIAL", created.ref_type.ToString().ToUpperInvariant());
        Assert.Equal("1", created.ref_id);

        var listResponse = await client.GetAsync("/raw-materials/1/multi-uoms");
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
        Assert.Equal(created.id, Assert.Single(
            (await listResponse.Content.ReadFromJsonAsync<List<MultiUOMDTO>>())!).id);
    }

    public void Dispose()
    {
        client.Dispose();
        factory.Dispose();
    }

    private async Task<Dictionary<string, int>> CreateUOMsAsync()
    {
        var ids = new Dictionary<string, int>();
        foreach (var code in new[] { "BOX", "PACK", "UNIT" })
        {
            var response = await client.PostAsJsonAsync(
                "/uoms",
                new CreateUOMDTO { uom_code = code, uom_name = code });
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            var uom = await response.Content.ReadFromJsonAsync<UOMDTO>();
            Assert.NotNull(uom);
            ids.Add(code, uom!.id);
        }

        return ids;
    }

    private static CreateMultiUOMDTO CreateRequest(
        IReadOnlyDictionary<string, int> uoms,
        MultiUOMRefType refType = MultiUOMRefType.Product,
        string refId = "1") => new()
    {
        ref_type = refType,
        ref_id = refId,
        alt_uom_id = uoms["BOX"],
        base_uom_id = uoms["UNIT"],
        conversion_factor = 12
    };
}

internal sealed class MultiUOMApiFactory : WebApplicationFactory<Program>
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
        dbContext.Products.Add(new RestarProduct.Models.Product
        {
            product_code = "PROD-1",
            product_name = "Test Product"
        });
        dbContext.RawMaterials.Add(new RestarProduct.Models.RawMaterial
        {
            material_code = "RM-1",
            material_name = "Test Material",
            uom = "UNIT"
        });
        dbContext.SaveChanges();
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        connection.Dispose();
    }
}
