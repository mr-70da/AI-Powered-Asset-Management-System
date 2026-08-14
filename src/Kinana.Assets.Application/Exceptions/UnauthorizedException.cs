namespace Kinana.AssetManagement.Application.Exceptions;

public sealed class UnauthorizedException : ApiException
{
    public UnauthorizedException(string message)
        : base(401, message)
    {
    }
}
