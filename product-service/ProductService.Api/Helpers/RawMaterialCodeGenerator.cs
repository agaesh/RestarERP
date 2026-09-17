namespace RestarProduct.Helpers;

public static class RawMaterialCodeGenerator
{
	public static string Generate(int rawMaterialId)
	{
		if (rawMaterialId <= 0)
		{
			throw new ArgumentOutOfRangeException(nameof(rawMaterialId), "Raw material ID must be positive.");
		}

		return $"RM-{rawMaterialId:D6}";
	}
}
