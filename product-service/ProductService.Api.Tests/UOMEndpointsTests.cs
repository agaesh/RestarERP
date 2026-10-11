using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RestarProduct.Data;
using RestarProduct.DTOs;

namespace ProductService.Api.Tests;

public sealed class UOMEndpointsTests : IDisposable
{
    private readonly UOMApiFactory factory = new();
    private readonly HttpClient client;

    public UOMEndpointsTests()
    {
        client = factory.CreateClient();
        factory.InitializeDatabase();
    }

    [Fact]
    public async Task GetUOMs_ReturnsOkAndCreatedRecords()
    {
        await client.PostAsJsonAsync("/uoms", CreateRequest("KG", "Kilogram"));

        var response = await client.GetAsync("/uoms");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var uoms = await response.Content.ReadFromJsonAsync<List<UOMDTO>>();
        var uom = Assert.Single(uoms!);
        Assert.Equal("KG", uom.uom_code);
    }

    [Fact]
    public async Task GetUOMs_ReturnsRequestedPage()
    {
        await client.PostAsJsonAsync("/uoms", CreateRequest("A", "Unit A"));
        await client.PostAsJsonAsync("/uoms", CreateRequest("B", "Unit B"));
        await client.PostAsJsonAsync("/uoms", CreateRequest("C", "Unit C"));

        var response = await client.GetAsync("/uoms?pageNumber=2&pageSize=1");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var uoms = await response.Content.ReadFromJsonAsync<List<UOMDTO>>();
        var uom = Assert.Single(uoms!);
        Assert.Equal("B", uom.uom_code);
    }

    [Fact]
    public async Task GetUOM_WhenMissing_ReturnsNotFound()
    {
        var response = await client.GetAsync("/uoms/99999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task PostUOM_ReturnsCreatedLocationAndTimestamps()
    {
        var response = await client.PostAsJsonAsync(
            "/uoms",
            new CreateUOMDTO
            {
                uom_code = "KG",
                uom_name = "Kilogram",
                description = "Metric unit of mass"
            });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);

        var uom = await response.Content.ReadFromJsonAsync<UOMDTO>();
        Assert.NotNull(uom);
        Assert.Equal("KG", uom!.uom_code);
        Assert.NotEqual(default, uom.create_date);
        Assert.Null(uom.update_date);
    }

    [Fact]
    public async Task PostUOM_WhenCodeAlreadyExists_ReturnsConflict()
    {
        var firstResponse = await client.PostAsJsonAsync("/uoms", CreateRequest("KG", "Kilogram"));
        var duplicateResponse = await client.PostAsJsonAsync("/uoms", CreateRequest(" kg ", "Another name"));

        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, duplicateResponse.StatusCode);
        Assert.Single(await client.GetFromJsonAsync<List<UOMDTO>>("/uoms") ?? []);
    }

    [Fact]
    public async Task PostUOM_WhenRequiredNameIsMissing_ReturnsBadRequestWithValidationText()
    {
        var response = await client.PostAsJsonAsync(
            "/uoms",
            new
            {
                uom_code = "KG",
                description = "Metric unit of mass"
            });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var errors = body.RootElement.GetProperty("errors");
        Assert.Contains(
            "UOM name is required.",
            errors.EnumerateObject()
                .SelectMany(error => error.Value.EnumerateArray())
                .Select(message => message.GetString()));
    }

    [Fact]
    public async Task PutUOM_UpdatesAndReturnsNoContent()
    {
        var createdResponse = await client.PostAsJsonAsync("/uoms", CreateRequest("KG", "Kilogram"));
        var created = await createdResponse.Content.ReadFromJsonAsync<UOMDTO>();

        var response = await client.PutAsJsonAsync(
            $"/uoms/{created!.id}",
            new UpdateUOMDTO
            {
                uom_code = "G",
                uom_name = "Gram",
                description = "Unit of mass",
                is_active = false
            });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var getResponse = await client.GetAsync($"/uoms/{created.id}");
        var updated = await getResponse.Content.ReadFromJsonAsync<UOMDTO>();
        Assert.NotNull(updated);
        Assert.Equal("G", updated!.uom_code);
        Assert.Equal("Gram", updated.uom_name);
        Assert.False(updated.is_active);
        Assert.NotNull(updated.update_date);
    }

    [Fact]
    public async Task PutUOM_WhenCodeBelongsToAnotherUOM_ReturnsConflict()
    {
        await client.PostAsJsonAsync("/uoms", CreateRequest("KG", "Kilogram"));
        var secondResponse = await client.PostAsJsonAsync("/uoms", CreateRequest("G", "Gram"));
        var second = await secondResponse.Content.ReadFromJsonAsync<UOMDTO>();

        var response = await client.PutAsJsonAsync(
            $"/uoms/{second!.id}",
            new UpdateUOMDTO
            {
                uom_code = "kg",
                uom_name = "Kilogram",
                is_active = true
            });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task PutUOM_WhenRequiredCodeIsMissing_ReturnsBadRequestWithValidationText()
    {
        var createdResponse = await client.PostAsJsonAsync("/uoms", CreateRequest("KG", "Kilogram"));
        var created = await createdResponse.Content.ReadFromJsonAsync<UOMDTO>();

        var response = await client.PutAsJsonAsync(
            $"/uoms/{created!.id}",
            new
            {
                uom_name = "Gram"
            });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var errors = body.RootElement.GetProperty("errors");
        Assert.Contains(
            "UOM code is required.",
            errors.EnumerateObject()
                .SelectMany(error => error.Value.EnumerateArray())
                .Select(message => message.GetString()));
    }

    [Fact]
    public async Task PutUOM_WhenMissing_ReturnsNotFound()
    {
        var response = await client.PutAsJsonAsync("/uoms/99999", UpdateRequest());

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeleteUOM_ReturnsNoContentAndThenNotFound()
    {
        var createdResponse = await client.PostAsJsonAsync("/uoms", CreateRequest("KG", "Kilogram"));
        var created = await createdResponse.Content.ReadFromJsonAsync<UOMDTO>();

        var deleteResponse = await client.DeleteAsync($"/uoms/{created!.id}");
        var getResponse = await client.GetAsync($"/uoms/{created.id}");

        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    public void Dispose()
    {
        client.Dispose();
        factory.Dispose();
    }

    private static CreateUOMDTO CreateRequest(string code, string name) => new()
    {
        uom_code = code,
        uom_name = name
    };

    private static UpdateUOMDTO UpdateRequest() => new()
    {
        uom_code = "G",
        uom_name = "Gram"
    };
}

internal sealed class UOMApiFactory : WebApplicationFactory<Program>
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
