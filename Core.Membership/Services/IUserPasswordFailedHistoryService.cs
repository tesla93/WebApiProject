using Core.Membership.DTO;
using Core.Membership.Model;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using Core.Crud.Interfaces;

namespace Core.Membership.Services
{
    public interface IUserPasswordFailedHistoryService : ICrudService<UserPasswordFailedHistoryDTO>
    {
        IQueryable<UserPasswordFailedHistory> SelectSimpleQuery(CriteriaUpfh criteria);
        Task ClearFromPasswordFailedHistory(string email, CancellationToken cancelationToken);
    }


    public class CriteriaUpfh
    {
        public string Email { get; set; }
        public int MinInterval { get; set; }
        public string Ip { get; set; }
    }
}