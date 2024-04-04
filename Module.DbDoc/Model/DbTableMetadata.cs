using System.Collections.Generic;

namespace Module.DbDoc.Model
{
    public class DbTableMetadata
    {
        public string Name { get; set; }
        public string ClrType { get; set; }
        public List<DbDtoDetails> DtoDetails { get; set; } = new List<DbDtoDetails>();
        public List<DbColumnMetadata> Columns { get; set; } = new List<DbColumnMetadata>();
    }

    public class DbDtoDetails
    {
        public string Name { get; set; }
        public string ClrType { get; set; }
        public List<DbDtoProperty> Properties { get; set; } = new List<DbDtoProperty>();
    }

    public class DbDtoProperty
    {
        public string PropertyName { get; set; }
        public string SourceEntityName { get; set; }
        public string SourceEntityFieldName { get; set; }
    }
}