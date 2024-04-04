using Module.DbDoc.Enums;
using System;
using System.Collections.Generic;

namespace Module.DbDoc.Model
{
    public class VirtualTreeNode
    {
        public Guid ObjectRef { get; set; }
        public bool Expanded { get; set; }
        public bool Hidden { get; set; }
        public List<VirtualTreeNode> Children { get; set; } = new List<VirtualTreeNode>();
        public DbItemType ItemType { get; set; }
    }
}