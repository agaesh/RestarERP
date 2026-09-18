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

namespace ProductService.Api.Tests;

public sealed class RawMaterialEndpointsTests : IDisposable
{
    private readonly RawMaterialApiFactory factory;
    private readonly HttpClient client;

    public RawMaterialEndpointsTests()
    {
        factory = new RawMaterialApiFactory();
        client = factory.CreateClient();
        factory.InitializeDatabase();
    }

    [Fact]
    public async Task GetRawMaterials_ReturnsOk()
    {
        var response = await client.GetAsync("/raw-materials");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var materials = await response.Content.ReadFromJsonAsync<List<RawMaterialDTO>>();
        Assert.NotNull(materials);
    }

    [Fact]
    public async Task PostRawMaterial_ReturnsCreated()
    {
        var response = await client.PostAsJsonAsync("/raw-materials", new CreateRawMaterialDTO
        {
            material_code = "RM-010",
            material_name = "Cucumber",
            uom = "kg"
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var material = await response.Content.ReadFromJsonAsync<RawMaterialDTO>();
        Assert.NotNull(material);
        Assert.Equal("Cucumber", material!.material_name);
    }

    [Fact]
    public async Task GetRawMaterial_WhenMissing_ReturnsNotFound()
    {
        var response = await client.GetAsync("/raw-materials/99999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task PutRawMaterial_UpdatesAndReturnsNoContent()
    {
        var createdResponse = await client.PostAsJsonAsync("/raw-materials", new CreateRawMaterialDTO
        {
            material_code = "RM-011",
            material_name = "Before update",
            uom = "kg"
        });
        var created = await createdResponse.Content.ReadFromJsonAsync<RawMaterialDTO>();

        var response = await client.PutAsJsonAsync($"/raw-materials/{created!.id}", new UpdateRawMaterialDTO
        {
            material_code = "RM-011",
            material_name = "After update",
            material_desc = "Updated",
            uom = "box",
            is_active = false
        });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var getResponse = await client.GetAsync($"/raw-materials/{created.id}");
        var updated = await getResponse.Content.ReadFromJsonAsync<RawMaterialDTO>();
        Assert.Equal("After update", updated!.material_name);
        Assert.Equal("box", updated.uom);
    }

    [Fact]
    public async Task DeleteRawMaterial_ReturnsNoContent()
    {
        var createdResponse = await client.PostAsJsonAsync("/raw-materials", new CreateRawMaterialDTO
        {
            material_code = "RM-012",
            material_name = "To delete",
            uom = "kg"
        });
        var created = await createdResponse.Content.ReadFromJsonAsync<RawMaterialDTO>();

        var deleteResponse = await client.DeleteAsync($"/raw-materials/{created!.id}");
        var getResponse = await client.GetAsync($"/raw-materials/{created.id}");

        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    public void Dispose()
    {
        client.Dispose();
        factory.Dispose();
    }
}

internal sealed class RawMaterialApiFactory : WebApplicationFactory<Program>
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
