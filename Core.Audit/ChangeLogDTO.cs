using Core.DTO;
using System;

namespace Core.Audit
{
    /// <summary>
    /// Audit definition
    /// </summary>
    public class ChangeLogDTO : IDTO
    {
        public int Id { get; set; }

        public string State { get; set; }

        public DateTime DateTime { get; set; }

        public string EntityName { get; set; }

        public string TableName { get; set; }

        public int EntityId { get; set; }

        public string UserName { get; set; }

        public string ChangeLogItemsText { get; set; }
    }

    public class ProjectStatusChangeLogDTO : IDTO
    {
        public int Id { get; set; }

        public string State { get; set; }

        public int EntityId { get; set; }

        public DateTime DateTime { get; set; }

        public string UserName { get; set; }

        public string ChangeLogItemsText { get; set; }
    }
}
