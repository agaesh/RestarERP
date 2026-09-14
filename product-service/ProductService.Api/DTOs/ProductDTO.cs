using RestarProduct.Enums;

namespace RestarProduct.DTOs;

public class ProductDTO
{
	public int id { get; set; }
	public string product_code { get; set; } = string.Empty;
	public string product_name { get; set; } = string.Empty;
	public string? description { get; set; }
	public string? image_url { get; set; }
	public ProductType product_type { get; set; }
	public bool is_active { get; set; }
	public bool is_available { get; set; }
	public string? uom { get; set; }
	public int? tax_id { get; set; }
	public string? barcode { get; set; }
	public string? vendor_barcode_no { get; set; }
	public DateTime create_date { get; set; }
	public DateTime? update_date { get; set; }
}
