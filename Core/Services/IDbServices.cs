using Core.Data;
using Microsoft.AspNetCore.Http;

namespace Core.Services
{
    public interface IDbServices
    {
        IAuditWrapper GetAuditWrapper();

        IMultiTenancyService GetMultiTenancyService();

        IHttpContextAccessor GetContextAccessor();
    }
}