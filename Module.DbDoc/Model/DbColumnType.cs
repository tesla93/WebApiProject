using Module.DbDoc.Enums;
using Module.DbDoc.Model.ValidationMetadata;
using System;

namespace Module.DbDoc.Model
{
    public class DbColumnType
    {
        public Guid? Id { get; set; }

        public string Name { get; set; }

        public ClrTypeGroup Group { get; set; }

        public DbAnonRule AnonRule { get; set; }

        public DbColumnViewMetadata ViewMetadata { get; set; }

        public DbColumnValidationMetadata ValidationMetadata { get; set; }
    }
}