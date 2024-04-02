using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace Messages
{
    public interface IEmailSender
    {
        Task SendEmail(string subject, string body, string from = null, IFormFile[] attachments = null,
            EmailBrandInfo brandInfo = null, params string[] to);
        Task SendEmailTo(string subject, string body, string from = null, string emailPriority = "1", IFormFile[] attachments = null,
             EmailBrandInfo brandInfo = null, string ccTo = null, string bccTo = null, params string[] to);
    }
}
