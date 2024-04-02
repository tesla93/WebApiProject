using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using CsvHelper;
using CsvHelper.Configuration;

namespace DataProcessing.FileReaders
{
    /// <summary>
    /// IDataImportReader implementation which reads csv files
    /// </summary>
    public class CSVFileReader : IDataImportReader
    {
        /// <summary>
        /// Reads the file and returns rows enumerable
        /// </summary>
        /// <param name="fileStream">File stream</param>
        /// <param name="firstRow">The first row from which a file is read</param>
        /// <param name="lastRow">The last row (optional)</param>
        /// <param name="sheetName">Sheet Name</param>
        /// <returns>Rows enumerable</returns>
        //public IEnumerable<object[]> ReadFile(Stream fileStream, int firstRow, int? lastRow, string sheetName)
        //{
        //    var csvConfig = CreateCsvConfiguration(fileStream);
        //    var parser = new CsvParser(new StreamReader(fileStream), csvConfig);

        //    for (var i = 1; ; i++)
        //    {
        //        var row = parser.Read();
        //        if (row == null) break;

        //        if (i < firstRow) continue;
        //        if (lastRow.HasValue && i > lastRow) break;

        //        yield return new List<List<object>>();
        //         throw new NotImplementedException();
        //    }
        //}

        /// <summary>
        /// Creates CsvConfiguration's instance from file stream
        /// </summary>
        /// <param name="fileStream">File stream</param>
        /// <returns>CsvConfiguration instance</returns>
        private CsvConfiguration CreateCsvConfiguration(Stream fileStream)
        {
            var csvConfig = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = false,
            };
            csvConfig.Delimiter = GetSeparator(fileStream);
            return csvConfig;
        }

        /// <summary>
        /// Determines if there is the "sep=" line at the beginning of the file
        /// </summary>
        /// <param name="fileStream">File stream</param>
        /// <param name="separator">separator</param>
        /// <returns>true if excel separator exists</returns>
        private bool TryToGetExcelSeparator(Stream fileStream, out string separator)
        {
            var sr = new StreamReader(fileStream);
            var firstLine = sr.ReadLine().TrimStart();

            ResetStreamReader(sr);

            var tag = "sep=";
            
            var res = firstLine.StartsWith(tag);
            if (res)
            {
                separator = firstLine.Substring(tag.Length);
            }
            separator = null;
            return res;
        }

        /// <summary>
        /// Determines and returns csv separator
        /// </summary>
        /// <param name="fileStream"></param>
        /// <returns>csv separator</returns>
        private string GetSeparator(Stream fileStream)
        {
            string excelSeparator;
            if (TryToGetExcelSeparator(fileStream, out excelSeparator))
            {
                return excelSeparator;
            }
            char[] separators = { ',', ';', '|', '\t' };

            var avgSeparatorsCounts = new Dictionary<char, int>();

            var firstRows = new List<string>();

            var i = 0;

            var sr = new StreamReader(fileStream);

            while (i++ < 10)
            {
                var row = sr.ReadLine();
                if (row == null) break;

                firstRows.Add(row);
            }

            ResetStreamReader(sr);

            foreach (var sep in separators)
            {
                avgSeparatorsCounts[sep] = 0;
                var counts = new List<int>();

                foreach (var row in firstRows)
                {
                    counts.Add(row.Count(c => c == sep));
                }

                avgSeparatorsCounts[sep] = (int)Math.Ceiling(counts.DefaultIfEmpty().Average());
            }

            return separators.First(sep => avgSeparatorsCounts[sep] == avgSeparatorsCounts.Max(a => a.Value)).ToString();
        }

        private void ResetStreamReader(StreamReader reader)
        {
            reader.BaseStream.Position = 0;
            reader.DiscardBufferedData();
        }

        IEnumerable<object[]> IDataImportReader.ReadFile(Stream fileStream, int firstRow, int? lastRow, string sheetName)
        {
            throw new NotImplementedException();
        }
    }
}