from sqlalchemy import (
    Column,
    Integer,
    String,
    Boolean,
    DateTime,
    ForeignKey,
    func
)
from sqlalchemy.orm import relationship

from app.core.database import Base

class Account(Base):
    __tablename__ = "accounts"

    id = Column(
        Integer,
        primary_key=True
    )

    account_code = Column(
        String(20),
        nullable=False,
        unique=True
    )

    account_name = Column(
        String(100),
        nullable=False
    )

    account_type = Column(
        String(30),
        nullable=False
    )

    parent_id = Column(
        Integer,
        ForeignKey("accounts.id"),
        nullable=True
    )

    normal_balance = Column(
        String(10),
        nullable=False,
        default="DEBIT"
    )

    description = Column(
        String(100),
        nullable=True
    )

    is_active = Column(
        Boolean,
        nullable=False,
        default=True
    )

    created_at = Column(
        DateTime,
        nullable=False,
        server_default=func.current_timestamp()
    )

    updated_at = Column(
        DateTime,
        nullable=True
    )

    parent = relationship(
        "Account",
        remote_side=[id],
        backref="children"
    )