using Backend.Builders;
using Backend.Common;
using Backend.Dtos.QrPayments.Output;

namespace Test.Builders;

[TestFixture]
public class QrPaymentViewBuilderTests
{
    private QrPaymentViewBuilder sut = null!;

    [SetUp]
    public void Setup() => sut = new QrPaymentViewBuilder();

    [Test]
    public void BuildReadyStatus_WithUrl_SetsStatusAndUrl()
    {
        var id = Guid.NewGuid();

        QrDebtStatusDto status = sut.BuildReadyStatus(id, "http://q.test");

        Assert.Multiple(() =>
        {
            Assert.That(status.IdentificadorDeuda, Is.EqualTo(id));
            Assert.That(status.Status, Is.EqualTo("Ready"));
            Assert.That(status.QrSimpleUrl, Is.EqualTo("http://q.test"));
            Assert.That(status.Error, Is.Null);
        });
    }

    [Test]
    public void BuildFailedStatus_WithError_CarriesReason()
    {
        var id = Guid.NewGuid();

        QrDebtStatusDto status = sut.BuildFailedStatus(id, "todotix down");

        Assert.Multiple(() =>
        {
            Assert.That(status.Status, Is.EqualTo("Failed"));
            Assert.That(status.Error, Is.EqualTo("todotix down"));
            Assert.That(status.QrSimpleUrl, Is.Null);
        });
    }

    [Test]
    public void BuildPendingStatus_HasPendingLabelOnly()
    {
        var id = Guid.NewGuid();

        QrDebtStatusDto status = sut.BuildPendingStatus(id);

        Assert.Multiple(() =>
        {
            Assert.That(status.Status, Is.EqualTo("Pending"));
            Assert.That(status.IdentificadorDeuda, Is.EqualTo(id));
        });
    }

    [Test]
    public void BuildPage_WrapsItemsWithIndices()
    {
        List<string> items = ["a", "b"];

        PageDto<string> page = sut.BuildPage(2, 4, items);

        Assert.Multiple(() =>
        {
            Assert.That(page.CurrentIndex, Is.EqualTo(2));
            Assert.That(page.MaxIndex, Is.EqualTo(4));
            Assert.That(page.Items, Is.EqualTo(items));
        });
    }
}
