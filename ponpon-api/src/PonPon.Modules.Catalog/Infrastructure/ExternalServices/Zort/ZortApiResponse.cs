namespace PonPon.Modules.Catalog.Infrastructure.ExternalServices.Zort;

public sealed record ZortApiResponse(bool IsSuccess, string? ResCode, string? ResDesc, string RawJson);
