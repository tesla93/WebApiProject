using DataProcessing.Classes;
using DataProcessing.DTO;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace DataProcessing.Services
{
    public interface IImportService
    {
        DataImportConfig CreateSettings(ImportDataModel importDataModel, MemoryStream memoryStream, string userId);
        Task<DataImportResultDTO> Import(DataImportConfig dataImportConfig, CancellationToken cancellationToken);
    }
}
