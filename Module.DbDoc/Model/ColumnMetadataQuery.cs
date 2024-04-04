namespace Module.DbDoc.Model
{
    public class ColumnMetadataQuery
    {
        public string Min { get; set; }
        public string Max { get; set; }
        public int Unique { get; set; }
        public string Name { get; set; }
        public string TableName { get; set; }
    }
}