using System.Collections.Generic;
using Module.DbDoc.Model.ValidationMetadata;

namespace Module.DbDoc.Model
{
    public class DbMetadataFieldResult
    {
        public string FieldName { get; set; }

        public List<ValidationRule> ValidationRules { get; set; }

        public GridColumnViewDetails GridColumnViewDetails { get; set; }
    }
}