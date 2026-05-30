using Backend.Common;

namespace Test.Common;

[TestFixture]
public class PaginationTests
{
    [TestCase(0, 10)]
    [TestCase(-5, 10)]
    public void ComputeMaxIndex_WhenTotalCountNonPositive_ReturnsZero(int totalCount, int pageSize)
    {
        Assert.That(Pagination.ComputeMaxIndex(totalCount, pageSize), Is.EqualTo(0));
    }

    [TestCase(1, 10, 0)]
    [TestCase(10, 10, 0)]
    [TestCase(11, 10, 1)]
    [TestCase(9, 4, 2)]
    public void ComputeMaxIndex_WhenTotalCountPositive_ReturnsLastZeroBasedPage(int totalCount, int pageSize, int expected)
    {
        Assert.That(Pagination.ComputeMaxIndex(totalCount, pageSize), Is.EqualTo(expected));
    }
}
