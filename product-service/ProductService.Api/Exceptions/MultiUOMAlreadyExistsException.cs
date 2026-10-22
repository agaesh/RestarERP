namespace RestarProduct.Exceptions;

public sealed class MultiUOMAlreadyExistsException(
    string altUOMId,
    string refType,
    string refId)
    : Exception($"Alternate UOM setup ID '{altUOMId}' already exists for {refType} '{refId}'.")
{
}
