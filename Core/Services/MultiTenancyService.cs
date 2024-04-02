using Microsoft.AspNetCore.Http;
using System.Linq;
using System.Security.Claims;

namespace Core.Services
{
    public class MultiTenancyService : IMultiTenancyService
    {
        public const string TenantField = "TenantId";

        private readonly IHttpContextAccessor _contextAccessor;


        public MultiTenancyService(IHttpContextAccessor contextAccessor) => _contextAccessor = contextAccessor;


        public int? GetTenancyId() =>
            int.TryParse(
                ((ClaimsIdentity) _contextAccessor.HttpContext?.User?.Identity)?.Claims.FirstOrDefault(x => x.Type == TenantField)?.Value,
                out var tenantId)
                ? (int?)tenantId
                : null;
    }
}