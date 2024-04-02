using System.Threading.Tasks;
using SystemData.DTO;

namespace SystemData
{
    public interface ISystemDataService
    {
        Task<SystemDataDTO> Get();
        string GetDockerContainerId();
        Task<DockerInfoDTO> GetDockerInfo(string dockerId);
        Task<DockerMetadataDTO> GetDockerMetadata();
        Task<T> RequestDockerAsync<T>(string requestUrl) where T : class;
        string GetCommitHash();
        VersionInfoDTO GetVersionInfo();
    }
}