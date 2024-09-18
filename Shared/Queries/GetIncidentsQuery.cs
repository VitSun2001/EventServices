namespace Shared.Queries;

public record GetIncidentsQuery
{
    public string? SortColumn { get; }
    public string? OrderBy { get; }
    public int Page { get; }
    public int PageSize { get; }
    
    public GetIncidentsQuery(string? sortColumn, string? orderBy, int page, int pageSize)
    {
        SortColumn = sortColumn;
        OrderBy = orderBy;
        Page = page < 1 ? 1 : page;
        PageSize = pageSize is < 0 or > 100 ? 100 : pageSize;
    }
}