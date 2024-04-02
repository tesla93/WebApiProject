using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace FileStorage
{
    public interface IFileStorageProvider
    {
        Task<StorageFileData> UploadFile(Stream stream, string key, CancellationToken cancellationToken = default);
        Task<StorageFileData> GetFile(string key, CancellationToken cancellationToken = default);
        Task<bool> DeleteFile(string key, CancellationToken cancellationToken = default);
    }
}