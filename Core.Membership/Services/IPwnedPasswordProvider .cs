using System.Net.Http;
using System.Threading.Tasks;

namespace Core.Membership.Services
{
    public interface IPwnedPasswordProvider
    {
        Task<string> GetPasswordPwned(string passwordSHA1);
    }
}
