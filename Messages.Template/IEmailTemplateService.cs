using System.Collections.Specialized;
using System.Threading;
using System.Threading.Tasks;
using Core.Crud.Interfaces;

namespace Messages.Templates
{
    public interface IEmailTemplateService : IPagedCrudService<EmailTemplateDTO>
    {
        Task<EmailTemplateDTO> GetByCode(string code, CancellationToken cancellationToken = default);
        void BuildEmail(EmailTemplateDTO template, NameValueCollection tagValues);
        bool CheckEmailTemplateCode(string code, int? id);
        string CreateBrand(string logoUrl);
    }
}