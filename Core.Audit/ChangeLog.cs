using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Core.Data;

namespace Core.Audit
{
    /// <summary>
    /// Change log definition
    /// </summary>
    public class ChangeLog : IEntity
    {
        public int Id { get; set; }

        public EntityState State { get; set; }

        public DateTime DateTime { get; set; }

        public string EntityName { get; set; }

        public string TableName { get; set; }

        public int EntityId { get; set; }

        public string UserName { get; set; }

        public virtual IList<ChangeLogItem> ChangeLogItems { get; set; }
    }
}
