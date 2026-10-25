using RestarProduct.Enums;

namespace RestarProduct.DTOs;

public sealed record MultiUOMDTO
{
    public int id { get; init; }
    public MultiUOMRefType ref_type { get; init; }
    public string ref_id { get; init; } = string.Empty;
    public int alt_uom_id { get; init; }
    public int base_uom_id { get; init; }
    public decimal? conversion_factor { get; init; }
}
