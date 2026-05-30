namespace Backend.Common;

public static class Pagination
{
    public static int ComputeMaxIndex(int totalCount, int pageSize)
    {
        if (totalCount <= 0)
        {
            return 0;
        }

        return (totalCount - 1) / pageSize;
    }
}
