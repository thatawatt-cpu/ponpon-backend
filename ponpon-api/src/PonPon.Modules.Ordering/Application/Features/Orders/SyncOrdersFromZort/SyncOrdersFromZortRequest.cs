namespace PonPon.Modules.Ordering.Application.Features.Orders.SyncOrdersFromZort;

public sealed record SyncOrdersFromZortRequest(
    int PageStart = 1,
    int PageLimit = 100,
    int? MaxPages = null);
