using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Core.Crud;
using Core.Data;
using Core.Membership.DTO;
using Core.Membership.Model;

namespace Core.Membership.Services
{
    public class ActivationTokenService : CrudService<ActivationToken, ActivationTokenDTO>, IActivationTokenService
    {
        public ActivationTokenService(IDbContext context, IMapper mapper) : base(context, mapper) { }

        public async Task<ActivationTokenDTO> GetByTokenValue(string token, CancellationToken cancellationToken = default)
            => await Get(x => x.Token == token, cancellationToken);
    }
}
