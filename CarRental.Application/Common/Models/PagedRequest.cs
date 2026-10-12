namespace _CarRental.Application.Common.Models;

public class PagedRequest
{
    public const int MaximumPageSize = 100;
    private int _page = 1;
    private int _pageSize = 20;

    public int Page
    {
        get => _page;
        init => _page = Math.Max(1, value);
    }

    public int PageSize
    {
        get => _pageSize;
        init => _pageSize = Math.Clamp(value, 1, MaximumPageSize);
    }
}
