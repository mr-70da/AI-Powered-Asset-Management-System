namespace Kinana.AssetManagement.Application.Exceptions;

public sealed class ValidationException : ApiException
{
    public ValidationException(string message)
        : base(400, message)
    {
    }
}
