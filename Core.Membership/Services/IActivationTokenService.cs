using System.Threading;
using System.Threading.Tasks;
using Core.Crud.Interfaces;
using Core.Membership.DTO;

namespace Core.Membership.Services
{
    public interface IActivationTokenService : ICrudService<ActivationTokenDTO>
    {
        Task<ActivationTokenDTO> GetByTokenValue(string token, CancellationToken cancellationToken = default);
    }
}
