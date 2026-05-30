using Grpc.Core;

namespace Backend.Grpc;

public static class GuidParser
{
    public static Guid ParseOrThrow(string rawValue, string parameterName)
    {
        if (Guid.TryParse(rawValue, out Guid parsedGuid))
        {
            return parsedGuid;
        }

        throw new RpcException(new Status(StatusCode.InvalidArgument, $"{parameterName} is not a valid GUID."));
    }
}
