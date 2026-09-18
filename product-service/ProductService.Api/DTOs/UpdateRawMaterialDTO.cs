using System.ComponentModel.DataAnnotations;

namespace RestarProduct.DTOs;

public class UpdateRawMaterialDTO
{
    [Required(ErrorMessage = "Material code is required.")]
    [StringLength(50, ErrorMessage = "Material code cannot exceed 50 characters.")]
    public string material_code { get; set; } = string.Empty;

    [Required(ErrorMessage = "Material name is required.")]
    [StringLength(200, MinimumLength = 2, ErrorMessage = "Material name must be between 2 and 200 characters.")]
    public string material_name { get; set; } = string.Empty;

    [StringLength(1000, ErrorMessage = "Material description cannot exceed 1000 characters.")]
    public string? material_desc { get; set; }

    [Required(ErrorMessage = "UOM is required.")]
    [StringLength(50, ErrorMessage = "UOM cannot exceed 50 characters.")]
    public string? uom { get; set; }

    public bool is_active { get; set; } = true;
}
