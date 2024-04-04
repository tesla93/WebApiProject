using System;
using System.Collections.Generic;
using System.Linq;

namespace Module.DbDoc.Model
{
    /// <summary>
    /// represents root node
    /// </summary>
    public class DbStructure
    {
        public const string DefaultFolderName = "Default Folder";
        public DateTime? Timestamp { get; set; }
        public List<DbTable> Tables { get; set; }
        public List<DbColumnType> ColumnTypes { get; set; }
        public List<DbDocToolEntity> Folders { get; set; } = new List<DbDocToolEntity>();
        public List<VirtualTreeNode> Tree { get; set; } = new List<VirtualTreeNode>();
        public bool IsValid => Tree.Any() == true &&
            Folders.Any(x => x.Name == DefaultFolderName) == true &&
            Tables.Any() == true &&
            Tables.All(t => !string.IsNullOrWhiteSpace(t.ClrType));

        public DbStructure()
        {
            ColumnTypes = new List<DbColumnType>();
            Tables = new List<DbTable>();
        }

        public static DbStructure GetCleanDBStructure()
        {
            var folder = new DbDocToolEntity
            {
                Id = Guid.NewGuid(),
                Name = DefaultFolderName,
                Description = "Default folder containing all tables"
            };

            var result = new DbStructure();
            result.Folders.Add(folder);
            result.Tree.Add(folder.AsTreeNode());
            return result;
        }
    }
}