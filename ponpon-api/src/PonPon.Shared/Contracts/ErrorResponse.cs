namespace PonPon.Shared.Contracts;

public sealed record ErrorResponse(string Code, string Message, object? Details = null);
