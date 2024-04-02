using DataProcessing.DTO;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DataProcessing.Classes
{
    public class ImportDataModel
    {
        private string _fileName;

        [FromForm]
        public DataImportConfigDTO Config { get; set; }

        public IFormFile File { get; set; }
        public bool UseOverride { get; set; }

        public string FileName
        {
            get
            {
                _fileName = _fileName ?? File?.FileName;
                return _fileName;
            }
            set
            {
                _fileName = value;
            }
        }
    }
}
