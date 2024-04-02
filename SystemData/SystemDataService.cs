using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using SystemData.DTO;

namespace SystemData
{
    internal class SystemDataService : ISystemDataService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IWebHostEnvironment _hostingEnvironment;

        public SystemDataService(IHttpContextAccessor httpContextAccessor, IWebHostEnvironment hostingEnvironment)
        {
            _httpContextAccessor = httpContextAccessor;
            _hostingEnvironment = hostingEnvironment;
        }

        public async Task<SystemDataDTO> Get()
        {
            var httpContext = _httpContextAccessor.HttpContext;
            var commitHash = GetCommitHash();

            string dockerId = null;
            DockerInfoDTO dockerInfo = null;
            DockerMetadataDTO dockerMetadata = null;

            try
            {
                dockerId = GetDockerContainerId();
                dockerInfo = await GetDockerInfo(dockerId);
                dockerMetadata = await GetDockerMetadata();
            }
            catch(Exception ex)
            {
                // If docker matadata is not fetched then we ignore it
                Console.WriteLine(ex);
            }

            var data = new SystemDataDTO
            {
                UserName = httpContext.User.Identity.Name,
                ServerName = Environment.MachineName,
                ServerIp = GetIpAddress(),
                ClientIp = httpContext.Connection.RemoteIpAddress.ToString(),
                CommitHash = commitHash,
                DockerInfo = dockerInfo,
                DockerMetadata = dockerMetadata,
                OperatingSystem = RuntimeInformation.OSDescription
            };

            return data;
        }

        private string GetIpAddress()
        {
            var pubIp = new WebClient().DownloadString("https://api.ipify.org");
            return pubIp;

        }

        public string GetDockerContainerId()
        {
            return Dns.GetHostName();
        }

        public Task<DockerInfoDTO> GetDockerInfo(string dockerId)
        {
            return RequestDockerAsync<DockerInfoDTO>($"tasks?dockerid={dockerId}");
        }

        public Task<DockerMetadataDTO> GetDockerMetadata()
        {
            return RequestDockerAsync<DockerMetadataDTO>("metadata");
        }

        public /*async*/ Task<T> RequestDockerAsync<T>(string requestUrl) where T : class
        {
            return null;
            //not working
            /*if (_hostingEnvironment.IsDevelopment())
            {
                return null;
            }
            using (var client = new HttpClient())
            {
                try
                {
                    client.BaseAddress = new Uri("http://172.17.0.1:51678/v1/");
                    var response = await client.GetAsync(requestUrl);
                    var stringData = await response.Content.ReadAsStringAsync();
                    var info = JsonConvert.DeserializeObject<T>(stringData);
                    return info;
                }
                catch
                {
                    return null;
                }
            }*/
        }

        public string GetCommitHash()
        {
            try
            {
                // version content format: [template version (3.x.x)]-[pipeline ID]-[commit hash]-[Git project ID]
                // in CI script: ${BBWT_VERSION}-${CI_PIPELINE_ID}-${CI_COMMIT_SHORT_SHA}-${CI_PROJECT_ID}
                var path = Path.Combine(_hostingEnvironment.WebRootPath, "version.txt");
                if (!File.Exists(path)) return null;

                var output = File.ReadAllLines(path)[0];
                var parts = output.Split('-');
                return parts.Length > 2 ? parts[2] : null;
            }
            catch
            {
                return null;
            }
        }

        public VersionInfoDTO GetVersionInfo()
        {
            var fileVersionInfo = FileVersionInfo.GetVersionInfo(System.Reflection.Assembly.GetExecutingAssembly().Location);

            var versionInfo = new VersionInfoDTO
            {
                FileVersion = fileVersionInfo.FileVersion,
                ProductVersion = fileVersionInfo.ProductVersion
            };

            if (fileVersionInfo.ProductVersion.Contains('-'))
            {
                var splitInfo = fileVersionInfo.ProductVersion.Split('-');
                var pipeline = splitInfo[1] ?? "";
                var projectId = splitInfo[3] ?? "";

                versionInfo.Pipeline = pipeline;
                versionInfo.ProjectID = projectId;
            }

            return versionInfo;
        }
    }
}