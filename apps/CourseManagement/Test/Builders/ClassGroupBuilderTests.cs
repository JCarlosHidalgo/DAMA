using Backend.Builders;
using Backend.Dtos.Groups.Input;
using Backend.Entities.Groups;

namespace Test.Builders;

[TestFixture]
public class ClassGroupBuilderTests
{
    private ClassGroupBuilder _builder = null!;

    [SetUp]
    public void SetUp() => _builder = new ClassGroupBuilder();

    [Test]
    public void BuildClassGroup_CopiesNameAndAssignsTenantAndId()
    {
        var tenantId = Guid.NewGuid();
        var payload = new CreateClassGroupDto { Name = "Grupo Demo" };

        ClassGroup result = _builder.BuildClassGroup(tenantId, payload);

        Assert.Multiple(() =>
        {
            Assert.That(result.Id, Is.Not.EqualTo(Guid.Empty));
            Assert.That(result.Name, Is.EqualTo("Grupo Demo"));
            Assert.That(result.TenantId, Is.EqualTo(tenantId));
        });
    }

    [Test]
    public void BuildClassGroup_GeneratesUniqueIdPerInvocation()
    {
        var payload = new CreateClassGroupDto { Name = "Grupo" };

        ClassGroup first = _builder.BuildClassGroup(Guid.NewGuid(), payload);
        ClassGroup second = _builder.BuildClassGroup(Guid.NewGuid(), payload);

        Assert.That(first.Id, Is.Not.EqualTo(second.Id));
    }
}
