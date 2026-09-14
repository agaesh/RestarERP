# Product Service

## Implemented

- ASP.NET Core .NET 10 Web API.
- Product CRUD endpoints under `/products`.
- Controller, service, repository, and `Interfaces/` layers.
- Product create, update, read, and list DTOs.
- `tax_id` used consistently across the model, DTOs, mappings, and tests.
- SQL Server configuration for the `Restar_Products` database.
- Structured logging through `ILogger<ProductService>`.
- xUnit service tests using EF Core SQLite in-memory storage.
- Tests cover create, list, get by ID, update, delete, and tax ID persistence.

## Product BOM Required Scope

Product BOM (Bill of Materials) support still needs to be implemented:

- BOM and BOM item models.
- Relationship between a parent product and component products.
- Create, update, read, list, and delete DTOs and endpoints.
- Validation for component quantity and product relationships.
- Repository and service interfaces and implementations.
- SQL Server migration for BOM tables.
- SQLite-backed unit tests for BOM operations.
