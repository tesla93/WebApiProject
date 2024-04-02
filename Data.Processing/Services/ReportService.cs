using System.Threading.Tasks;
using jsreport.Local;
using jsreport.Types;
using System.Runtime.InteropServices;
using System.Net.Http;

namespace DataProcessing.Services
{
    /// <summary>
    /// Service for creating reports using node services
    /// </summary>
    public class ReportService : IReportService
    {
        /// <summary>
        /// Convert html to pdf
        /// </summary>
        /// <param name="htmlReport">HTML report</param>
        public async Task<byte[]> HtmlToPdf(string htmlReport)
        {
            var rs = new LocalReporting()
               .UseBinary(RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? jsreport.Binary.JsReportBinary.GetBinary() : jsreport.Binary.Linux.JsReportBinary.GetBinary())
               .Configure(cfg => cfg.FileSystemStore().BaseUrlAsWorkingDirectory())
               .AsUtility()
               .Create();

            var report = await rs.RenderAsync(new RenderRequest
            {
                Template = new Template
                {
                    Recipe = Recipe.ChromePdf,
                    Engine = Engine.None,
                    Content = htmlReport
                },
                Options = new RenderOptions
                {
                    Timeout = 300000
                }
            });

            using (var ms = new System.IO.MemoryStream())
            {
                report.Content.CopyTo(ms);

                // send the PDF file to browser
                return ms.ToArray();
            }
        }

        /// <summary>
        /// Downloading the page and converting html to pdf
        /// </summary>
        /// <param name="url">The address of the page to load</param>
        public async Task<byte[]> PageToPdf(string url)
        {
            using (var client = new HttpClient())
            {
                var response = await client.GetAsync(url);
                var html = await response.Content.ReadAsStringAsync();
                return await HtmlToPdf(html);
            }
        }
    }
}