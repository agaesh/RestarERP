# Supplier Service

A Flask microservice for creating, listing, updating, viewing, and deactivating suppliers. SQLAlchemy provides database access, Pydantic validates supplier data, and the test suite uses an isolated in-memory SQLite database.

## Requirements

- Python 3.14 or compatible Python version
- SQLAlchemy, Flask, Pydantic, python-dotenv, Alembic, and pytest
- A configured `supplier-service/.env` file
- A database supported by the configured SQLAlchemy connection URL

Example `.env`:

```env
DATABASE_URL=sqlite:///supplier.db
```

For SQL Server, use the appropriate `mssql+pyodbc` connection URL and installed ODBC driver.

## Setup

From the repository root:

```powershell
cd C:\Users\agaes\RestarERP\supplier-service
.\.venv\Scripts\python.exe -m pip install Flask SQLAlchemy pydantic python-dotenv alembic pytest
```

The project virtual environment is located at `supplier-service/.venv`.

## Database Migration

Apply the Alembic schema:

```powershell
cd C:\Users\agaes\RestarERP\supplier-service
.\.venv\Scripts\python.exe -m alembic upgrade head
```

The runtime database connection is read from `DATABASE_URL` in `.env`. Update `alembic.ini` or configure the migration environment if Alembic should target a database different from its default URL.

## Run the Service

```powershell
cd C:\Users\agaes\RestarERP\supplier-service
.\.venv\Scripts\python.exe supplier_service.py
```

The service runs at:

```text
http://127.0.0.1:5001
```

## API Endpoints

### Health check

```http
GET /suppliers/health
```

Successful response, HTTP `200`:

```json
{
  "status": "ok"
}
```

### List suppliers

```http
GET /suppliers
```

Optional query parameters:

- `page`: page number, starting at `1`; defaults to `1`
- `limit`: records per page; defaults to `10`
- `search`: case-insensitive search against `supplier_name`
- `city`: exact city filter
- `state`: exact state filter
- `country`: country code filter
- `status`: supplier status; defaults to `ACTIVE`

Example:

```text
GET http://127.0.0.1:5001/suppliers?page=1&limit=10&search=Acme&country=MY
```

If no matching suppliers exist, the endpoint returns HTTP `404`:

```json
{
  "message": "No suppliers found"
}
```

### Create a supplier

```http
POST /suppliers
Content-Type: application/json
```

Example request:

```json
{
  "supplier_name": "Northwind Supplies",
  "email_address": "northwind@example.com",
  "contact_no": "555-0100",
  "address_line1": "1 Main Street",
  "city": "Kuala Lumpur",
  "state": "Wilayah Persekutuan",
  "postal_code": "50000",
  "country_code": "MY",
  "currency_code": "MYR",
  "status": "ACTIVE"
}
```

Successful response, HTTP `201`:

```json
{
  "message": "Supplier created",
  "id": 1
}
```

### Update a supplier

```http
PUT /suppliers/<supplier_id>
Content-Type: application/json
```

The request must include a valid email address and at least one contact number. The fields `id`, `supplier_code`, and `updated_at` cannot be changed.

Example:

```text
PUT http://127.0.0.1:5001/suppliers/1
```

```json
{
  "supplier_name": "Northwind Updated",
  "email_address": "updated@example.com",
  "contact_no": "555-0111"
}
```

Successful response, HTTP `200`:

```json
{
  "message": "Supplier updated successfully",
  "id": 1
}
```

### Supplier details

```http
GET /suppliers/<supplier_id>/details
```

Credit fields are hidden by default. Include them with `include_credit=true`:

```text
GET http://127.0.0.1:5001/suppliers/1/details?include_credit=true
```

Successful response, HTTP `200`:

```json
{
  "message": "Supplier details retrieved successfully",
  "supplier": {
    "id": 1,
    "supplier_name": "Northwind Supplies",
    "email_address": "northwind@example.com",
    "status": "ACTIVE",
    "credit_limit": 0,
    "credit_days": 0,
    "account_payable_id": null
  }
}
```

### Deactivate a supplier

```http
DELETE /suppliers/<supplier_id>
```

This is a soft delete: it changes the supplier status to `INACTIVE`.

Successful response, HTTP `200`:

```json
{
  "message": "Supplier deactivated successfully",
  "id": 1
}
```

## Validation

Supplier requests are rejected with HTTP `400` when validation fails. Important rules include:

- Email and at least one contact number are required.
- Supplier names must contain at least two characters.
- TIN numbers must contain exactly 14 characters.
- Website URLs must start with `http://` or `https://`.
- Country and currency codes must contain 2 or 3 letters.
- Credit limits and credit days cannot be negative.
- If `address_line1` is provided, `city`, `state`, and `postal_code` are required.
- Unknown request fields are rejected.

Example validation response:

```json
{
  "message": "Validation failed",
  "errors": [
    "tin_no: Value error, tin_no must be exactly 14 characters"
  ]
}
```

## Tests

Run the complete supplier-service test suite:

```powershell
cd C:\Users\agaes\RestarERP\supplier-service
.\.venv\Scripts\python.exe -m pytest tests -q
```

The tests use an in-memory SQLite database and do not modify the configured production database. The suite covers listing, pagination, searching, creation, validation, and supplier deactivation. The update, details, and health endpoints can also be exercised with the Flask test client or against the running service.
