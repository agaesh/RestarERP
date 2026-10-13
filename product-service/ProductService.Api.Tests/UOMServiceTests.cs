using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using RestarProduct.Data;
using RestarProduct.DTOs;
using RestarProduct.Repositories;
using RestarProduct.Services;

namespace ProductService.Api.Tests;

public sealed class UOMServiceTests : IDisposable
{
    private readonly SqliteConnection connection;
    private readonly ProductDbContext dbContext;
    private readonly UOMService service;

    public UOMServiceTests()
    {
        connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<ProductDbContext>()
            .UseSqlite(connection)
            .Options;

        dbContext = new ProductDbContext(options);
        dbContext.Database.EnsureCreated();
        service = new UOMService(
            new UOMRepository(dbContext),
            NullLogger<UOMService>.Instance);
    }

    // Creates a UOM with trimmed values and a creation timestamp.
    [Fact]
    public async Task CreateAsync_TrimsValuesAndSetsCreationTimestamp()
    {
        var beforeCreate = DateTimeOffset.UtcNow;

        var created = await service.CreateAsync(new CreateUOMDTO
        {
            uom_code = " KG ",
            uom_name = " Kilogram ",
            description = " Metric unit of mass ",
            is_active = true
        });

        var persisted = await dbContext.UOMs.SingleAsync();
        Assert.Equal(persisted.Id, created.id);
        Assert.Equal("KG", created.uom_code);
        Assert.Equal("Kilogram", created.uom_name);
        Assert.Equal("Metric unit of mass", created.description);
        Assert.True(created.is_active);
        Assert.InRange(created.create_date, beforeCreate, DateTimeOffset.UtcNow);
        Assert.Null(created.update_date);
    }

    // Returns UOMs ordered by their codes.
    [Fact]
    public async Task GetAllAsync_ReturnsUOMsInCodeOrder()
    {
        await service.CreateAsync(CreateRequest("ML", "Milliliter"));
        await service.CreateAsync(CreateRequest("G", "Gram"));

        var result = await service.GetAllAsync(1, 10);

        Assert.Equal(["G", "ML"], result.Select(uom => uom.uom_code));
    }

    // Returns only the requested page of UOMs.
    [Fact]
    public async Task GetAllAsync_ReturnsRequestedPage()
    {
        await service.CreateAsync(CreateRequest("A", "Unit A"));
        await service.CreateAsync(CreateRequest("B", "Unit B"));
        await service.CreateAsync(CreateRequest("C", "Unit C"));

        var result = await service.GetAllAsync(2, 1);

        Assert.Equal("B", Assert.Single(result).uom_code);
    }

    // Returns the requested UOM or null when its ID does not exist.
    [Fact]
    public async Task GetByIdAsync_ReturnsUOMOrNullWhenMissing()
    {
        var created = await service.CreateAsync(CreateRequest("KG", "Kilogram"));

        var result = await service.GetByIdAsync(created.id);
        var missing = await service.GetByIdAsync(-1);

        Assert.Equal(created.id, result?.id);
        Assert.Null(missing);
    }

    // Returns active UOMs but does not return a soft-deleted UOM.
    [Fact]
    public async Task GetByIdAsync_ReturnsActiveUOMAndNullForDeletedUOM()
    {
        var created = await service.CreateAsync(CreateRequest("KG", "Kilogram"));

        var activeUOM = await service.GetByIdAsync(created.id);
        Assert.NotNull(activeUOM);
        Assert.Equal(created.id, activeUOM!.id);
        Assert.Equal("KG", activeUOM.uom_code);

        var deleted = await service.DeleteAsync(created.id);
        var deletedUOM = await service.GetByIdAsync(created.id);

        Assert.True(deleted);
        Assert.Null(deletedUOM);
    }

    // Updates the UOM fields and sets the update timestamp.
    [Fact]
    public async Task UpdateAsync_UpdatesUOMAndSetsUpdateTimestamp()
    {
        var created = await service.CreateAsync(CreateRequest("KG", "Kilogram"));

        var updated = await service.UpdateAsync(created.id, new UpdateUOMDTO
        {
            uom_code = "G",
            uom_name = "Gram",
            description = "Unit of mass",
            is_active = false
        });

        var result = await service.GetByIdAsync(created.id);

        Assert.True(updated);
        Assert.NotNull(result);
        Assert.Equal("G", result!.uom_code);
        Assert.Equal("Gram", result.uom_name);
        Assert.Equal("Unit of mass", result.description);
        Assert.False(result.is_active);
        Assert.Equal(created.create_date, result.create_date);
        Assert.NotNull(result.update_date);
        Assert.True(result.update_date >= result.create_date);
    }

    // Returns false when attempting to update a UOM that does not exist.
    [Fact]
    public async Task UpdateAsync_ReturnsFalseWhenUOMDoesNotExist()
    {
        var updated = await service.UpdateAsync(-1, UpdateRequest());

        Assert.False(updated);
    }

    // Soft-deletes the UOM, updates its timestamp, and hides it from reads.
    [Fact]
    public async Task DeleteAsync_SoftDeletesUOMAndUpdatesTimestamp()
    {
        var created = await service.CreateAsync(CreateRequest("KG", "Kilogram"));

        var deleted = await service.DeleteAsync(created.id);
        var missing = await service.DeleteAsync(created.id);
        var persisted = await dbContext.UOMs.SingleAsync();

        Assert.True(deleted);
        Assert.False(missing);
        Assert.True(persisted.IsDelete);
        Assert.NotNull(persisted.UpdateDate);
        Assert.True(persisted.UpdateDate >= created.create_date.UtcDateTime);
        Assert.Empty(await service.GetAllAsync(1, 10));
        Assert.Null(await service.GetByIdAsync(created.id));
    }

    public void Dispose()
    {
        dbContext.Dispose();
        connection.Dispose();
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
