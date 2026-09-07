from flask import Flask, jsonify, request
from sqlalchemy import func, select

from database import SessionLocal
from supplier import Supplier
from validator import validate_supplier

app = Flask(__name__)


@app.route("/suppliers", methods=["GET"])
def get_suppliers():
    limit = max(request.args.get("limit", 10, type=int), 1)
    page = max(request.args.get("page", 1, type=int), 1)
    search = request.args.get("search", "", type=str)
    status = request.args.get("status","ACTIVE", type=str)
    city = request.args.get("city","", type =str)
    state = request.args.get("state", "", type =str)
    country = request.args.get("country", "",type = str)

    offset = (page - 1) * limit

    with SessionLocal() as session:
        filters = Supplier.supplier_name.ilike(f"%{search}%")
        filters &= Supplier.status == status

        if city:
            filters &= Supplier.city == city

        if state:
            filters &= Supplier.state == state

        if country:
            filters &= Supplier.country_code == country.upper()

        total = session.scalar(
            select(func.count()).select_from(Supplier).where(filters)
        )
        supplier_rows = session.scalars(
            select(Supplier)
            .where(filters)
            .order_by(Supplier.id)
            .offset(offset)
            .limit(limit)
        ).all()

        if not supplier_rows:
            return jsonify({"message": "No suppliers found"}), 404

        suppliers = [
            {
                column.name: getattr(supplier, column.name)
                for column in Supplier.__table__.columns
            }
            for supplier in supplier_rows
        ]

        return jsonify({
            "message": "Suppliers retrieved successfully",
            "suppliers": suppliers,
            "pagination": {
                "page": page,
                "limit": limit,
                "total": total,
                "pages": (total + limit - 1) // limit,
            },
        })


@app.route("/suppliers", methods=["POST"])
def create_supplier():
    data = request.get_json(silent=True) or {}
    validated = validate_supplier(data)

    if isinstance(validated, list):
        return jsonify({"message": "Validation failed", "errors": validated}), 400


    columns = {
        key: value
        for key, value in validated.items()
        if key in Supplier.__table__.columns.keys()
    }
    with SessionLocal() as session:
        supplier = Supplier(
            **columns
        )
        session.add(supplier)
        session.commit()

        return jsonify({"message": "Supplier created", "id": supplier.id}), 201


@app.route("/suppliers/<int:supplier_id>", methods=["PUT"])
def update_supplier(supplier_id):
    data = request.get_json(silent=True)

    if not data:
        return jsonify({
            "message": "Request data is required"
        }), 400

    validated_fields = validate_supplier(data)

    if isinstance(validated_fields, list):
        return jsonify({
            "message": "Validation failed",
            "errors": validated_fields
        }), 400

    with SessionLocal() as session:
        supplier = session.get(Supplier, supplier_id)

        if not supplier:
            return jsonify({
                "message": "Supplier not found"
            }), 404

        updateable_columns = {
            column.name
            for column in Supplier.__table__.columns
            if column.name not in {
                "id",
                "supplier_code",
                "updated_at",
            }
        }

        for field, value in validated_fields.items():
            if field in updateable_columns:
                setattr(supplier, field, value)

        session.commit()

        return jsonify({
            "message": "Supplier updated successfully",
            "id": supplier.id
        }), 200


@app.route("/suppliers/<int:supplier_id>/details", methods=["GET"])
def supplier_details(supplier_id):
    with SessionLocal() as session:
        supplier = session.get(Supplier, supplier_id)
        include_credit = request.args.get("include_credit", "").lower() in {
            "1",
            "true",
            "yes",
        }

        if not supplier:
            return jsonify({
                "message": "Supplier cannot be found with the given id"
            }), 404

        credit_columns = {"credit_limit", "credit_days", "account_payable_id"}
        supplier_detail = {
            column.name: getattr(supplier, column.name)
            for column in Supplier.__table__.columns
            if include_credit or column.name not in credit_columns
        }

        return jsonify({
            "message": "Supplier details retrieved successfully",
            "supplier": supplier_detail,
        }), 200

@app.route("/suppliers/<int:supplier_id>", methods=["DELETE"])
def deactivate_supplier(supplier_id):
    with SessionLocal() as session:
        supplier = session.get(Supplier, supplier_id)
        if not supplier:
            return jsonify({"message": "Supplier not found"}), 404

        supplier.status = "INACTIVE"
        session.commit()

        return jsonify({"message": "Supplier deactivated successfully", "id": supplier.id}), 200


@app.route("/suppliers/health", methods=["GET"])
def health_check():
    return jsonify({"status": "ok"}), 200


if __name__ == "__main__":
    app.run(debug=True, port=5001)
