from sqlalchemy import Column, DateTime, Integer, Numeric, String
from sqlalchemy.orm import synonym

from database import Base


class Supplier(Base):
    __tablename__ = "suppliers"

    id = Column(Integer, primary_key=True, autoincrement=True)
    supplier_code = Column(String(20), nullable=True)

    supplier_name = Column(String(50), nullable=False)
    business_registration_no = Column(String(20), nullable=True)
    tin_no = Column(String(14), nullable=True)

    email_address = Column(String(50), nullable=True)
    website_url = Column(String(255), nullable=True)
    contact_no = Column(String(12), nullable=True)
    contact_no1 = Column(String(12), nullable=True)
    contact_no2 = Column(String(12), nullable=True)
    contact_no3 = Column(String(12), nullable=True)
    address_line1 = Column(String(50), nullable=True)
    address_line2 = Column(String(50), nullable=True)
    city = Column(String(100), nullable=True)
    state = Column(String(100), nullable=True)
    postal_code = Column(String(20), nullable=True)
    country_code = Column(String(3), nullable=False, default="MY")
    currency_code = Column(String(3), nullable=False, default="MYR")
    credit_limit = Column(Numeric(15, 2), nullable=False, default=0)
    
    credit_days = Column(Integer, nullable=False, default=0)
    account_payable_id = Column(Integer, nullable=True)
    status = Column(
        String(20),
        nullable=False,
        default="ACTIVE",
    )

    created_at = Column(DateTime, nullable=True)
    updated_at = Column(DateTime, nullable=True)

    @property
    def name(self):
        return self.company_name

    @name.setter
    def name(self, value):
        self.company_name = value

    @property
    def email(self):
        return self.email_address

    @email.setter
    def email(self, value):
        self.email_address = value

    @property
    def phone(self):
        return self.contact_no

    @phone.setter
    def phone(self, value):
        self.contact_no = value

    @property
    def address(self):
        return self.address_line1

    @address.setter
    def address(self, value):
        self.address_line1 = value

    @property
    def is_active(self):
        return self.status == "ACTIVE"

    @is_active.setter
    def is_active(self, value):
        self.status = "ACTIVE" if value else "INACTIVE"
