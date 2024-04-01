namespace Core.Filters
{
    public class FilterValue<T>
    {
        public FilterMatchMode MatchMode { get; set; }
        public T Value { get; set; }
    }
}
