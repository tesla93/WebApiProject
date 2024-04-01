namespace Core.Filters
{
    public interface ISorter
    {
        string SortField { get; set; }
        OrderDirection SortOrder { get; set; }
        bool IsAsc => SortOrder == OrderDirection.Asc;
    }
}
