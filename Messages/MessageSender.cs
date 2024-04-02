using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MailKit.Net.Smtp;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using MimeKit;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;
using MimeKit.Utils;
using System.Text.RegularExpressions;

namespace Messages
{
    public class MessageSender : IEmailSender, ISmsSender
    {
        private readonly IOptionsSnapshot<EmailSettings> _emailSettings;
        private readonly IOptionsSnapshot<SMSSettings> _smsSettings;

        public MessageSender(IOptionsSnapshot<EmailSettings> emailSettings, IOptionsSnapshot<SMSSettings> smsSettings)
        {
            _emailSettings = emailSettings;
            _smsSettings = smsSettings;
        }

        public async Task SendEmail(string subject, string body, string from = null, IFormFile[] attachments = null,
            EmailBrandInfo brandInfo = null, params string[] to)
        {
            var emailMessage = new MimeMessage();
            var settings = _emailSettings.Value;
            var toAddresses = new List<MailboxAddress>();

            if (settings != null)
            {
                var testMessage = new StringBuilder();

                if (settings.TestMode)
                {
                    // Recipient = @TestEmailRecipient
                    testMessage.Append("<h5>**TEST MODE**</h5>");
                    testMessage.AppendFormat("<div>This email should have gone to {0} but in test mode, it is not sent to the intended recipient.</div><br/>", string.Join(',', to));
                    toAddresses.Add(new MailboxAddress(settings.TestEmailAddress));
                }

                // to addresses
                if (!to.Any() && !toAddresses.Any())
                {
                    toAddresses.Add(new MailboxAddress(settings.AdminAddress));
                }
                else
                {
                    toAddresses.AddRange(to.Where(o => !string.IsNullOrWhiteSpace(o))
                        .Select(o => new MailboxAddress(o.Trim())));
                }

                // from address
                from = string.IsNullOrWhiteSpace(from) ? settings.FromAddress : from;
                var port = settings.Port == 0 ? settings.DefaultPort : settings.Port;

                emailMessage.From.Add(new MailboxAddress(from));
                emailMessage.To.AddRange(toAddresses);
                emailMessage.Subject = subject;


                var htmlBody = $"{ testMessage } { body }";

                var builder = new BodyBuilder();

                htmlBody = await Converbase64Image(htmlBody, builder);

                if (brandInfo != null && !string.IsNullOrEmpty(brandInfo.Body))
                {

                    var brandingHtml = await Converbase64Image(brandInfo.Body, builder);
                    htmlBody += brandingHtml;
                }

                builder.HtmlBody = htmlBody;


                if (attachments != null)
                {
                    foreach (var file in attachments)
                    {
                        using (var stream = file.OpenReadStream())
                        {
                            builder.Attachments.Add(file.FileName, StreamToByteArray(stream), ContentType.Parse(file.ContentType));
                        }
                    }
                }

                emailMessage.Body = builder.ToMessageBody();

                using (var client = new SmtpClient())
                {
                    await client.ConnectAsync(settings.SMTP, port, settings.EnableSsl);
                    if (!string.IsNullOrEmpty(settings.UserName) && !string.IsNullOrEmpty(settings.Password))
                    {
                        await client.AuthenticateAsync(settings.UserName, settings.Password);
                    }
                    await client.SendAsync(emailMessage);
                    await client.DisconnectAsync(true);
                }
            }

        }

        public async Task SendEmailTo(string subject, string body, string from = null, string emailPriority = "1", IFormFile[] attachments = null,
            EmailBrandInfo brandInfo = null, string ccTo = null, string bccTo = null, params string[] to)
        {
            var emailMessage = new MimeMessage();
            var settings = _emailSettings.Value;
            var toAddresses = new List<MailboxAddress>();
            var toCc = new List<MailboxAddress>();
            var toBcc = new List<MailboxAddress>();

            if (settings != null)
            {
                var testMessage = new StringBuilder();

                if (settings.TestMode)
                {
                    // Recipient = @TestEmailRecipient
                    testMessage.Append("<h5>**TEST MODE**</h5>");
                    testMessage.AppendFormat("<div>This email should have gone to {0} but in test mode, it is not sent to the intended recipient.</div><br/>", string.Join(',', to));
                    toAddresses.Add(new MailboxAddress(settings.TestEmailAddress));
                }

                // to addresses
                if (!to.Any() && !toAddresses.Any())
                {
                    toAddresses.Add(new MailboxAddress(settings.AdminAddress));
                }
                else
                {
                    toAddresses.AddRange(to.Where(o => !string.IsNullOrWhiteSpace(o))
                        .Select(o => new MailboxAddress(o.Trim())));
                }

                // to cc
                if (!string.IsNullOrEmpty(ccTo) && to.Any() && toAddresses.Any())
                {
                    string[] emails = ccTo.Split(';');
                    toCc.AddRange(emails.Where(o => !string.IsNullOrWhiteSpace(o))
                         .Select(o => new MailboxAddress(o.Trim())));
                    emailMessage.Cc.AddRange(toCc);
                }

                // to bcc
                if (!string.IsNullOrEmpty(bccTo) && to.Any() && toAddresses.Any())
                {
                    string[] emails = bccTo.Split(';');
                    toBcc.AddRange(emails.Where(o => !string.IsNullOrWhiteSpace(o))
                        .Select(o => new MailboxAddress(o.Trim())));
                    emailMessage.Bcc.AddRange(toBcc);
                }

                // from address
                from = string.IsNullOrWhiteSpace(from) ? settings.FromAddress : from;
                var port = settings.Port == 0 ? settings.DefaultPort : settings.Port;

                emailMessage.From.Add(new MailboxAddress(from));
                emailMessage.To.AddRange(toAddresses);
                emailMessage.Subject = subject;

                if (!string.IsNullOrEmpty(emailPriority))
                {
                    bool isPriorityValid = Enum.TryParse(emailPriority, out MessagePriority messagePriority);
                    emailMessage.Priority = isPriorityValid ? messagePriority : MessagePriority.Normal;
                }

                var htmlBody = $"{ testMessage } { body }";

                var builder = new BodyBuilder();

                htmlBody = await Converbase64Image(htmlBody, builder);

                if (brandInfo != null && !string.IsNullOrEmpty(brandInfo.Body))
                {

                    var brandingHtml = await Converbase64Image(brandInfo.Body, builder);
                    htmlBody += brandingHtml;
                }

                builder.HtmlBody = htmlBody;


                if (attachments != null)
                {
                    foreach (var file in attachments)
                    {
                        using (var stream = file.OpenReadStream())
                        {
                            builder.Attachments.Add(file.FileName, StreamToByteArray(stream), ContentType.Parse(file.ContentType));
                        }
                    }
                }

                emailMessage.Body = builder.ToMessageBody();

                using (var client = new SmtpClient())
                {
                    await client.ConnectAsync(settings.SMTP, port, settings.EnableSsl);
                    if (!string.IsNullOrEmpty(settings.UserName) && !string.IsNullOrEmpty(settings.Password))
                    {
                        await client.AuthenticateAsync(settings.UserName, settings.Password);
                    }
                    await client.SendAsync(emailMessage);
                    await client.DisconnectAsync(true);
                }
            }

        }

        private async Task<string> Converbase64Image(string htmlBody, BodyBuilder builder)
        {
            var isContinue = true;

            while (isContinue)
            {

                var pattern = @"<img.*?\ssrc=""data:(image/\w*);base64,([\S]*?)"".*?>";

                var regex = new Regex(pattern);

                var matches = regex.Match(htmlBody);

                if (matches.Success)
                {
                    foreach (Match match in matches.Captures)
                    {
                        var type = match.Groups[1].Value;
                        var fromBase64 = match.Groups[2].Value;
                        var data = Convert.FromBase64String(fromBase64);
                        var stream = new MemoryStream(data);
                        var image = ContentType.TryParse(type, out var contentType)
                            ? await MimeEntity.LoadAsync(contentType, stream)
                            : await MimeEntity.LoadAsync(stream);
                        builder.LinkedResources.Add(image);
                        image.ContentId = MimeUtils.GenerateMessageId();

                        fromBase64 = fromBase64.Replace(@"\", @"\\");
                        fromBase64 = fromBase64.Replace("+", @"\+");
                        fromBase64 = fromBase64.Replace("*", @"\*");
                        fromBase64 = fromBase64.Replace("^", @"\^");
                        fromBase64 = fromBase64.Replace(".", @"\.");
                        fromBase64 = fromBase64.Replace("$", @"\$");
                        fromBase64 = fromBase64.Replace("?", @"\?");

                        var replacePattern = $@"src=""data:image/\w*;base64,{fromBase64}""";

                        var replaceRegex = new Regex(replacePattern);

                        htmlBody = replaceRegex.Replace(htmlBody, $@"src=""cid:{image.ContentId}""");
                    }
                }
                else
                {
                    isContinue = false;
                }
            }

            return htmlBody;
        }

        public async Task SendSms(string number, string message)
        {
            var settings = _smsSettings.Value;
            TwilioClient.Init(settings.ApiKey, settings.AuthToken, settings.AccountSid);

            var phoneNumber = number.StartsWith("0") ? "+44" + number.Substring(1) : number;

            var from = new PhoneNumber(settings.ShortCode);
            var to = new PhoneNumber(phoneNumber);

            await MessageResource.CreateAsync(to, from: from, body: message);
        }

        private byte[] StreamToByteArray(Stream stream)
        {
            var buffer = new byte[16 * 1024];
            using (var ms = new MemoryStream())
            {
                int read;
                while ((read = stream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    ms.Write(buffer, 0, read);
                }
                return ms.ToArray();
            }
        }
    }
}
