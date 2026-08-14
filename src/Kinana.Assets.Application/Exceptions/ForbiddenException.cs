namespace Kinana.AssetManagement.Application.Exceptions;

public sealed class ForbiddenException : ApiException
{
    public ForbiddenException(string message)
        : base(403, message)
    {
    }
}
