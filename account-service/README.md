# Account Service

A Flask microservice for creating, listing, searching, paginating, and deactivating accounts. SQLAlchemy connects the service to SQL Server, while endpoint tests use an isolated in-memory SQLite database.

## Requirements

- Python 3.14 or compatible Python version
- SQL Server and the Microsoft ODBC Driver 17 for SQL Server
- A configured `account-service/.env` file

Example `.env`:

```env
DATABASE_URL=mssql+pyodbc:///?odbc_connect=Driver={ODBC Driver 17 for SQL Server};Server=(localdb)\MSSQLLocalDB;Database=Restar;Trusted_Connection=yes;
```

## Setup

From the repository root:

```powershell
cd C:\Users\agaes\restar\account-service
.\.venv\Scripts\python.exe -m pip install Flask SQLAlchemy python-dotenv alembic pyodbc pytest
```

The project virtual environment is located at `account-service/.venv`.

## Database Migration

Apply the database schema to the SQL Server configured in `.env`:

```powershell
cd C:\Users\agaes\restar\account-service
.\.venv\Scripts\python.exe -m alembic upgrade head
```

Use `python -m alembic` instead of the `alembic.exe` launcher if the virtual environment has been moved, because the launcher may retain an old Python path.

## Run the Service

```powershell
cd C:\Users\agaes\restar\account-service
.\.venv\Scripts\python.exe -m flask --app app run --debug
```

The service runs at:

```text
http://127.0.0.1:5000
```

## API Endpoints

### List accounts

```http
GET /accounts
```

Optional query parameters:

- `page`: page number, starting at `1`; defaults to `1`
- `limit`: number of records per page; defaults to `10`
- `search`: case-insensitive search against `account_name`

Example:

```text
GET http://127.0.0.1:5000/accounts?page=1&limit=2&search=Cash
```

Successful response:

```json
{
  "message": "Accounts retrieved successfully",
  "accounts": [
    {
      "id": 1,
      "account_code": "1000",
      "account_name": "Cash",
      "account_type": "ASSET",
      "parent_id": null,
      "normal_balance": "DEBIT",
      "description": null,
      "is_active": true,
      "created_at": "2026-08-19T12:00:00",
      "updated_at": null
    }
  ],
  "pagination": {
    "page": 1,
    "limit": 2,
    "total": 1,
    "pages": 1
  }
}
```

If no matching records exist, the endpoint returns `404`:

```json
{
  "message": "No accounts found"
}
```

### Create an account

```http
POST /accounts
Content-Type: application/json
```

Example request:

```json
{
  "account_code": "4000",
  "account_name": "Revenue",
  "account_type": "INCOME",
  "normal_balance": "CREDIT",
  "description": "Operating revenue",
  "is_active": true
}
```

Successful response, HTTP `201`:

```json
{
  "message": "Account created",
  "id": 4
}
```

### Deactivate an account

This is a soft delete: it sets `is_active` to `false`.

```http
DELETE /accounts?id=4
```

An account cannot be deactivated while it has active child accounts.

## Tests

Tests use an in-memory SQLite database, so they do not modify SQL Server data:

```powershell
cd C:\Users\agaes\restar\account-service
.\.venv\Scripts\python.exe -m pytest tests\test_accounts.py -q
```

The suite covers:

- Account listing, pagination, and name search
- Empty search results
- Account creation
- Missing account IDs
- Account deactivation
- Protection of parents with active child accounts

SQLite tests verify endpoint behavior quickly. Run the service and migration against SQL Server separately to verify the production database connection and SQL Server-specific behavior.
