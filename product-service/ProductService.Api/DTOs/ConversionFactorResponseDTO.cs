namespace RestarProduct.DTOs;

public class ConversionFactorResponseDTO
{
    public int from_uom_id { get; init; }
    public int to_uom_id { get; init; }
    public decimal conversion_factor { get; init; }
}
