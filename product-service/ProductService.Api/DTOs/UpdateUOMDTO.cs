using System.ComponentModel.DataAnnotations;

namespace RestarProduct.DTOs;

/// <summary>Payload for updating a unit of measure.</summary>
public sealed record UpdateUOMDTO
{
    [Required(ErrorMessage = "UOM code is required.")]
    [StringLength(20, MinimumLength = 1, ErrorMessage = "UOM code must be between 1 and 20 characters.")]
    public string uom_code { get; init; } = string.Empty;

    [Required(ErrorMessage = "UOM name is required.")]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "UOM name must be between 1 and 100 characters.")]
    public string uom_name { get; init; } = string.Empty;

    [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters.")]
    public string? description { get; init; }

    public bool is_active { get; init; }
}
