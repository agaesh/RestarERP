using System.Security.Cryptography;

namespace RestarProduct.Helpers;

public static class ProductCodeGenerator
{
    public static string Generate(string productName)
    {
        var prefix = new string(productName
            .Where(char.IsLetterOrDigit)
            .Take(3)
            .ToArray())
            .ToUpperInvariant();

        prefix = prefix.PadRight(3, 'X');

        Span<byte> randomBytes = stackalloc byte[3];
        RandomNumberGenerator.Fill(randomBytes);
        var suffix = Convert.ToHexString(randomBytes);

        return $"{prefix}-{suffix}";
    }
}
