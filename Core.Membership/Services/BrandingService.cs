using AutoMapper;
using Core.Data;
using Core.Membership.DTO;
using Core.Membership.Model;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FileStorage;
using Core.Exceptions;
using Core.Crud;

namespace Core.Membership.Services
{
    public class BrandingService : CrudService<Branding, BrandingDTO>, IBrandingService
    {
        private readonly IFileStorageService _fileStorageService;


        public BrandingService(IDbContext context, IMapper mapper, IFileStorageService fileStorageService)
            : base(context, mapper) =>
            _fileStorageService = fileStorageService;

        public async Task DeleteLogoIcon(int brandingId, CancellationToken cancellationToken = default)
        {
            var branding = await GetQueryable<Branding>().FirstOrDefaultAsync(x => x.Id == brandingId, cancellationToken);

            if (branding == null)
                throw new ObjectNotExistsException("Branding not found.");

            var logoIconId = branding.LogoIconId;
            if (logoIconId == null) return;

            branding.LogoIconId = null;
            branding.LogoIcon = null;
            await SaveChangesAsync(cancellationToken);

            await _fileStorageService.DeleteFile((int)logoIconId, cancellationToken);
        }

        public async Task DeleteLogoImage(int brandingId, CancellationToken cancellationToken = default)
        {
            var branding = await GetQueryable<Branding>().FirstOrDefaultAsync(x => x.Id == brandingId, cancellationToken);

            if (branding == null)
                throw new ObjectNotExistsException("Branding not found.");

            var logoImageId = branding.LogoImageId;
            if (logoImageId == null) return;

            branding.LogoImageId = null;
            branding.LogoImage = null;
            await SaveChangesAsync(cancellationToken);

            await _fileStorageService.DeleteFile((int) logoImageId, cancellationToken);
        }

        public async Task<BrandingDTO> GetAnyBranding(CancellationToken cancellationToken = default) =>
            //Now this code will look for the first company that has data on the brand
            await Get(o => true, cancellationToken);

        public async Task<BrandingDTO> GetCompanyBranding(int companyId, CancellationToken cancellationToken = default) =>
            await Get(o => o.Company.Id == companyId, cancellationToken); 

        protected override IQueryable<Branding> ConfigureDataReader(IQueryable<Branding> entities) =>
            entities.Include(x => x.Company)
                .Include(x => x.LogoIcon)
                .Include(x => x.LogoImage);
    }
}