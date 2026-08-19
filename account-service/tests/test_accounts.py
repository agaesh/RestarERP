"""SQLite-backed tests for the account service endpoints."""

import datetime

import pytest
from sqlalchemy import create_engine
from sqlalchemy.orm import sessionmaker
from sqlalchemy.pool import StaticPool

import app as app_module
from app.core.database import Base
from app.models.accounts import Account


@pytest.fixture
def client(monkeypatch):
    """Provide a Flask test client backed by a fresh in-memory SQLite database."""
    engine = create_engine(
        "sqlite://",
        connect_args={"check_same_thread": False},
        poolclass=StaticPool,
    )
    Base.metadata.create_all(engine)
    test_session = sessionmaker(bind=engine)
    monkeypatch.setattr(app_module, "SessionLocal", test_session)

    app_module.app.config.update(TESTING=True)
    with app_module.app.test_client() as test_client:
        yield test_client

    Base.metadata.drop_all(engine)

def add_account(session_factory, code, name, account_type="ASSET", balance="DEBIT"):
    """Insert an account into the test database and return its generated ID."""
    with session_factory() as session:
        account = Account(
            account_code=code,
            account_name=name,
            account_type=account_type,
            normal_balance=balance,
            is_active=True,
            created_at=datetime.datetime.now(),
        )
        session.add(account)
        session.commit()
        return account.id


    # Test account listing with name search and pagination metadata.
def test_get_accounts_paginates_and_searches(client):
    """Verify account-name search and pagination metadata for matching accounts."""
    add_account(app_module.SessionLocal, "1000", "Cash")
    add_account(app_module.SessionLocal, "2000", "Cash Reserve")
    add_account(
        app_module.SessionLocal,
        "3000",
        "Accounts Payable",
        "LIABILITY",
        "CREDIT",
    )

    response = client.get("/accounts?page=1&limit=1&search=Cash")

    assert response.status_code == 200
    body = response.get_json()

    assert [account["account_name"] for account in body["accounts"]] == ["Cash"]
    assert body["pagination"] == {
        "page": 1,
        "limit": 1,
        "total": 2,
        "pages": 2,
    }


# Test that an empty search result returns HTTP 404.
def test_get_accounts_returns_404_when_empty(client):
    """Verify that a search with no matching accounts returns HTTP 404."""
    response = client.get("/accounts?search=Missing")

    assert response.status_code == 404
    assert response.get_json() == {"message": "No accounts found"}


# Test that a valid JSON body creates an account and returns its ID.
def test_post_account_creates_account(client):
    """Verify that POST /accounts creates an account and returns HTTP 201."""
    response = client.post(
        "/accounts",
        json={
            "account_code": "4000",
            "account_name": "Revenue",
            "account_type": "INCOME",
            "normal_balance": "CREDIT",
        },
    )

    assert response.status_code == 201
    assert response.get_json()["message"] == "Account created"


# Test that deleting without an account ID returns a validation error.
def test_delete_account_requires_id(client):
    """Verify that DELETE /accounts rejects requests without an account ID."""
    response = client.delete("/accounts")

    assert response.status_code == 400
    assert response.get_json() == {"message": "Account ID is required"}


# Test that deleting an account performs a soft delete by setting is_active false.
def test_delete_account_deactivates_account(client):
    """Verify that DELETE /accounts performs a soft delete."""
    account_id = add_account(app_module.SessionLocal, "5000", "Deactivatable")

    response = client.delete(f"/accounts?id={account_id}")

    assert response.status_code == 200
    with app_module.SessionLocal() as session:
        assert session.get(Account, account_id).is_active is False


    # Test that an account with active children cannot be deactivated.
def test_delete_account_rejects_active_parent(client):
    """Verify that an account with active children cannot be deactivated."""
    parent_id = add_account(app_module.SessionLocal, "6000", "Parent")
    child_id = add_account(app_module.SessionLocal, "6001", "Child")
    with app_module.SessionLocal() as session:
        session.get(Account, child_id).parent_id = parent_id
        session.commit()

    response = client.delete(f"/accounts?id={parent_id}")

    assert response.status_code == 400
    assert response.get_json()["message"] == (
        "Account cannot be deactivated because it has active child accounts"
    )
