namespace Module.DbDoc.Model.ValidationMetadata
{
    public abstract class RangeValidationRule<T> : ValidationRule where T : struct
    {
        public T? Min { get; set; }
        public T? Max { get; set; }
    }
}