using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RestarProduct.Models;

[Table("UOMs")]
public class UOM
{
    [Key]
    public int id { get; set; }

    [Required]
    [StringLength(20)]
    public string uom_code { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string uom_name { get; set; } = string.Empty;

    [StringLength(500)]
    public string? description { get; set; }

    public bool is_active { get; set; } = true;

    public bool is_delete {get; set;} = false;

    [Column(TypeName = "datetime2")]
    public DateTime create_date { get; set; } = DateTime.UtcNow;

    [Column(TypeName = "datetime2")]
    public DateTime? update_date { get; set; }
}
