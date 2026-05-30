namespace Backend.Pagination;

public static class PageCalculator
{
    public static (int effectivePageIndex, int maxPageIndex, int offset)
        Resolve(long totalCount, int requestedPageIndex, int pageSize)
    {
        int maxPageIndex = totalCount <= 0
            ? 0
            : (int)((totalCount - 1) / pageSize);
        int effectivePageIndex = requestedPageIndex > maxPageIndex
            ? maxPageIndex
            : requestedPageIndex;
        int offset = effectivePageIndex * pageSize;
        return (effectivePageIndex, maxPageIndex, offset);
    }
}
