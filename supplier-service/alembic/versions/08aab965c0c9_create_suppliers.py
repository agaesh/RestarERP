"""create_suppliers

Revision ID: 08aab965c0c9
Revises: 
Create Date: 2026-09-02 15:44:35.237928

"""
from alembic import op
import sqlalchemy as sa


# revision identifiers, used by Alembic.
revision = '08aab965c0c9'
down_revision = None
branch_labels = None
depends_on = None


def upgrade() -> None:
    op.create_table(
        'suppliers',
        sa.Column('id', sa.Integer(), autoincrement=True, nullable=False),
        sa.Column('supplier_code', sa.String(length=20), nullable=True),
        sa.Column('supplier_name', sa.String(length=50), nullable=False),
        sa.Column('business_registration_no', sa.String(length=20), nullable=True),
        sa.Column('tin_no', sa.String(length=14), nullable=True),
        sa.Column('email_address', sa.String(length=50), nullable=True),
        sa.Column('website_url', sa.String(length=255), nullable=True),
        sa.Column('contact_no', sa.String(length=12), nullable=True),
        sa.Column('contact_no1', sa.String(length=12), nullable=True),
        sa.Column('contact_no2', sa.String(length=12), nullable=True),
        sa.Column('contact_no3', sa.String(length=12), nullable=True),
        sa.Column('address_line1', sa.String(length=50), nullable=True),
        sa.Column('address_line2', sa.String(length=50), nullable=True),
        sa.Column('city', sa.String(length=100), nullable=True),
        sa.Column('state', sa.String(length=100), nullable=True),
        sa.Column('postal_code', sa.String(length=20), nullable=True),
        sa.Column('country_code', sa.String(length=3), server_default='MY', nullable=False),
        sa.Column('currency_code', sa.String(length=3), server_default='MYR', nullable=False),
        sa.Column('credit_limit', sa.Numeric(precision=15, scale=2), server_default='0', nullable=False),
        sa.Column('credit_days', sa.Integer(), server_default='0', nullable=False),
        sa.Column('account_payable_id', sa.Integer(), nullable=True),
        sa.Column('status', sa.String(length=20), server_default='ACTIVE', nullable=False),
        sa.Column('created_at', sa.DateTime(), nullable=True),
        sa.Column('updated_at', sa.DateTime(), nullable=True),
        sa.PrimaryKeyConstraint('id'),
    )


def downgrade() -> None:
    op.drop_table('suppliers')
