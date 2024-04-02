using Core.Data;
using System;

namespace Core.Membership.Model
{
    /// <summary>
    /// Password History
    /// </summary>
    public class PasswordHistory : IEntity
    {
        public int Id { get; set; }

        /// <summary>
        /// Link to a user
        /// </summary>
        //public virtual User User { get; set; }
        public string UserId { get; set; }

        /// <summary>
        /// Password
        /// </summary>
        public string Password { get; set; }

        /// <summary>
        /// Date of creation of record
        /// </summary>
        public DateTimeOffset CreateDate { get; set; }
    }
}
