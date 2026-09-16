using System.Text.Json.Serialization;

namespace RestarProduct.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ProductType
{
    Unknown = 0,
    Food = 1,
    Beverage = 2,
    Addon = 3,
    Combo = 4,
    Packaging = 5
}