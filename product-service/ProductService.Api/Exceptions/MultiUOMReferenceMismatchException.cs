namespace RestarProduct.Exceptions;

public sealed class MultiUOMReferenceMismatchException()
    : Exception("The request reference type and ID must match the owner in the route.")
{
}
