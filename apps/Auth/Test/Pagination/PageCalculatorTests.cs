using Backend.Pagination;

namespace Test.Pagination;

[TestFixture]
public class PageCalculatorTests
{
    [Test]
    public void Resolve_WhenTotalCountIsZero_ReturnsZeroEffectiveIndexAndZeroMax()
    {
        (int effectivePageIndex, int maxPageIndex, int offset) =
            PageCalculator.Resolve(totalCount: 0, requestedPageIndex: 0, pageSize: 10);

        Assert.Multiple(() =>
        {
            Assert.That(effectivePageIndex, Is.EqualTo(0));
            Assert.That(maxPageIndex, Is.EqualTo(0));
            Assert.That(offset, Is.EqualTo(0));
        });
    }

    [Test]
    public void Resolve_WhenTotalCountIsNegative_ReturnsZeroEffectiveIndexAndZeroMax()
    {
        (int effectivePageIndex, int maxPageIndex, int offset) =
            PageCalculator.Resolve(totalCount: -5, requestedPageIndex: 3, pageSize: 10);

        Assert.Multiple(() =>
        {
            Assert.That(effectivePageIndex, Is.EqualTo(0));
            Assert.That(maxPageIndex, Is.EqualTo(0));
            Assert.That(offset, Is.EqualTo(0));
        });
    }

    [Test]
    public void Resolve_WhenTotalCountFitsInOnePage_ReturnsZeroMax()
    {
        (int effectivePageIndex, int maxPageIndex, int offset) =
            PageCalculator.Resolve(totalCount: 10, requestedPageIndex: 0, pageSize: 10);

        Assert.Multiple(() =>
        {
            Assert.That(effectivePageIndex, Is.EqualTo(0));
            Assert.That(maxPageIndex, Is.EqualTo(0));
            Assert.That(offset, Is.EqualTo(0));
        });
    }

    [Test]
    public void Resolve_WhenTotalCountSpansMultiplePages_ComputesMaxIndex()
    {
        (int effectivePageIndex, int maxPageIndex, int offset) =
            PageCalculator.Resolve(totalCount: 25, requestedPageIndex: 1, pageSize: 10);

        Assert.Multiple(() =>
        {
            Assert.That(effectivePageIndex, Is.EqualTo(1));
            Assert.That(maxPageIndex, Is.EqualTo(2));
            Assert.That(offset, Is.EqualTo(10));
        });
    }

    [Test]
    public void Resolve_WhenRequestedPageIndexExceedsMax_ClampsToMax()
    {
        (int effectivePageIndex, int maxPageIndex, int offset) =
            PageCalculator.Resolve(totalCount: 25, requestedPageIndex: 99, pageSize: 10);

        Assert.Multiple(() =>
        {
            Assert.That(effectivePageIndex, Is.EqualTo(2));
            Assert.That(maxPageIndex, Is.EqualTo(2));
            Assert.That(offset, Is.EqualTo(20));
        });
    }

    [Test]
    public void Resolve_WhenRequestedPageIndexEqualsMax_IsAccepted()
    {
        (int effectivePageIndex, int maxPageIndex, int offset) =
            PageCalculator.Resolve(totalCount: 25, requestedPageIndex: 2, pageSize: 10);

        Assert.Multiple(() =>
        {
            Assert.That(effectivePageIndex, Is.EqualTo(2));
            Assert.That(maxPageIndex, Is.EqualTo(2));
            Assert.That(offset, Is.EqualTo(20));
        });
    }
}
