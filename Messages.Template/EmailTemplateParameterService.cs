using AutoMapper;
using Core.Crud;
using Core.Crud.Interfaces;
using Core.Data;

namespace Messages.Templates
{
    public interface IEmailTemplateParameterService : IPagedCrudService<EmailTemplateParameterDTO>
    {
    }

    public class EmailTemplateParameterService: PagedCrudService<EmailTemplateParameter, EmailTemplateParameterDTO>, IEmailTemplateParameterService
    {
        public EmailTemplateParameterService(IDbContext context, IMapper mapper) : base(context, mapper)
        {
        }
    }
}
