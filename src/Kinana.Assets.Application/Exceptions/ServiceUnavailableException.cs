namespace Kinana.AssetManagement.Application.Exceptions;

public sealed class ServiceUnavailableException : ApiException
{
    public ServiceUnavailableException(string message)
        : base(503, message)
    {
    }
}
