namespace StudentManagementSystem.API.DTOs.Common;

/// <summary>
/// Generic wrapper for paginated API responses.
/// </summary>
public class PagedResultDto<T>
{
    public IReadOnlyList<T> Items { get; set; } = [];

    public int PageNumber { get; set; }

    public int PageSize { get; set; }

    public int TotalRecords { get; set; }

    public int TotalPages => PageSize > 0
        ? (int)Math.Ceiling(TotalRecords / (double)PageSize)
        : 0;

    public bool HasPreviousPage => PageNumber > 1;

    public bool HasNextPage => PageNumber < TotalPages;
}
