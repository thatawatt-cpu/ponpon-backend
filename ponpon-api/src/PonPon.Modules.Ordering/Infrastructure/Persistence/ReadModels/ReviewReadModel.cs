namespace PonPon.Modules.Ordering.Infrastructure.Persistence.ReadModels;

public sealed class ReviewReadModel
{
    public Guid Id { get; set; }
    public Guid OrderItemId { get; set; }
    public DateTime? DeletedAtUtc { get; set; }
}
