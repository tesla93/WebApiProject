using System;
using System.Threading.Tasks;
using ReportProblem.DTO;

namespace ReportProblem
{
    public interface IReportProblemService
    {
        Task Send(ReportProblemDTO reportProblem, string userAgent, string baseUrl);
        Task AutoSend(Exception exception);
        Task AutoSend(ErrorLogDTO errorLogDTO);
    }
}
