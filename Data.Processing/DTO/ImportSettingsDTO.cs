using System.IO;

namespace DataProcessing.DTO
{
    public class ImportSettings
    {
        public Stream File { get; set; }

        public string FileName { get; set; }

        public DataImportConfigDTO Config { get; set; }
    }
}
