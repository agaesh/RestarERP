using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RestarProduct.Models;

public class RawMaterial
{
    [Key]
    public int id { get; set; }

    [Required]
    [StringLength(50)]
    public string material_code { get; set; } = string.Empty;

    [Required]
    [StringLength(200)]
    public string material_name { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? material_desc { get; set; }

    [StringLength(50)]
    public string? uom { get; set; }

    public bool is_active { get; set; } = true;

    [Column(TypeName = "datetime2")]
    public DateTime create_date { get; set; } = DateTime.UtcNow;

    public DateTime? update_date { get; set; }
}
