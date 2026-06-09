using System.Reflection;

using NetArchTest.Rules;

namespace Test.Architecture;

[TestFixture]
public class OutboxPublishingRulesTests
{
    private static readonly Assembly BackendAssembly = typeof(Backend.Modules.HealthCheckModule).Assembly;

    [Test]
    public void Types_OutsideMessagingWorkersModulesAndExternalCheck_DoNotDependOnRabbitMqClient()
    {
        TestResult result = Types.InAssembly(BackendAssembly)
            .That()
            .DoNotResideInNamespaceStartingWith("Backend.Messaging")
            .And().DoNotResideInNamespaceStartingWith("Backend.Workers")
            .And().DoNotResideInNamespaceStartingWith("Backend.Modules")
            .And().DoNotResideInNamespaceStartingWith("Backend.ExternalCheck")
            .ShouldNot()
            .HaveDependencyOn("RabbitMQ.Client")
            .GetResult();

        Assert.That(result.IsSuccessful, Is.True, DescribeFailures(result));
    }

    private static string DescribeFailures(TestResult result)
    {
        if (result.IsSuccessful)
        {
            return string.Empty;
        }

        return "Offending types: " + string.Join(", ", result.FailingTypeNames);
    }
}
