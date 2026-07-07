namespace PonPon.Modules.Ordering.Application.Pricing;

public interface IPricingStep
{
    int Order { get; }
    Task ExecuteAsync(PricingContext context, CancellationToken cancellationToken);
}

public sealed class PricingPipeline
{
    private readonly IReadOnlyCollection<IPricingStep> _steps;

    public PricingPipeline(IEnumerable<IPricingStep> steps)
    {
        _steps = steps.OrderBy(x => x.Order).ToArray();
    }

    public async Task<PricingResult> ExecuteAsync(
        PricingContext context,
        CancellationToken cancellationToken = default)
    {
        foreach (var step in _steps)
            await step.ExecuteAsync(context, cancellationToken);

        return PricingResult.From(context);
    }
}
