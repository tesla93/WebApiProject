using System.Collections.Generic;
using Module.DbDoc.Model.ValidationMetadata;

namespace Module.DbDoc.Model
{
    public class DbColumnMetadata
    {
        public string Name { get; set; }
        public List<DbColumnViewMetadata> ViewMetadata { get; set; } = new List<DbColumnViewMetadata>();
        public List<DbColumnValidationMetadata> ValidationMetadata { get; set; } = new List<DbColumnValidationMetadata>();
    }
}