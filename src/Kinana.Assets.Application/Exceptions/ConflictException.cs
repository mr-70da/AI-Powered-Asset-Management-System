namespace Kinana.AssetManagement.Application.Exceptions;

public sealed class ConflictException : ApiException
{
    public ConflictException(string message)
        : base(409, message)
    {
    }
}
