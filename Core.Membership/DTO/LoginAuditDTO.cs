using Core.DTO;
using System;

namespace Core.Membership.DTO
{
    public class LoginAuditDTO : IDTO
    {
        public int Id { get; set; }
        public DateTimeOffset Datetime { get; set; }
        public string Ip { get; set; }
        public string Location { get; set; }
        public string Fingerprint { get; set; }
        public string Email { get; set; }
        public string Browser { get; set; }
        public string Result { get; set; }

        public static LoginAuditDTO Create(string email, string ip, string browser, string fingerprint, string location, string result)
        {
            return new LoginAuditDTO
            {
                Datetime = DateTimeOffset.Now,
                Email = email,
                Ip = ip,
                Browser = browser,
                Fingerprint = fingerprint,
                Location = location,
                Result = result
            };
        }
    }
}