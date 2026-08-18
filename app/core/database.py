import os

from sqlalchemy import create_engine
from sqlalchemy.orm import sessionmaker
# 1. CHANGE: Import DeclarativeBase (Capitalized) instead of declarative_base
from sqlalchemy.orm import DeclarativeBase

DATABASE_URL = os.getenv("DATABASE_URL")

engine = create_engine(
    DATABASE_URL,
    echo=True
)

SessionLocal = sessionmaker(
    autocommit=False,
    autoflush=False,
    bind=engine
)

# 2. FIX: Inherit from the DeclarativeBase class directly
class Base(DeclarativeBase):
    pass
