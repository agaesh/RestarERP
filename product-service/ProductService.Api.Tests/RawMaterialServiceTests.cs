using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using RestarProduct.Data;
using RestarProduct.DTOs;
using RestarProduct.Repositories;
using RestarProduct.Services;

namespace ProductService.Api.Tests;

public sealed class RawMaterialServiceTests : IDisposable
{
    private readonly SqliteConnection connection;
    private readonly ProductDbContext dbContext;
    private readonly RawMaterialService rawMaterialService;

    public RawMaterialServiceTests()
    {
        connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<ProductDbContext>()
            .UseSqlite(connection)
            .Options;

        dbContext = new ProductDbContext(options);
        dbContext.Database.EnsureCreated();

        rawMaterialService = new RawMaterialService(
            new RawMaterialRepository(dbContext),
            NullLogger<RawMaterialService>.Instance);
    }

    [Fact]
    public async Task GetAllAsync_WhenPaged_ReturnsRequestedPage()
    {
        await rawMaterialService.CreateAsync(new CreateRawMaterialDTO { material_code = "RM-001", material_name = "Apple", uom = "kg" });
        await rawMaterialService.CreateAsync(new CreateRawMaterialDTO { material_code = "RM-002", material_name = "Banana", uom = "kg" });
        await rawMaterialService.CreateAsync(new CreateRawMaterialDTO { material_code = "RM-003", material_name = "Cherry", uom = "kg" });

        var result = await rawMaterialService.GetAllAsync(2, 2);

        Assert.Single(result);
        Assert.Equal(["Cherry"], result.Select(material => material.material_name));
    }

    [Fact]
    public async Task CreateAsync_PersistsRawMaterial()
    {
        var result = await rawMaterialService.CreateAsync(new CreateRawMaterialDTO
        {
            material_code = "RM-001",
            material_name = "Tomato",
            material_desc = "Fresh tomatoes",
            uom = "kg",
            is_active = true
        });

        var persisted = await dbContext.RawMaterials.SingleAsync();

        Assert.Equal(persisted.id, result.id);
        Assert.Equal("RM-001", result.material_code);
        Assert.Equal("Tomato", result.material_name);
        Assert.Equal("kg", persisted.uom);
        Assert.True(persisted.is_active);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsRawMaterialsOrderedByName()
    {
        await rawMaterialService.CreateAsync(new CreateRawMaterialDTO { material_code = "RM-002", material_name = "Zucchini", uom = "kg" });
        await rawMaterialService.CreateAsync(new CreateRawMaterialDTO { material_code = "RM-001", material_name = "Apple", uom = "kg" });

        var materials = await rawMaterialService.GetAllAsync(1, 10);

        Assert.Equal(["Apple", "Zucchini"], materials.Select(material => material.material_name));
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsPersistedMaterial()
    {
        var created = await rawMaterialService.CreateAsync(new CreateRawMaterialDTO
        {
            material_code = "RM-003",
            material_name = "Onion",
            uom = "kg"
        });

        var result = await rawMaterialService.GetByIdAsync(created.id);

        Assert.NotNull(result);
        Assert.Equal(created.id, result!.id);
        Assert.Equal("RM-003", result.material_code);
    }

    [Fact]
    public async Task UpdateAsync_UpdatesMaterialData()
    {
        var created = await rawMaterialService.CreateAsync(new CreateRawMaterialDTO
        {
            material_code = "RM-004",
            material_name = "Old name",
            uom = "kg"
        });

        var updated = await rawMaterialService.UpdateAsync(created.id, new UpdateRawMaterialDTO
        {
            material_code = "RM-004",
            material_name = "New name",
            material_desc = "Updated description",
            uom = "box",
            is_active = false
        });

        var result = await rawMaterialService.GetByIdAsync(created.id);

        Assert.True(updated);
        Assert.NotNull(result);
        Assert.Equal("New name", result!.material_name);
        Assert.Equal("Updated description", result.material_desc);
        Assert.Equal("box", result.uom);
        Assert.False(result.is_active);
    }

    public void Dispose()
    {
        dbContext.Dispose();
        connection.Dispose();
    }
}
