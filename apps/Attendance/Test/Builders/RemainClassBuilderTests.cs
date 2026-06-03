using Backend.Builders;
using Backend.Entities.Remain;

namespace Test.Builders;

[TestFixture]
public class RemainClassBuilderTests
{
    private RemainClassBuilder _sut = null!;

    [SetUp]
    public void SetUp() => _sut = new RemainClassBuilder();

    [Test]
    public void BuildEmptyRemain_ReturnsZeroClassesAndNullName()
    {
        var tenantId = Guid.NewGuid();
        var studentId = Guid.NewGuid();

        StudentRemainClasses remain = _sut.BuildEmptyRemain(tenantId, studentId);

        Assert.Multiple(() =>
        {
            Assert.That(remain.TenantId, Is.EqualTo(tenantId));
            Assert.That(remain.Id, Is.EqualTo(studentId));
            Assert.That(remain.NumberOfClasses, Is.EqualTo(0));
            Assert.That(remain.StudentName, Is.Null);
        });
    }
}
