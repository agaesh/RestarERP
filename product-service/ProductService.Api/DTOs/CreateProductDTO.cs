using System.ComponentModel.DataAnnotations;
using RestarProduct.Enums;

namespace RestarProduct.DTOs;

public class CreateProductDTO
{
	[Required(ErrorMessage = "Product name is required.")]
	[StringLength(200, MinimumLength = 2, ErrorMessage = "Product name must be between 2 and 200 characters.")]
	public string product_name { get; set; } = string.Empty;

	[StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters.")]
	public string? description { get; set; }

	[Url(ErrorMessage = "Image URL must be a valid URL.")]
	[StringLength(500, ErrorMessage = "Image URL cannot exceed 500 characters.")]
	public string? image_url { get; set; }

	public ProductType product_type { get; set; } = ProductType.Unknown;

	public bool is_active { get; set; } = true;
	public bool is_available { get; set; } = true;

	[StringLength(20)]
	public string? uom { get; set; }

	public int? tax_id { get; set; }

	[StringLength(100)]
	public string? barcode { get; set; }

	[StringLength(100)]
	public string? vendor_barcode_no { get; set; }	
}
