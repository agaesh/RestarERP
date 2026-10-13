namespace RestarProduct.Exceptions;

public sealed class UOMCodeAlreadyExistsException(string code)
    : Exception($"A unit of measure with code '{code}' already exists.")
{
}
