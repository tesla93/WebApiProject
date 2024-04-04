using System;
using System.Collections.Generic;
using System.Linq;
using Module.DbDoc.Enums;

namespace Module.DbDoc.Model
{
    /// <summary>
    /// represents single table
    /// </summary>
    public class DbTable : DbDocToolEntity
    {
        public int Anonymisation { get; set; }

        public int NumRecords { get; set; }

        public string Representation { get; set; }

        public string ClrType { get; set; }

        public List<DbColumn> Columns { get; set; } = new List<DbColumn>();

        protected override DbItemType ItemType => DbItemType.Table;

        public override VirtualTreeNode AsTreeNode()
        {
            return new VirtualTreeNode
            {
                ItemType = ItemType,
                ObjectRef = Id,
                Children = Columns.Select(c => c.AsTreeNode()).ToList()
            };
        }
    }
}