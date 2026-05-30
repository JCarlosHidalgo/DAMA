namespace Backend.Common;

public class PageDto<T>
{
    public int CurrentIndex { get; set; }

    public int MaxIndex { get; set; }

    public List<T> Items { get; set; } = new List<T>();
}
