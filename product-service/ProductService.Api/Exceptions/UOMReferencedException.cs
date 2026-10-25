namespace RestarProduct.Exceptions;

public sealed class UOMReferencedException(int id)
    : Exception($"UOM {id} is referenced by a multi-UOM setup and cannot be deleted or have its code changed.")
{
}
