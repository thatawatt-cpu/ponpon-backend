namespace PonPon.Modules.Identity.Infrastructure.Line;

public sealed record LineTokenVerifyRequest(string IdToken, string ClientId);
