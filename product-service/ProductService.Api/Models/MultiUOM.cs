using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using RestarProduct.Enums;

namespace RestarProduct.Models;

public class MultiUOM
{
    [Key]
    public int id { get; set; }

    [Column("alt_uom_id")]
    public int altUOMId { get; set; }
    public UOM altUOM { get; set; } = null!;

    [Column("base_uom_id")]
    public int baseUOMId { get; set; }

    public UOM baseUOM { get; set; } = null!;

    public MultiUOMRefType refType { get; set; }

    [Column("ref_id")]
    public string refId { get; set; } = string.Empty;

    [Column("conversion_factor", TypeName = "decimal(10, 6)")]
    public decimal? conversionFactor { get; set; }

    [Column("created_at", TypeName = "datetime2")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at", TypeName = "datetime2")]
    public DateTime? UpdatedAt { get; set; }
}