using PonPon.Modules.Payment.Application.Abstractions;

namespace PonPon.Modules.Payment.Application.Features.GetChargeStatus;

public sealed class GetChargeStatusHandler
{
    private readonly IOmiseClient _omise;

    public GetChargeStatusHandler(IOmiseClient omise) => _omise = omise;

    public async Task<GetChargeStatusResponse> HandleAsync(GetChargeStatusQuery query, CancellationToken cancellationToken)
    {
        var result = await _omise.GetChargeAsync(query.ChargeId, cancellationToken);

        return new GetChargeStatusResponse(result.ChargeId, result.Status, result.Paid, result.PaidAt, result.FailureCode, result.FailureMessage);
    }
}
