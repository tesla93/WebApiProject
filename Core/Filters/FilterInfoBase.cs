namespace Core.Filters
{
    public abstract class FilterInfoBase
    {
        public string PropertyName { get; set; }
    }

    public abstract class FilterInfoBase<T> : FilterInfoBase
    {
        public T Value { get; set; }
    }
}