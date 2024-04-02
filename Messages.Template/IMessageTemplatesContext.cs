using Microsoft.EntityFrameworkCore;
using Core.Data;

namespace Messages.Templates
{
    public interface IMessageTemplatesContext : IDbContext
    {
        DbSet<EmailTemplate> EmailTemplates { get; set; }
        DbSet<EmailTemplateParameter> EmailTemplateParameters { get; set; }
    }
}
