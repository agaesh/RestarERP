Sure. I’d make `raw-materials-agent.md` a concise implementation guide for the coding agent, matching your existing Product Service architecture.

````md
# Raw Materials Agent

## Objective

Implement the Raw Material Setup module within the Product Service.

Raw materials represent the materials used by restaurant products and will later be referenced by Product BOM.

---

## RawMaterial Model

```text
RawMaterial
----------------
id
material_code
material_name
material_desc
uom
is_active
create_date
update_date
````

---

## Architecture

Follow the existing Clean Architecture layered approach used by the Product module:

```text
RawMaterialEndpoints
        ↓
RawMaterialService
        ↓
RawMaterialRepository
        ↓
Database
```

### Required Components

1. `RawMaterial` Entity/Model
2. `RawMaterialRepository`
3. `IRawMaterialRepository`
4. `RawMaterialService`
5. `IRawMaterialService`
6. Create DTO
7. Update DTO
8. Read DTO
9. List DTO
10. `RawMaterialEndpoints`
11. Dependency Injection configuration
12. EF Core migration
13. Unit tests
14. API endpoint tests

---

## Entity Requirements

Create the `RawMaterial` entity with:

* `id`
* `material_code`
* `material_name`
* `material_desc`
* `uom`
* `is_active`
* `create_date`
* `update_date`

Follow the existing project conventions for:

* Naming
* Data types
* Primary keys
* Nullable fields
* Date/time handling
* Entity configuration
* Database constraints

Do not introduce unnecessary fields.

---

## Repository

Create:

```text
IRawMaterialRepository
RawMaterialRepository
```

The repository should handle database access only.

Required operations:

* Create
* Get by ID
* Get all
* Update
* Support active/inactive filtering where appropriate

Do not place business logic inside the repository.

---

## Service

Create:

```text
IRawMaterialService
RawMaterialService
```

The service should contain business/application logic.

Responsibilities:

* Validate input
* Create raw material
* Retrieve raw material
* List raw materials
* Update raw material
* Handle active/inactive status
* Map between DTOs and entities

Do not expose database entities directly through the API.

---

## DTOs

Create separate DTOs for:

### Create

```text
CreateRawMaterialDto
```

### Update

```text
UpdateRawMaterialDto
```

### Read

```text
RawMaterialDto
```

### List

```text
RawMaterialListDto
```

Follow the existing Product DTO conventions.

---

## API Endpoints

Create endpoints under:

```text
/raw-materials
```

Required operations:

```text
POST   /raw-materials
GET    /raw-materials
GET    /raw-materials/{id}
PUT    /raw-materials/{id}
```

Follow the existing Product endpoint conventions for:

* HTTP status codes
* Request validation
* Response structure
* Error handling
* Logging

---

## Validation

Required fields should be validated.

At minimum:

* `material_code`
* `material_name`
* `uom`

Validate appropriate string lengths and prevent invalid empty values.

`material_code` should be treated as the unique identifier/code for the raw material from a business perspective.

---

## Dependency Injection

Register:

```text
IRawMaterialRepository → RawMaterialRepository
IRawMaterialService → RawMaterialService
```

Follow the existing dependency injection structure in the Product Service.

---

## Database

Create the required EF Core entity configuration and migration.

Ensure:

* Primary key is configured.
* Required fields are configured.
* Appropriate maximum lengths are configured.
* `material_code` has a unique constraint/index if consistent with the existing database conventions.
* `is_active` has an appropriate default if required by the project conventions.
* `create_date` and `update_date` are handled consistently with existing entities.

---

## Testing

Add unit tests for:

* Create raw material
* Get raw material by ID
* Get all raw materials
* Update raw material
* Validation failures
* Active/inactive handling
* Repository/service error scenarios where applicable

Add API endpoint tests for:

* `POST /raw-materials`
* `GET /raw-materials`
* `GET /raw-materials/{id}`
* `PUT /raw-materials/{id}`
* Invalid request handling
* Not-found scenarios

Run the complete automated test suite after implementation.

---

## Out of Scope

Do NOT implement:

* Stock quantity
* Inventory transactions
* Supplier information
* Purchase pricing/cost
* UOM conversion
* Batch tracking
* Expiry tracking
* Brand
* Inventory management
* Purchasing
* Product BOM implementation

These will be implemented as separate modules/issues.

---

## Implementation Rules

* Follow the existing Product Service architecture and coding conventions.
* Reuse existing infrastructure where possible.
* Do not duplicate existing shared functionality.
* Keep controllers/endpoints thin.
* Keep business logic in the service layer.
* Keep database operations in the repository layer.
* Use DTOs for API requests and responses.
* Do not expose EF Core entities directly through API responses.
* Add tests alongside the implementation.
* Keep commits focused and logically separated.
* Do not introduce unnecessary abstractions or dependencies.

---

## Acceptance Criteria

* [x] Raw Material entity created.
* [x] Database configuration created.
* [x] EF Core migration created.
* [x] Repository and interface implemented.
* [x] Service and interface implemented.
* [x] Create DTO implemented.
* [x] Update DTO implemented.
* [x] Read DTO implemented.
* [x] List DTO implemented.
* [x] CRUD endpoints implemented.
* [x] Dependency injection configured.
* [x] Validation implemented.
* [x] Active/inactive status supported.
* [x] Unit tests implemented.
* [x] API endpoint tests implemented.
* [x] Migration applies successfully.
* [x] All automated tests pass.

```
```
