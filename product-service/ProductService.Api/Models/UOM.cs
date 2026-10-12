using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RestarProduct.Models;

[Table("UOMSetup")]
public class UOM
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Required]
    [Column("code")]
    public string Code { get; set; } = string.Empty;

    [Required]
    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Column("description")]
    public string? Description { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("is_delete")]
    public bool IsDelete { get; set; } = false;

    [Column("create_date", TypeName = "datetime2")]
    public DateTime CreateDate { get; set; } = DateTime.UtcNow;

    [Column("update_date", TypeName = "datetime2")]
    public DateTime? UpdateDate { get; set; }
}
