using PonPon.Shared.Application.Abstractions;

namespace PonPon.Shared.Infrastructure.Time;

public sealed class DateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;
}
