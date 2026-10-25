namespace RestarProduct.DTOs;

public sealed record ApiResponse(
    bool Success,
    string Message,
    string? Error = null);
