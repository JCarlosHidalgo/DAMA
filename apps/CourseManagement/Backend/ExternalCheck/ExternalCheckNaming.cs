namespace Backend.ExternalCheck;

public static class ExternalCheckNaming
{
    public const string ServiceName = "CourseManagementService";

    public static string Name(ExternalDependency dependency)
    {
        return $"{ServiceName}-{dependency}";
    }
}
