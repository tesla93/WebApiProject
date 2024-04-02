using System;
using System.Threading.Tasks;
using AutoMapper;
using Core.Data;
using Microsoft.EntityFrameworkCore;
using Core.Membership.Model;
using Core.Membership.DTO;
using Core.Crud;

namespace Core.Membership.Services
{
    public class LoginAuditService: PagedCrudService<LoginAudit, LoginAuditDTO>, ILoginAuditService
    {
        public LoginAuditService(IDbContext context, IMapper mapper) : base(context, mapper) { }


        public async Task<int> GetLastAttemptsCount(string ip, DateTimeOffset withInDate) =>
            await GetQueryable<LoginAudit>().CountAsync(c => c.Ip == ip && c.Datetime > withInDate);
    }
}