using System.Text.Json.Serialization;

namespace RestarProduct.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum MultiUOMRefType
{
    [JsonStringEnumMemberName("PRODUCT")]
    Product = 1,

    [JsonStringEnumMemberName("RAWMATERIAL")]
    RawMaterial = 2,
}