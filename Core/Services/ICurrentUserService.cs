using System.Collections.Generic;
using System.Threading.Tasks;

namespace Core.Services
{
    public interface ICurrentUserService
    {
        string GetCurrentUserId();
        Task<string> GetCurrentUserEmail();
        Task<List<string>> GetCurrentUserRoles();
        string GetUserIp();
    }
}
