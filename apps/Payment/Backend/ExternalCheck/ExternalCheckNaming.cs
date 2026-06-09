namespace Backend.ExternalCheck;

public static class ExternalCheckNaming
{
    public const string ServiceName = "PaymentService";

    public static string Name(ExternalDependency dependency)
    {
        return $"{ServiceName}-{dependency}";
    }
}
