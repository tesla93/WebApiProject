using Core.Data;
using Microsoft.AspNetCore.Http;

namespace Core.Services
{
    public class DbServices : IDbServices
    {
        private readonly IMultiTenancyService _multiTenancyService;
        private readonly IAuditWrapper _auditWrapper;
        private readonly IHttpContextAccessor _contextAccessor;

        public DbServices(IAuditWrapper auditWrapper = null, IMultiTenancyService multiTenancyService = null, IHttpContextAccessor contextAccessor = null)
        {
            _multiTenancyService = multiTenancyService;
            _auditWrapper = auditWrapper;
            _contextAccessor = contextAccessor;
        }

        public IAuditWrapper GetAuditWrapper()
        {
            return _auditWrapper;
        }

        public IMultiTenancyService GetMultiTenancyService()
        {
            return _multiTenancyService;
        }

        public IHttpContextAccessor GetContextAccessor()
        {
            return _contextAccessor;
        }
    }
}