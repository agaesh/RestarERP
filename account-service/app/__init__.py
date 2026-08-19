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
