using System.Threading;
using Core.Membership.DTO;
using System.Threading.Tasks;
using Core.Crud.Interfaces;

namespace Core.Membership.Services
{
    public interface IBrandingService : ICrudService<BrandingDTO>
    {
        Task DeleteLogoIcon(int brandingId, CancellationToken cancellationToken = default);
        Task DeleteLogoImage(int brandingId, CancellationToken cancellationToken = default);
        Task<BrandingDTO> GetAnyBranding(CancellationToken cancellationToken = default);
        Task<BrandingDTO> GetCompanyBranding(int companyId, CancellationToken cancellationToken = default);
    }
}
