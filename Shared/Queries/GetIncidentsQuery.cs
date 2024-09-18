namespace Shared.Queries;

public class GetIncidentsQuery
{
    public string? SortColumn { get; init; }
    public string? OrderBy { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
    
    public GetIncidentsQuery(string? sortColumn, string? orderBy, int page, int pageSize)
    {
        SortColumn = sortColumn;
        OrderBy = orderBy;
        Page = page < 1 ? 1 : page;
        PageSize = pageSize is < 0 or > 100 ? 100 : pageSize;
    }
}