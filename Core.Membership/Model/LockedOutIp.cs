using Core.Data;
using System;

namespace LockIP
{
    public class LockedOutIp : IEntity
    {
        public int Id { get; set; }
        public string IpAddress { get; set; }
        public DateTime LockoutEnd { get; set; }
    }
}
