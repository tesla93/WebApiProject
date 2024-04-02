using System.Threading.Tasks;

namespace Messages
{
    public interface ISmsSender
    {
        Task SendSms(string number, string message);
    }
}
