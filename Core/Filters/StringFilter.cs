using Core.Filters.Handlers;

namespace Core.Filters
{
    [RelatedHandler(typeof(StringFilterHandler))]
    public class StringFilter: FilterInfoBase<string>
    {
        public StringFilterMatchMode MatchMode { get; set; }
    }

    public enum StringFilterMatchMode
    {
        Contains,
        StartsWith,
        EndsWith,
        Equals
    }
}