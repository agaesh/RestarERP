from flask import Flask, jsonify, request
from sqlalchemy import func, select
import datetime
from app.core.database import SessionLocal
from app.models.accounts import Account

app = Flask(__name__)


@app.route("/accounts", methods=["GET"])
def get_accounts():
    limit = max(request.args.get("limit", 10, type=int), 1)
    page = max(request.args.get("page", 1, type=int), 1)
    search = request.args.get("search", "", type=str)
    offset = (page - 1) * limit

    with SessionLocal() as session:
        filters = Account.account_name.ilike(f"%{search}%")
        total = session.scalar(
            select(func.count()).select_from(Account).where(filters)
        )
        account_rows = session.scalars(
            select(Account)
            .where(filters)
            .order_by(Account.id)
            .offset(offset)
            .limit(limit)
        ).all()

        # added condition to send appropriate response back
        if not account_rows:
            return jsonify({
                "message": "No accounts found",
            }), 404
        else:
            return jsonify({
                "message": "Accounts retrieved successfully",
                "accounts": [
                    {
                        "id": account.id,
                        "account_code": account.account_code,
                        "account_name": account.account_name,
                        "account_type": account.account_type,
                        "parent_id": account.parent_id,
                        "normal_balance": account.normal_balance,
                        "description": account.description,
                        "is_active": account.is_active,
                        "created_at": account.created_at.isoformat()
                        if account.created_at else None,
                        "updated_at": account.updated_at.isoformat()
                        if account.updated_at else None,
                    }
                    for account in account_rows
                ],
                "pagination": {
                    "page": page,
                    "limit": limit,
                    "total": total,
                    "pages": (total + limit - 1) // limit,
                },
            })

#Route to Insert Accounts
@app.route("/accounts", methods=["POST"])
def insert_account():
    data = request.get_json()

    with SessionLocal() as session:
        account = Account(
            account_code=data.get("account_code"),
            account_name=data.get("account_name"),
            account_type=data.get("account_type"),
            parent_id = data.get("parent_id"),
            normal_balance=data.get("normal_balance"),
            description=data.get("description"),
            is_active=data.get("is_active", True),
            created_at= datetime.datetime.now(),
            updated_at=data.get("updated_at")
        )

        session.add(account)
        session.commit()

        return jsonify({
            "message": "Account created",
            "id": account.id
        }), 201
    
# Route to deactivate an account
@app.route("/accounts", methods=["DELETE"])
def delete_account():

    account_id = request.args.get("id", type=int)

    if not account_id:
        return jsonify({
            "message": "Account ID is required"
        }), 400

    with SessionLocal() as session:

        account = session.get(Account, account_id)

        if not account:
            return jsonify({
                "message": "Account not found"
            }), 404

        # Prevent deactivation if the account has active child accounts
        child_exists = session.scalar(
            select(Account.id)
            .where(
                Account.parent_id == account_id,
                Account.is_active == True
            )
            .limit(1)
        )

        if child_exists:
            return jsonify({
                "message": "Account cannot be deactivated because it has active child accounts"
            }), 400

        # Soft delete
        account.is_active = False
        account.updated_at = datetime.datetime.now()

        session.commit()

        return jsonify({
            "message": "Account deactivated successfully",
            "id": account.id
        }), 200
     
if __name__ == "__main__":
    app.run(debug=True)