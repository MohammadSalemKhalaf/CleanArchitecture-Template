namespace TemplateApp.Contracts.Common;

/// <summary>Query-string paging parameters (<c>?page=1&amp;pageSize=10</c>). Ranges are validated by the application query validator.</summary>
public sealed record PageRequest
{
    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 10;
}
