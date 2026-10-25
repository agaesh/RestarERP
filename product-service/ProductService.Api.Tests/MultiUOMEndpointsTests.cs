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

        var locationResponse = await client.GetAsync(createResponse.Headers.Location);
        Assert.Equal(HttpStatusCode.OK, locationResponse.StatusCode);
        Assert.Equal(
            created.id,
            (await locationResponse.Content.ReadFromJsonAsync<MultiUOMDTO>())!.id);

        var listResponse = await client.GetAsync("/products/1/multi-uoms");
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
        var listed = await listResponse.Content.ReadFromJsonAsync<List<MultiUOMDTO>>();
        Assert.Equal(created.id, Assert.Single(listed!).id);

        var duplicateResponse = await client.PostAsJsonAsync("/products/1/multi-uoms", request);
        var duplicateError = await AssertApiErrorAsync(duplicateResponse, HttpStatusCode.Conflict);
        Assert.Contains("already exists", duplicateError.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("already exists", duplicateError.Error!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task PutMultiUOM_WhenAlternateUOMIsAlreadyUsed_ReturnsConflictApiResponse()
    {
        var uoms = await CreateUOMsAsync();
        var firstResponse = await client.PostAsJsonAsync(
            "/products/1/multi-uoms",
            CreateRequest(uoms));
        var first = await firstResponse.Content.ReadFromJsonAsync<MultiUOMDTO>();
        Assert.NotNull(first);

        var secondResponse = await client.PostAsJsonAsync(
            "/products/1/multi-uoms",
            CreateRequest(uoms) with
            {
                alt_uom_id = uoms["PACK"],
                conversion_factor = 6
            });
        var second = await secondResponse.Content.ReadFromJsonAsync<MultiUOMDTO>();
        Assert.NotNull(second);

        var updateResponse = await client.PutAsJsonAsync(
            $"/products/1/multi-uoms/{second.id}",
            new UpdateMultiUOMDTO
            {
                ref_type = MultiUOMRefType.Product,
                ref_id = "1",
                alt_uom_id = uoms["BOX"],
                base_uom_id = uoms["UNIT"],
                conversion_factor = 5
            });

        var error = await AssertApiErrorAsync(updateResponse, HttpStatusCode.Conflict);
        Assert.Contains("already exists", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task PostMultiUOM_WhenBaseAndAlternateUOMAreSame_ReturnsBadRequest()
    {
        var uoms = await CreateUOMsAsync();
        var request = CreateRequest(uoms) with
        {
            base_uom_id = uoms["BOX"]
        };

        var response = await client.PostAsJsonAsync("/products/1/multi-uoms", request);

        var error = await AssertApiErrorAsync(response, HttpStatusCode.BadRequest);
        Assert.Contains("invalid", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    //this test is to check chained conversion returns correct message 
    [Fact]
    public async Task PostMultiUOM_AllowsChainedConversionsAndResolvesConversionFactor()
    {
        var uoms = await CreateUOMsAsync();
        var createResponse = await client.PostAsJsonAsync(
            "/products/1/multi-uoms",
            CreateRequest(uoms) with { conversion_factor = 10 });
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var chainRequest = CreateRequest(uoms) with
        {
            alt_uom_id = uoms["PACK"],
            base_uom_id = uoms["BOX"],
            conversion_factor = 10
        };
        var chainResponse = await client.PostAsJsonAsync(
            "/products/1/multi-uoms",
            chainRequest);
        Assert.Equal(HttpStatusCode.Created, chainResponse.StatusCode);

        var conversionResponse = await client.GetAsync(
            $"/products/1/multi-uoms/conversion?from_uom_id={uoms["PACK"]}&to_uom_id={uoms["UNIT"]}");
        Assert.Equal(HttpStatusCode.OK, conversionResponse.StatusCode);
        var conversion = await conversionResponse.Content.ReadFromJsonAsync<ConversionFactorResponseDTO>();
        Assert.NotNull(conversion);
        Assert.Equal(100m, conversion.conversion_factor);

        var inverseResponse = await client.GetAsync(
            $"/products/1/multi-uoms/conversion?from_uom_id={uoms["UNIT"]}&to_uom_id={uoms["PACK"]}");
        Assert.Equal(HttpStatusCode.OK, inverseResponse.StatusCode);
        var inverse = await inverseResponse.Content.ReadFromJsonAsync<ConversionFactorResponseDTO>();
        Assert.Equal(0.01m, inverse!.conversion_factor);
    }

    [Fact]
    public async Task GetConversionFactor_WhenNoConversionPathExists_ReturnsNotFound()
    {
        var uoms = await CreateUOMsAsync();

        var response = await client.GetAsync(
            $"/products/1/multi-uoms/conversion?from_uom_id={uoms["PACK"]}&to_uom_id={uoms["UNIT"]}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
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
    //this testing is to check with 
    public async Task PutMultiUOM_WhenChangeWouldCreateCycle_ReturnsBadRequest()
    {
        var uoms = await CreateUOMsAsync();
        var existingResponse = await client.PostAsJsonAsync(
            "/products/1/multi-uoms",
            CreateRequest(uoms));
        Assert.Equal(HttpStatusCode.Created, existingResponse.StatusCode);
        var existing = await existingResponse.Content.ReadFromJsonAsync<MultiUOMDTO>();
        Assert.NotNull(existing);

        var updateTargetResponse = await client.PostAsJsonAsync(
            "/products/1/multi-uoms",
            CreateRequest(uoms) with
            {
                alt_uom_id = uoms["PACK"],
                base_uom_id = uoms["BOX"],
                conversion_factor = 10
            });
        Assert.Equal(HttpStatusCode.Created, updateTargetResponse.StatusCode);
        var updateTarget = await updateTargetResponse.Content.ReadFromJsonAsync<MultiUOMDTO>();
        Assert.NotNull(updateTarget);
        Assert.NotEqual(existing.id, updateTarget.id);
        Assert.Equal(uoms["BOX"], existing.alt_uom_id);
        Assert.Equal(uoms["UNIT"], existing.base_uom_id);
        Assert.Equal(uoms["PACK"], updateTarget.alt_uom_id);
        Assert.Equal(uoms["BOX"], updateTarget.base_uom_id);

        var updateResponse = await client.PutAsJsonAsync(
            $"/products/1/multi-uoms/{existing.id}",
            new UpdateMultiUOMDTO
            {
                ref_type = MultiUOMRefType.Product,
                ref_id = "1",
                alt_uom_id = uoms["BOX"],
                base_uom_id = uoms["PACK"],
                conversion_factor = 6
            });

        var error = await AssertApiErrorAsync(updateResponse, HttpStatusCode.BadRequest);
        Assert.Contains("invalid", error.Message, StringComparison.OrdinalIgnoreCase);

        var getResponse = await client.GetAsync(
            $"/products/1/multi-uoms/{existing.id}");
        var unchanged = await getResponse.Content.ReadFromJsonAsync<MultiUOMDTO>();
        Assert.Equal(uoms["BOX"], unchanged!.alt_uom_id);
        Assert.Equal(uoms["UNIT"], unchanged.base_uom_id);
        Assert.Equal(12m, unchanged.conversion_factor);
    }

    [Fact]
    public async Task PostMultiUOM_WhenPrimaryUOMIsSelectedAsAlternate_ReturnsBadRequest()
    {
        var uoms = await CreateUOMsAsync();
        var request = CreateRequest(uoms) with
        {
            alt_uom_id = uoms["UNIT"],
            base_uom_id = uoms["BOX"]
        };

        var response = await client.PostAsJsonAsync("/products/1/multi-uoms", request);

        var error = await AssertApiErrorAsync(response, HttpStatusCode.BadRequest);
        Assert.Contains("invalid", error.Message, StringComparison.OrdinalIgnoreCase);
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

        var error = await AssertApiErrorAsync(response, HttpStatusCode.BadRequest);
        Assert.Contains("unknown UOM", error.Message, StringComparison.OrdinalIgnoreCase);
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

        var error = await AssertApiErrorAsync(response, HttpStatusCode.NotFound);
        Assert.Contains("owner", error.Message, StringComparison.OrdinalIgnoreCase);
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

        var error = await AssertApiErrorAsync(response, HttpStatusCode.BadRequest);
        Assert.Contains("does not match", error.Message, StringComparison.OrdinalIgnoreCase);
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
    //added a method to simplify error asserting for test coverage
    private static async Task<ApiResponse> AssertApiErrorAsync(
        HttpResponseMessage response,
        HttpStatusCode expectedStatusCode)
    {
        Assert.Equal(expectedStatusCode, response.StatusCode);

        var error = await response.Content.ReadFromJsonAsync<ApiResponse>();
        Assert.NotNull(error);
        Assert.False(error.Success);
        Assert.False(string.IsNullOrWhiteSpace(error.Message));
        Assert.False(string.IsNullOrWhiteSpace(error.Error));
        return error;
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
            product_name = "Test Product",
            uom = "UNIT"
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
