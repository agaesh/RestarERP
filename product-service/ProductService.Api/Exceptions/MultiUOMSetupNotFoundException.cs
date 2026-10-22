namespace RestarProduct.Exceptions;

public sealed class MultiUOMSetupNotFoundException(int id)
    : Exception($"UOM setup with ID '{id}' does not exist.")
{
}
