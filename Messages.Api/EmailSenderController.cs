using System;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Core.Membership;
using Core.Membership.Model;
using Core.Membership.Services;
using Messages.Templates;
using ControllerBase = Core.Web.ControllerBase;
using Core.Web;

namespace Messages.Api
{
    /// <summary>
    ///  Controller for Email Template Parameters
    /// </summary>
    [Produces("application/json")]
    [Route("api/send-email")]
    [Authorize(Roles = Roles.SystemAdminRole)]
    public class EmailSenderController : ControllerBase
    {
        private readonly IEmailSender _emailSender;
        private readonly IEmailTemplateService _emailTemplateService;
        private readonly IUserService _userService;
        private readonly UserManager<User> _userManager;
        private readonly IBrandingService _brandingService;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public EmailSenderController(
            IEmailSender emailSender,
            IEmailTemplateService emailTemplateService,
            IUserService userService,
            UserManager<User> userManager,
            ILogger<EmailSenderController> logger,
            IBrandingService brandingService,
            IHttpContextAccessor httpContextAccessor) : base(logger)
        {
            _emailSender = emailSender;
            _emailTemplateService = emailTemplateService;
            _userService = userService;
            _userManager = userManager;
            _brandingService = brandingService;
            _httpContextAccessor = httpContextAccessor;
        }

        [HttpPost]
        public async Task<IActionResult> SendEmail([FromForm] EmailDTO email, int emailTemplateId)
        {
            var user = await _userManager.FindByEmailAsync(email.To);
            if (user == null)
            {
                return BadRequest("user not found");
            }

            var emailTemplate = await _emailTemplateService.Get(email.EmailTemplateId);
            if (emailTemplate == null)
            {
                return NotFound(emailTemplateId);
            }

            var url = Request.Scheme + "://" + Request.Host + "/account/resetpassword";
            var ip = _httpContextAccessor.HttpContext.Connection.RemoteIpAddress.ToString();
            var tagValues = await _userService.GetTagValues(email.To, url, ip);

            var testTagValues = new NameValueCollection
                    {
                        {"$OldEmail", email.To},
                        {"$NewEmail", email.To},
                    };

            tagValues.Add(testTagValues);

            _emailTemplateService.BuildEmail(emailTemplate, tagValues);
            var attachments = Request.Form.Files.ToArray();

            var brand = await _brandingService.GetAnyBranding();
            EmailBrandInfo brandInfo = null;
            if (brand != null && !string.IsNullOrEmpty(brand.EmailBody))
            {
                brandInfo = new EmailBrandInfo
                {
                    Body = brand.EmailBody
                };
            }

            await _emailSender.SendEmail(emailTemplate.Subject, emailTemplate.Body, emailTemplate.From, attachments, brandInfo, email.To);
            return NoContent();
        }

        [HttpGet]
        [Route("test-email")]
        public async Task<IActionResult> SendTestEmail()
        {
            var additionalInfo = new StringBuilder("Test Email<br/><br/>");
            additionalInfo.AppendFormat("OS Name: {0}<br/>", Environment.OSVersion.Platform);
            additionalInfo.AppendFormat("Server IP: {0}<br/>", HttpContext.Connection.LocalIpAddress);

            await _emailSender.SendEmail("Email Sender testing", additionalInfo.ToString());
            return NoContent();
        }
    }
}