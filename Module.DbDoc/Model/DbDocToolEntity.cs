using Module.DbDoc.Enums;
using System;

namespace Module.DbDoc.Model
{
    public class DbDocToolEntity
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }

        protected virtual DbItemType ItemType => DbItemType.Folder;
        public virtual VirtualTreeNode AsTreeNode() => new VirtualTreeNode { ItemType = ItemType, ObjectRef = Id };
    }
}