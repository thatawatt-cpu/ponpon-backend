namespace PonPon.Shared.Application.Abstractions;

public interface IDateTimeProvider
{
    DateTime UtcNow { get; }
}
