using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Messages;
using ReportProblem.DTO;
using SystemData;

namespace ReportProblem
{
    public class ReportProblemService : IReportProblemService
    {
        private readonly IEmailSender _emailSender;
        private readonly IOptionsSnapshot<SupportSettings> _supportSettings;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ISystemDataService _systemDataService;
        public ReportProblemService(IEmailSender emailSender, IOptionsSnapshot<SupportSettings> supportSettings, IHttpContextAccessor httpContextAccessor, ISystemDataService systemData )
        {
            _emailSender = emailSender;
            _supportSettings = supportSettings;
            _httpContextAccessor = httpContextAccessor;
            _systemDataService = systemData;
        }

        public async Task Send(ReportProblemDTO reportProblem, string userAgent, string baseUrl)
        {
            //Generate report body
           // var parameters = $"userEmail={reportProblem.Email.Trim() }&time={ reportProblem.Time.Trim() }&browserString={ userAgent.Trim() }" +
            //                    $"&severity={ reportProblem.Severity.Trim() }&error={ reportProblem.Description.Trim() }";
           /* var request = WebRequest.Create($"{baseUrl}/Report?{parameters}");
            var responseData = request.GetResponse();
            var dataStream = responseData.GetResponseStream();
            var reader = new StreamReader(dataStream);
            var responseFromServer = reader.ReadToEnd();

            reader.Close();
            responseData.Close();

            var rRemScript = new Regex(@"<head[^>]*>[\s\S]*?</head>");
           */

            var message = $"<div>User: { reportProblem.User } <br/>" +
                         $"Email: { reportProblem.Email } <br/>" +
                         $"From : { baseUrl} "+

                         $"Time: { reportProblem.Time } <br/>" +
                        $"Build Version of the site: - { _systemDataService.GetVersionInfo()?.ProductVersion } <br/>" +
                        $"Browser string: { userAgent } <br/><br/> " +

                         $"Description: { reportProblem.Description } <br/><br/>" +
                          $"Severity: { reportProblem.Severity }" +
                        $"</div>";

            var settings = _supportSettings.Value;
            await _emailSender.SendEmail(reportProblem.Subject, message, null, null, null, settings.EmailAddress1, settings.EmailAddress2,
                                              settings.EmailAddress3, settings.EmailAddress4);
        }

        public async Task AutoSend(Exception exception)
        {
            var settings = _supportSettings.Value;
            if ((settings.EmailAddress1 == null && settings.EmailAddress2 == null && settings.EmailAddress3 == null && settings.EmailAddress4 == null) || exception == null) return;

            var errorLogDTO = new ErrorLogDTO
            {
                ExceptionType = "Server side exception",
                ExceptionMessage = exception.Message,
                StackTrace = exception.ToString(),
                PathBase = $"{_httpContextAccessor.HttpContext.Request.Scheme}://{_httpContextAccessor.HttpContext.Request.Host}",
                Path = _httpContextAccessor.HttpContext.Request.Path
            };

            await AutoSend(errorLogDTO);
        }

        public async Task AutoSend(ErrorLogDTO errorLogDTO)
        {
            var settings = _supportSettings.Value;
            if ((settings.EmailAddress1 == null && settings.EmailAddress2 == null && settings.EmailAddress3 == null && settings.EmailAddress4 == null) || errorLogDTO == null) return;

            // Generates a body of the report.
            // (!) Note well: Current PTS System (Aug 18 2020) uses a regex pattern to fetch values from pairs
            // {title: value} (like {Path: errorLogDTO.Path} below).
            // The values help PTS to recognize how to process the email's body on the PTS side.
            // Ideally we would need some code interface to explicitely define relationship between BBWT3-based project
            // and PTS, but at the moment we just hardcode the body as it's done here.
            // Therefore, if you change any title name or remove it, then PTS may process it incorrectly
            // (may skip creating PTS ticket). Do keep it in mind.
            // If you do make a change in the body format, it's recommended to give a note to PTS managers.

            var message = $"<div>Exception type: {errorLogDTO.ExceptionType} <br/><br/>" +
                          $"Description: {errorLogDTO.ExceptionMessage} <br/><br/>" +
                          $"Path base: {errorLogDTO.PathBase} <br/>" +
                          $"Path: {errorLogDTO.Path} <br/><br/>" +
                          $"Stack trace: {errorLogDTO.StackTrace}" +
                          $"</div>";

            await _emailSender.SendEmail($"ERROR {errorLogDTO.ExceptionType}", message, null, null, null, settings.EmailAddress1, settings.EmailAddress2,
                                              settings.EmailAddress3, settings.EmailAddress4);
        }

        // private async Task<string> GetSystemData()
        // {
        //     StringBuilder strSystemData = new StringBuilder();
        //     var systemData = await _systemDataService.Get();

        //     if (systemData != null)
        //     {
        //         strSystemData.Append("System Data<br/><br/>");
        //         strSystemData.AppendFormat("Server name: {0}<br/>", systemData.ServerName);
        //         strSystemData.AppendFormat("Server IP: {0}<br/>", systemData.ServerIp);
        //         strSystemData.AppendFormat("Client IP: {0}<br/>", systemData.ClientIp);
        //         strSystemData.AppendFormat("Commit Hash: {0}<br/><br/>", systemData.CommitHash);

        //         if (systemData.DockerInfo != null)
        //         {
        //             strSystemData.Append("Docker Info: <br/><br/>");
        //             strSystemData.AppendFormat("Arn: {0}<br/>", systemData.DockerInfo.Arn);
        //             strSystemData.AppendFormat("Desired status: {0}<br/>", systemData.DockerInfo.DesiredStatus);
        //             strSystemData.AppendFormat("Family: {0}<br/>", systemData.DockerInfo.Family);
        //             strSystemData.AppendFormat("Known status: {0}<br/>", systemData.DockerInfo.KnownStatus);
        //             strSystemData.AppendFormat("Version: {0}<br/><br/>", systemData.DockerInfo.Version);

        //             if (systemData.DockerInfo.Containers.Count() > 0)
        //             {
        //                 strSystemData.Append("Containers: <br/><br/>");

        //                 foreach (var container in systemData.DockerInfo.Containers)
        //                 {
        //                     strSystemData.AppendFormat("Name: {0}<br/>", container.Name);
        //                     strSystemData.AppendFormat("Docker Id: {0}<br/>", container.DockerId);
        //                     strSystemData.AppendFormat("Docker name: {0}<br/><br/>", container.DockerName);
        //                 }
        //             }
        //         }

        //         if (systemData.DockerMetadata != null)
        //         {
        //             strSystemData.Append("Docker Metadata: <br/><br/>");
        //             strSystemData.AppendFormat("Cluster: {0}", systemData.DockerMetadata.Cluster);
        //             strSystemData.AppendFormat("Container instance Arn: {0}", systemData.DockerMetadata.ContainerInstanceArn);
        //             strSystemData.AppendFormat("Version: {0}", systemData.DockerMetadata.Version);
        //         }
        //     }

        //     return strSystemData.Append("<br/><br/>").ToString();
        // }
    }
}
