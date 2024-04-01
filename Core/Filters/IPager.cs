namespace Core.Filters
{
    public interface IPager
    {
        int First { get; set; }
        int Rows { get; set; }
    }
}
