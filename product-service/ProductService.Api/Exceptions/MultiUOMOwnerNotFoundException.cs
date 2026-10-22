namespace RestarProduct.Exceptions;

public sealed class MultiUOMOwnerNotFoundException(string refType, string refId)
    : Exception($"{refType} '{refId}' was not found.")
{
}
