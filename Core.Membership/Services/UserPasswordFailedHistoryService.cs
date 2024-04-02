using System;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using AutoMapper;
using Core.Data;
using Core.Membership.DTO;
using Core.Membership.Model;
using Core.Crud;
using System.Threading;

namespace Core.Membership.Services
{
    public class UserPasswordFailedHistoryService : CrudService<UserPasswordFailedHistory, UserPasswordFailedHistoryDTO>, IUserPasswordFailedHistoryService
    {
        public UserPasswordFailedHistoryService(IDbContext context, IMapper mapper) : base(context, mapper)
        {
        }


        public IQueryable<UserPasswordFailedHistory> SelectSimpleQuery(CriteriaUpfh criteria)
        {
            var query = GetQueryable<UserPasswordFailedHistory>().AsQueryable();
            if (!string.IsNullOrEmpty(criteria.Email))
            {
                query = query.Where(x => x.email == criteria.Email);
            }

            if (criteria.MinInterval > 0)
            {
                var test = DateTime.Now.AddSeconds(-criteria.MinInterval);
                query = query.Where(x => x.failedDate >= test);
            }

            if (!string.IsNullOrEmpty(criteria.Ip))
            {
                query = query.Where(x => x.IpAddress == criteria.Ip);
            }

            return query;
        }

        public async Task ClearFromPasswordFailedHistory(string email, CancellationToken cancelationToken)
        {
            var emailParam = new SqlParameter("@email", SqlDbType.Text) { Value = email };
            await ExecuteSqlCommandAsync("DELETE FROM UserPasswordFailedHistory WHERE email =@email", new[] { emailParam }, cancelationToken);
        }
    }
}