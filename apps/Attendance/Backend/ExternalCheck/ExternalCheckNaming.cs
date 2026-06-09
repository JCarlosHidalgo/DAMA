namespace Backend.ExternalCheck;

public static class ExternalCheckNaming
{
    public const string ServiceName = "AttendanceService";

    public static string Name(ExternalDependency dependency)
    {
        return $"{ServiceName}-{dependency}";
    }
}
