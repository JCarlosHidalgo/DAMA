namespace Backend.ExternalCheck;

public static class ExternalCheckNaming
{
    public const string ServiceName = "AuthService";

    public static string Name(ExternalDependency dependency)
    {
        return $"{ServiceName}-{dependency}";
    }
}
