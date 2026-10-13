namespace RestarProduct.DTOs;

/// <summary>Represents a unit of measure returned by the API.</summary>
public sealed record UOMDTO(
    int id,
    string uom_code,
    string uom_name,
    string? description,
    bool is_active,
    DateTimeOffset create_date,
    DateTimeOffset? update_date);
