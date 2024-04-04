using System;

namespace Module.DbDoc.Model
{
    /// <summary>
    /// represents reference on table
    /// </summary>
    public class DbTableRef
    {
        public string Name { get; set; }
        public Guid RefId { get; set; } = Guid.Empty;
        internal string ClrType { get; set; }
    }
}