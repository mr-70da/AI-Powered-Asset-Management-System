namespace Kinana.AssetManagement.Application.Exceptions;

public abstract class ApiException : Exception
{
    public int StatusCode { get; }

    protected ApiException(int statusCode, string message)
        : base(message)
    {
        StatusCode = statusCode;
    }
}
