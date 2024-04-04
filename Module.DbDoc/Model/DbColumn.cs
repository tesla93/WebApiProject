using System;
using System.Collections.Generic;
using Module.DbDoc.Enums;
using Module.DbDoc.Model.ValidationMetadata;

namespace Module.DbDoc.Model
{
    /// <summary>
    /// represents single column
    /// </summary>
    public class DbColumn : DbDocToolEntity
    {
        // sql type
        public string Type { get; set; }
        // c# type
        public string ClrType { get; set; }

        public ClrTypeGroup ClrTypeGroup { get; set; }

        public string DefaultValue { get; set; }

        public string DefaultValueSQL { get; set; }

        public bool? AllowNulls { get; set; }

        public bool? IsPrimaryKey { get; set; }

        public bool? IsForeignKey { get; set; }

        public bool? IsIndex { get; set; }

        public List<DbTableRef> RefTables { get; set; } = new List<DbTableRef>();

        public List<DbColumnViewMetadata> ViewMetadata { get; set; } = new List<DbColumnViewMetadata>();

        public List<DbColumnValidationMetadata> ValidationMetadata { get; set; } = new List<DbColumnValidationMetadata>();

        public DbAnonRule? AnonRule { get; set; }

        public Guid? DbColumnTypeRef { get; set; }

        public string MinValue { get; set; }

        public string MaxValue { get; set; }

        public int UniqueCount { get; set; }

        protected override DbItemType ItemType => DbItemType.Column;
    }
}