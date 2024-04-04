using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FileStorage;
using Microsoft.AspNetCore.Hosting;

namespace Module.FileStorage.DiskSpace
{
    public class DiskSpaceStorageProvider : IFileStorageProvider
    {
        private readonly string _path;


        public DiskSpaceStorageProvider(
            IWebHostEnvironment hostingEnvironment) =>
            _path = Path.Combine($"{hostingEnvironment.WebRootPath}", "data", "images");


        public Task<bool> DeleteFile(string key, CancellationToken cancellationToken = default)
        {
            return Task.Run(() =>
            {
                File.Delete(GetFilePath(key));
                return true;
            });
        }

        public Task<StorageFileData> GetFile(string key, CancellationToken cancellationToken = default)
        {
            return Task.Run(() =>
            {
                var fileInfo = new FileInfo(GetFilePath(key));
                return !fileInfo.Exists
                    ? null
                    : new StorageFileData
                    {
                        Key = key,
                        IsImage = true,
                        LastModifiedDate = fileInfo.LastWriteTimeUtc,
                        Size = fileInfo.Length,
                        Url = $"data/images/{key}"
                    };
            }, cancellationToken);
        }

        public async Task<StorageFileData> UploadFile(Stream stream, string key, CancellationToken cancellationToken = default)
        {
            if (!Directory.Exists(_path)) Directory.CreateDirectory(_path);

            using (var fileStream = File.Create(GetFilePath(key)))
            {
                stream.Seek(0, SeekOrigin.Begin);
                await stream.CopyToAsync(fileStream);
            }

            return new StorageFileData
            {
                Key = key,
                Url = $"data/images/{key}",
            };
        }


        private string GetFilePath(string key) => Path.Combine(_path, key);
    }
}
