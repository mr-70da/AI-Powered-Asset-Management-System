namespace Kinana.AssetManagement.Application.Exceptions;

public sealed class NotFoundException : ApiException
{
    public NotFoundException(string message)
        : base(404, message)
    {
    }
}
