using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Core.Crud;
using Core.Data;
using Microsoft.EntityFrameworkCore;

namespace Messages.Templates
{
    public class EmailTemplateService : PagedCrudService<EmailTemplate, EmailTemplateDTO>, IEmailTemplateService
    {
        private static readonly Regex TagExtractor = new Regex(@"(\$\w+)", RegexOptions.Compiled);
        private static readonly Regex ChildTemplateExtractor = new Regex(@"(\$&lt;(\w+)&gt;)", RegexOptions.Compiled);


        public EmailTemplateService(IDbContext context, IMapper mapper) : base(context, mapper)
        {
        }

        protected override async Task<ValidationResult> IsValid(EmailTemplateDTO dto, CancellationToken cancellationToken)
        {
            if (!CheckEmailTemplateCode(dto.Code, dto.Id))
            {
                return new ValidationResult("Duplicated EmailTemplate Code", new[] { nameof(EmailTemplateDTO.Code) });
            }
            return await base.IsValid(dto, cancellationToken);
        }

        protected override async Task<EmailTemplateDTO> BeforeSave(EmailTemplateDTO dto, CancellationToken cancellationToken = default)
        {
            // Fix Quill setting null body when it's empty
            dto.Body ??= "";
            // Fix issue with Quill Editor wrapping everything in <p> tags, causing duplicate spacing.
            dto.Body = dto.Body.Replace("<p><br></p>", "<br/>")
                .Replace(" target=\"_blank\"", string.Empty);
            
            // #231111 Replace everything not in specific UTF-8 character set ranges
            dto.Body = Regex.Replace(dto.Body, @"[^\u0020-\u007E]", string.Empty);

            return await base.BeforeSave(dto, cancellationToken);
        }

        public async Task<EmailTemplateDTO> GetByCode(string code, CancellationToken cancellationToken = default) =>
            await Get(item => item.Code == code, cancellationToken);

        public void BuildEmail(EmailTemplateDTO template, NameValueCollection tagValues)
        {
            template.From = BuildStringFromTemplate(template.From, tagValues);
            template.Subject = BuildStringFromTemplate(template.Subject, tagValues);
            template.Body = BuildStringFromTemplate(template.Body, tagValues, true);
        }

        public bool CheckEmailTemplateCode(string code, int? id) =>
            GetQueryable<EmailTemplate>().All(t => t.Id == id || t.Code != code);

        public string CreateBrand(string logoUrl) =>
            @"<div style=""width: 100%; height: 50px; background-size: cover; background-image: url('" + logoUrl + @"')'""></div>";


        private string BuildStringFromTemplate(string templateString, NameValueCollection tagValues, bool includeChildTemplates = false)
        {
            var result = templateString;
            var tagValuesList = tagValues ?? new NameValueCollection();

            var originTagValues = new NameValueCollection(tagValuesList);

            PrepareSystemTags(tagValuesList);

            var tags = GetTagList(templateString);
            foreach (var tag in tags)
            {
                var tagValue = tagValuesList[tag];
                result = result.Replace(tag, tagValue);
            }

            if (!includeChildTemplates) return result;

            var childTemplateNames = GetChildTemplates(templateString).Distinct().ToArray();
            if (!childTemplateNames.Any()) return result;

            var childTemplates = GetQueryable<EmailTemplate>().Where(t => childTemplateNames.Contains(t.Code, StringComparer.OrdinalIgnoreCase)).ToDictionary(t => t.Code, t => t.Body);
            if (childTemplates.Count != childTemplateNames.Length) return result;

            foreach (var childTemplate in childTemplates)
            {
                // only one level allowed for now
                result = result.Replace($"$&lt;{childTemplate.Key}&gt;", BuildStringFromTemplate(childTemplate.Value, originTagValues));
            }

            return result;
        }

        private static void PrepareSystemTags(NameValueCollection tagValues)
        {
            //var user = this.membershipService.GetCurrentUser()
            //           ?? new User { UserName = "<unknown>", FirstName = "<unknown>", LastName = "<unknown>" };

            if (tagValues["$AppName"] == null)
            {
                tagValues["$AppName"] = "BBWT3";
            }

            if (tagValues["$DateTime"] == null)
            {
                tagValues["$DateTime"] = DateTimeOffset.UtcNow.ToString("dd/MM/yyyy HH:MM", CultureInfo.GetCultureInfo("en-GB"));
            }

            //if (user != null)
            //{
            //    if (tagValues["$UserName"] == null)
            //    {
            //        tagValues["$UserName"] = string.Format("{0} {1}", user.FirstName, user.Surname);
            //    }

            //    if (tagValues["$UserFirstName"] == null)
            //    {
            //        tagValues["$UserFirstName"] = user.FirstName;
            //    }

            //    if (tagValues["$UserSurname"] == null)
            //    {
            //        tagValues["$UserSurname"] = user.Surname;
            //    }

            //    if (tagValues["$UserEmail"] == null)
            //    {
            //        tagValues["$UserEmail"] = user.Name;
            //    }
            //}
        }

        private static IEnumerable<string> GetChildTemplates(string source)
        {
            if (string.IsNullOrEmpty(source)) yield break;

            var matches = ChildTemplateExtractor.Matches(source);
            foreach (Match match in matches)
            {
                if (match.Groups.Count == 3)
                {
                    yield return match.Groups[2].Value;
                }
            }
        }

        private static IEnumerable<string> GetTagList(string source)
        {
            string tag;
            var tags = new List<string>();

            if (string.IsNullOrEmpty(source)) return tags.ToArray();

            var matches = TagExtractor.Matches(source);
            foreach (Match match in matches)
            {
                if (match.Groups.Count != 2) continue;

                tag = match.Groups[1].Value;
                if (!tags.Contains(tag))
                {
                    tags.Add(tag);
                }
            }

            return tags.ToArray();
        }
    }
}