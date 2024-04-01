namespace Core.Filters
{
    public abstract class CountableFilterBase<T> : FilterInfoBase<T>
    {
        public CountableFilterMatchMode MatchMode { get; set; }
    }

    public abstract class CountableBetweenFilterBase<T> : FilterInfoBase
    {
        public T From { get; set; }

        public T To { get; set; }
    }

    public enum CountableFilterMatchMode
    {
        Equals,
        LessThan,
        LessThanOrEqual,
        GreaterThanOrEqual,
        GreaterThan,
        Between
    }
}