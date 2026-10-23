namespace RestarProduct.Exceptions;

public sealed class MultiUOMSetupNotFoundException(int id)
    : Exception($"UOM setup with ID '{id}' does not exist.")
{
}

public sealed class MultIUOMSetupNotFoundExceptionString(string code)
    : Exception($"UOM Setup with '{code}' does not exist ");