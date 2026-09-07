import pytest
from sqlalchemy import create_engine
from sqlalchemy.orm import sessionmaker
from sqlalchemy.pool import StaticPool

import supplier_service as app_module
from database import Base
from supplier import Supplier


@pytest.fixture
def client(monkeypatch):
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


def add_supplier(session_factory, supplier_name, email_address, contact_no, address_line1=None, status="ACTIVE"):
    with session_factory() as session:
        supplier = Supplier(
            supplier_name=supplier_name,
            email_address=email_address,
            contact_no=contact_no,
            address_line1=address_line1,
            status=status,
        )
        session.add(supplier)
        session.commit()
        return supplier.id


def test_get_suppliers_paginates_and_searches(client):
    add_supplier(app_module.SessionLocal, "Acme Supply", "acme@example.com", "123")
    add_supplier(app_module.SessionLocal, "Acme Goods", "goods@example.com", "456")
    add_supplier(app_module.SessionLocal, "Beta Parts", "beta@example.com", "789")

    response = client.get("/suppliers?page=1&limit=1&search=Acme")

    assert response.status_code == 200
    body = response.get_json()

    assert [supplier["supplier_name"] for supplier in body["suppliers"]] == ["Acme Supply"]
    assert body["pagination"] == {
        "page": 1,
        "limit": 1,
        "total": 2,
        "pages": 2,
    }


def test_get_suppliers_returns_404_when_empty(client):
    response = client.get("/suppliers?search=Missing")

    assert response.status_code == 404
    assert response.get_json() == {"message": "No suppliers found"}


def test_post_supplier_creates_supplier(client):
    response = client.post(
        "/suppliers",
        json={
            "supplier_name": "Northwind",
            "email_address": "northwind@example.com",
            "contact_no": "555-0100",
            "address_line1": "New York",
            "city": "Kuala Lumpur",
            "state": "Wilayah Persekutuan",
            "postal_code": "50088",
            "status": "ACTIVE",
        },
    )

    assert response.status_code == 201
    assert response.get_json()["message"] == "Supplier created"


def test_post_supplier_validates_required_fields(client):
    response = client.post(
        "/suppliers",
        json={
            "company_name": "Missing fields",
        },
    )

    assert response.status_code == 400
    assert response.get_json()["message"] == "Validation failed"
    assert "email is required" in response.get_json()["errors"]
    assert "phone is required" in response.get_json()["errors"]


def test_delete_supplier_deactivates_supplier(client):
    supplier_id = add_supplier(app_module.SessionLocal, "Close Supplier", "close@example.com", "321")

    response = client.delete(f"/suppliers/{supplier_id}")

    assert response.status_code == 200
    with app_module.SessionLocal() as session:
        assert session.get(Supplier, supplier_id).status == "INACTIVE"


@pytest.mark.parametrize("tin_no", ["1234567890123", "123456789012345"])
def test_post_supplier_rejects_tin_no_unless_exactly_14_characters(client, tin_no):
    response = client.post(
        "/suppliers",
        json={
            "supplier_name": "TIN Supplier",
            "email_address": "tin@example.com",
            "contact_no": "555-0100",
            "tin_no": tin_no,
        },
    )

    assert response.status_code == 400
    assert response.get_json() == {
        "message": "Validation failed",
        "errors": ["tin_no: Value error, tin_no must be exactly 14 characters"],
    }

    with app_module.SessionLocal() as session:
        assert session.query(Supplier).count() == 0
