namespace PonPon.Modules.Ordering.Application.Features.Orders.SyncOrdersFromZort;

public sealed record SyncOrdersFromZortResponse(
    int TotalFetched,
    int Created,
    int Updated,
    int Failed,
    IReadOnlyCollection<string> Errors);
