using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using RestarProduct.Enums;

namespace RestarProduct.Models;

public class Product
{
    [Key]
    public int id { get; set; }

    public string product_code { get; set; } = string.Empty;

    public string product_name { get; set; } = string.Empty;

    public string? description { get; set; }

    public string? image_url { get; set; }

    public ProductType product_type { get; set; } = ProductType.Unknown;

    public bool is_active { get; set; } = true;
    public bool is_available { get; set; } = true;
  
    public string? uom { get; set; }
    public int? tax_id { get; set; }
    public string? barcode { get; set; }

    public string? vendor_barcode_no { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public DateTime create_date { get; set; } = DateTime.UtcNow;
    public DateTime? update_date { get; set; }
}