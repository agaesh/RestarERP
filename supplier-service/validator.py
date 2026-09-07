from decimal import Decimal
from typing import Literal

from pydantic import BaseModel, ConfigDict, ValidationError, field_validator, model_validator


class SupplierSchema(BaseModel):
    model_config = ConfigDict(extra="forbid")

    company_name: str | None = None
    supplier_name: str | None = None
    business_registration_no: str | None = None
    tin_no: str | None = None

    email_address: str | None = None
    website_url: str | None = None
    contact_no1: str | None = None
    contact_no2: str | None = None
    contact_no3: str | None = None
    contact_no: str | None = None

    address_line1: str | None = None
    address_line2: str | None = None
    city: str | None = None
    state: str | None = None
    postal_code: str | None = None
    country_code: str = "MY"

    currency_code: str = "MYR"

    credit_limit: Decimal = Decimal("0")
    credit_days: int = 0

    account_payable_id: int | None = None
    status: Literal["ACTIVE", "INACTIVE", "BLOCKED"] = "ACTIVE"

    @field_validator("company_name", "supplier_name")
    @classmethod
    def validate_company_name(cls, value: str | None) -> str | None:
        if value is None:
            return None
        value = value.strip()
        if not value:
            raise ValueError("company_name is required")
        if len(value) < 2:
            raise ValueError("company_name must be at least 2 characters")
        return value

    @field_validator("business_registration_no", "tin_no")
    @classmethod
    def validate_tax_fields(cls, value: str | None, info) -> str | None:
        if value is None:
            return None
        value = value.strip()
        if not value:
            return None
        if info.field_name == "tin_no" and len(value) != 14:
            raise ValueError("tin_no must be exactly 14 characters")
        if not value.replace("-", "").isalnum():
            raise ValueError("must contain only letters, numbers, and optional hyphen")
        return value

    @field_validator("email_address")
    @classmethod
    def validate_email_address(cls, value: str | None) -> str | None:
        if value is None:
            return None
        value = value.strip()
        if not value:
            return None
        if "@" not in value or "." not in value:
            raise ValueError("email_address is invalid")
        return value

    @field_validator("website_url")
    @classmethod
    def validate_website(cls, value: str | None) -> str | None:
        if value is None:
            return None
        value = value.strip()
        if not value:
            return None
        if not value.startswith(("http://", "https://")):
            raise ValueError("website_url must start with http:// or https://")
        return value

    @field_validator("contact_no1", "contact_no2", "contact_no3", "contact_no")
    @classmethod
    def validate_contact_no(cls, value: str | None) -> str | None:
        if value is None:
            return None
        value = value.strip()
        if not value:
            return None
        if not value.replace("+", "").replace("-", "").replace(" ", "").isdigit():
            raise ValueError("must contain only digits, spaces, +, and -")
        return value

    @field_validator("address_line1", "city", "state", "postal_code")
    @classmethod
    def validate_address_parts(cls, value: str | None, info) -> str | None:
        if value is None:
            return None
        value = value.strip()
        if not value:
            return None
        if info.field_name == "postal_code" and not value.replace("-", "").isalnum():
            raise ValueError("postal_code is invalid")
        return value

    @field_validator("country_code", "currency_code")
    @classmethod
    def validate_codes(cls, value: str) -> str:
        value = value.strip().upper()
        if len(value) not in (2, 3):
            raise ValueError("must be 2 or 3 uppercase characters")
        if not value.isalpha():
            raise ValueError("must contain only letters")
        return value

    @field_validator("credit_limit")
    @classmethod
    def validate_credit_limit(cls, value: Decimal) -> Decimal:
        if value < 0:
            raise ValueError("credit_limit cannot be negative")
        return value

    @field_validator("credit_days")
    @classmethod
    def validate_credit_days(cls, value: int) -> int:
        if value < 0:
            raise ValueError("credit_days cannot be negative")
        return value

    @field_validator("account_payable_id")
    @classmethod
    def validate_account_payable_id(cls, value: int | None) -> int | None:
        if value is not None and value <= 0:
            raise ValueError("account_payable_id must be positive")
        return value

    @model_validator(mode="after")
    def validate_contact_and_address(self):
        has_contact = any(
            value and value.strip()
            for value in [self.contact_no1, self.contact_no2, self.contact_no3, self.contact_no]
        )
        if not has_contact:
            raise ValueError("at least one contact number is required")

        has_address = any(
            value and value.strip()
            for value in [self.address_line1, self.city, self.state, self.postal_code]
        )
        if self.address_line1 is not None and self.address_line1.strip() and not self.city:
            raise ValueError("city is required when address_line1 is provided")
        if self.address_line1 is not None and self.address_line1.strip() and not self.state:
            raise ValueError("state is required when address_line1 is provided")
        if self.address_line1 is not None and self.address_line1.strip() and not self.postal_code:
            raise ValueError("postal_code is required when address_line1 is provided")

        if has_address and not self.address_line1:
            raise ValueError("address_line1 is required when address details are provided")

        return self


def validate_supplier(data: dict):
    payload = data or {}
    normalized = dict(payload)

    if "company_name" not in normalized and "supplier_name" in normalized:
        normalized["company_name"] = normalized["supplier_name"]
    if "supplier_name" not in normalized and "company_name" in normalized:
        normalized["supplier_name"] = normalized["company_name"]

    if "contact_no1" not in normalized and "contact_no" in normalized:
        normalized["contact_no1"] = normalized["contact_no"]
    if "contact_no" not in normalized and "contact_no1" in normalized:
        normalized["contact_no"] = normalized["contact_no1"]

    email_value = (normalized.get("email_address") or normalized.get("email") or "").strip()
    phone_value = (
        normalized.get("contact_no1")
        or normalized.get("contact_no")
        or normalized.get("phone")
        or ""
    ).strip()

    errors = []
    if not email_value:
        errors.append("email is required")
    if not phone_value:
        errors.append("phone is required")
    if errors:
        return errors

    try:
        validated = SupplierSchema.model_validate(normalized)

        if validated.contact_no1 is None and validated.contact_no is not None:
            validated.contact_no1 = validated.contact_no
        if validated.company_name is None and "name" in normalized:
            validated.company_name = normalized["name"]
        if validated.email_address is None and "email" in normalized:
            validated.email_address = normalized["email"]
        if validated.contact_no1 is None and "phone" in normalized:
            validated.contact_no1 = normalized["phone"]
        if validated.address_line1 is None and "address" in normalized:
            validated.address_line1 = normalized["address"]

        result = validated.model_dump(exclude_none=True)
        if "company_name" in result and "supplier_name" not in result:
            result["supplier_name"] = result["company_name"]
        if "contact_no1" in result and "contact_no" not in result:
            result["contact_no"] = result["contact_no1"]
        return result
    except ValidationError as exc:
        errors = []
        for error in exc.errors():
            field = ".".join(str(part) for part in error.get("loc", ()))
            message = error.get("msg", "invalid value")
            errors.append(f"{field}: {message}" if field else message)
        return errors
