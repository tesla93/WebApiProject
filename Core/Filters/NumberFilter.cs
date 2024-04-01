using Core.Filters.Handlers;

namespace Core.Filters
{
    [RelatedHandler(typeof(NumberFilterHandler))]
    public class NumberFilter : CountableFilterBase<double>
    {
    }

    [RelatedHandler(typeof(CountableBetweenFilterHandler<double>))]
    public class NumberBetweenFilter : CountableBetweenFilterBase<double>
    {
    }
}