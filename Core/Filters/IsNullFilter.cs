using Core.Filters.Handlers;

namespace Core.Filters
{
    [RelatedHandler(typeof(IsNullFilterHandler))]
    public class IsNullFilter: FilterInfoBase
    {
    }
}
