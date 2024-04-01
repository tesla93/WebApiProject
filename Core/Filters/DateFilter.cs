using System;
using Core.Filters.Handlers;

namespace Core.Filters
{
    [RelatedHandler(typeof(DateFilterHandler))]
    public class DateFilter : CountableFilterBase<DateTime>
    {
    }

    [RelatedHandler(typeof(CountableBetweenFilterHandler<DateTime>))]
    public class DateBetweenFilter : CountableBetweenFilterBase<DateTime>
    {
    }
}