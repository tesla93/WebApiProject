using System.Threading.Tasks;

namespace DataProcessing.Services
{
    public interface IReportService
    {
        Task<byte[]> HtmlToPdf(string reportPage);
        Task<byte[]> PageToPdf(string url);
    }
}