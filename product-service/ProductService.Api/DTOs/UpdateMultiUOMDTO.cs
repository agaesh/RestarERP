using System.ComponentModel.DataAnnotations;
using RestarProduct.Enums;

namespace RestarProduct.DTOs;

public sealed record UpdateMultiUOMDTO
{
    [EnumDataType(typeof(MultiUOMRefType))]
    public MultiUOMRefType ref_type { get; init; }

    [Required(ErrorMessage = "Reference ID is required.")]
    public string ref_id { get; init; } = string.Empty;

    [Range(1, int.MaxValue, ErrorMessage = "Alternate UOM ID must be greater than zero.")]
    public int alt_uom_id { get; init; }

    [Range(1, int.MaxValue, ErrorMessage = "Base UOM ID must be greater than zero.")]
    public int base_uom_id { get; init; }

    [Range(typeof(decimal), "0.000001", "999999999999.999999",
        ErrorMessage = "Conversion factor must be greater than zero.")]
    public decimal? conversion_factor { get; init; }
}
