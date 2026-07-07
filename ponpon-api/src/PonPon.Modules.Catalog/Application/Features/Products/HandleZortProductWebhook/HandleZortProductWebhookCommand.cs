namespace PonPon.Modules.Catalog.Application.Features.Products.HandleZortProductWebhook;

public sealed record HandleZortProductWebhookCommand(long ZortProductId, string Method);
