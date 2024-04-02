using System;
using Core.Data;

namespace Core.Membership.Model
{
    public class ActivationToken : IEntity
    {
        public int Id { get; set; }

        public string Token { get; set; }

        public DateTime? ExpirationDate { get; set; }
    }
}
