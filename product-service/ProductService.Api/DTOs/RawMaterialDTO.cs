namespace RestarProduct.DTOs;

public class RawMaterialDTO
{
    public int id { get; set; }
    public string material_code { get; set; } = string.Empty;
    public string material_name { get; set; } = string.Empty;
    public string? material_desc { get; set; }
    public string? uom { get; set; }
    public bool is_active { get; set; }
    public DateTime create_date { get; set; }
    public DateTime? update_date { get; set; }
}
