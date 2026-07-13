namespace PonPon.Shared.Application.Exceptions;

public class BadRequestException : Exception
{
    public BadRequestException(string message, string errorCode = "bad_request", object? details = null) : base(message)
    {
        ErrorCode = errorCode;
        Details = details;
    }

    public string ErrorCode { get; }
    public object? Details { get; }
}
