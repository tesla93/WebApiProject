using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Core.Audit
{
    /// <summary>
    /// Audit entry definition
    /// </summary>
    public class AuditChangeEntry
    {
        public AuditChangeEntry(EntityEntry entry)
        {
            State = entry.State;
        }

        public EntityState State { get; set; }
        public string TableName { get; set; }
        public string EntityName { get; set; }
        public string UserName { get; set; }
        public int EntityId { get; set; }
        public Dictionary<string, object> OldValues { get; } = new Dictionary<string, object>();
        public Dictionary<string, object> NewValues { get; } = new Dictionary<string, object>();
        public List<PropertyEntry> KeyProperties { get; } = new List<PropertyEntry>();

        public ChangeLog ToAudit()
        {
            var audit = new ChangeLog();
            audit.State = State;
            audit.UserName = UserName;
            audit.TableName = TableName;
            audit.EntityName = EntityName;
            audit.DateTime = DateTime.UtcNow;
            audit.EntityId = EntityId;
            audit.ChangeLogItems = NewValues.Keys.Select(k => new ChangeLogItem
            {
                NewValue = NewValues[k] == null ? null : NewValues[k].ToString(),
                OldValue = OldValues.ContainsKey(k) && OldValues[k] != null ? OldValues[k].ToString() : null,
                PropertyName = k
            }).ToArray();
            return audit;
        }
    }
}
