namespace PonPon.Shared.Contracts;

public sealed record ValidationErrorResponse(IReadOnlyDictionary<string, string[]> Errors);
