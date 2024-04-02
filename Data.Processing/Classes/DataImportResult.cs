using NPOI.SS.UserModel;
using System.Collections.Generic;
using System.Linq;

namespace DataProcessing.Classes
{
    /// <summary>
    /// The data import result
    /// </summary>
    public class DataImportResult
    {
        /// <summary>
        /// Custom constructor
        /// </summary>
        /// <param name="entries">The ImportEntry's enumeration</param>
        public DataImportResult(IEnumerable<ImportEntry> entries)
        {
            Result = new List<ImportEntry>(entries);
        }

        /// <summary>
        /// Custom constructor
        /// </summary>
        /// <param name="warning">warning message</param>
        public DataImportResult(string warning)
        {
            Warning = warning;
        }

        /// <summary>
        /// The user friendly warning message
        /// </summary>
        public string Warning { get; set; }

        /// <summary>
        /// All processed entries
        /// </summary>
        public List<ImportEntry> Result { get; private set; }

        /// <summary>
        /// Only invalid processed entries
        /// </summary>
        public List<ImportEntry> InvalidEntries
        {
            get { return Result?.Where(a => !a.IsValid).ToList() ?? new List<ImportEntry>(); }
        }

        /// <summary>
        /// Only valid processed entries
        /// </summary>
        public IEnumerable<ImportEntry> ValidEntries
        {
            get { return Result?.Where(a => a.IsValid) ?? Enumerable.Empty<ImportEntry>(); }
        }


        public IWorkbook ExcelResult { get; set; }

        /// <summary>
        /// Imported entries count
        /// </summary>
        public int ImportedCount { get; set; }

        /// <summary>
        /// File name
        /// </summary>
        public string FileName { get; set; }
    }
}
