using AutofacExtensions;
using DataProcessing.Classes;
using DataProcessing.FileReaders;
using DataProcessing.Validation;
using Core.Exceptions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DataProcessing.Services
{
    /// <summary>
    /// Data import helper implementation
    /// </summary>
    public class DataImportHelper : IDataImportHelper
    {
        /// <summary>
        /// IDataImportReaderProvider instance
        /// </summary>
        private readonly IDataImportReaderProvider _dataImportReaderProvider;
        private readonly ITypeValidatorsProvider _typeValidatorsProvider;


        /// <summary>
        /// Custom constructor
        /// </summary>
        /// <param name="dataImportReaderProvider"></param>
        public DataImportHelper(IDataImportReaderProvider dataImportReaderProvider, ITypeValidatorsProvider typeValidatorsProvider)
        {
            _dataImportReaderProvider = dataImportReaderProvider;
            _typeValidatorsProvider = typeValidatorsProvider;
        }

        #region Import

        /// <summary>
        /// Processes the data import from scv,xls,xlsx file stream
        /// </summary>
        /// <param name="config">Import configuration</param>
        /// <param name="onEntryProcessedCallback">Callback which is called on each processed entry</param>
        [IgnoreLogging]
        public DataImportResult ProcessDataImport(DataImportConfig config, OnEntryProcessedCallback onEntryProcessedCallback = null)
        {
            if (config == null)
                throw new ArgumentNullException("config is null");
            if (config.FileStream == null)
                throw new ArgumentException("config.FileStream is null");
            if (string.IsNullOrWhiteSpace(config.FileName))
                throw new ArgumentException("config.FileName is null or empty");

            if (config.FileStream.Length == 0)
                return new DataImportResult("Your file is empty");

            var extension = Path.GetExtension(config.FileName);
            try
            {
                byte[] StreamByte = null;
                StreamByte = ((MemoryStream)config.FileStream).ToArray();

                var reader = _dataImportReaderProvider.GetReader(extension);
                if (reader is ExcelFileReader)
                {
                    var excelReader = (ExcelFileReader)reader;
                    var dataReader = excelReader.CreateExcelDataReader(new MemoryStream(StreamByte));
                    config.LastRow = dataReader.RowCount - 2;
                }
                var parsedRows = reader.ReadFile(new MemoryStream(StreamByte), config.FirstRow, config.LastRow, config.SheetName);

                var entries = ProcessDataImportInternal(parsedRows, config, onEntryProcessedCallback);
                var result = new DataImportResult(entries.ToList());


                return result;
            }
            catch (ArgumentException e)
            {
                return new DataImportResult(e.Message);
            }
        }

        /// <summary>
        /// Processes the data import from enumerable with parsed rows
        /// </summary>
        /// <param name="parsedRows">Parsed rows enumerable</param>
        /// <param name="config">Import configuration</param>
        /// <param name="onEntryProcessedCallback">Callback which is called on each processed entry</param>
        /// <returns></returns>
        private IEnumerable<ImportEntry> ProcessDataImportInternal(IEnumerable<object[]> parsedRows, DataImportConfig config, OnEntryProcessedCallback onEntryProcessedCallback)
        {
            var errorsCount = 0;
            var currentRow = config.FirstRow;
           
            foreach (var row in parsedRows)
            {
                var entry = ValidateRow(row, config.ColumnDefinitions);
                entry.LineNumber = currentRow++;

                if (!entry.IsValid)
                {
                    if (config.SkipInvalidRows)
                        continue;

                    errorsCount++;
                }

                if (RaiseOnEntryProcessedCallback(entry, onEntryProcessedCallback))
                    yield break;

                if (config.MaxErrorsCount.HasValue && errorsCount > config.MaxErrorsCount)
                    yield break;

                yield return entry;
            }

        }

        /// <summary>
        /// Raises OnEntryProcessedCallback
        /// </summary>
        /// <param name="entry">Entry</param>
        /// <param name="callback">Callback instance</param>
        /// <returns>If true, the user wants to stop processing</returns>
        private bool RaiseOnEntryProcessedCallback(ImportEntry entry, OnEntryProcessedCallback callback)
        {
            if (callback != null)
            {
                var args = new OnEntryProcessedArgs(entry);
                callback.Invoke(args);
                return args.IsProcessStopped;
            }
            return false;
        }

        #endregion

        #region Validation

        /// <summary>
        /// Performs the row validation and returns ImportEntry instance
        /// </summary>
        /// <param name="row">The row's cells array</param>
        /// <param name="columnDefinitions">column definitions</param>
        /// <returns>ImportEntry intance</returns>
        private ImportEntry ValidateRow(object[] row, ColumnsDefinitionsCollection columnDefinitions)
        {
            var result = new ImportEntry(row);

            for (var i = 0; i < row.Length && i < columnDefinitions.Count; i++)
            {
                var colDef = columnDefinitions.ElementAt(i);
                if (colDef.Position > row.Length)
                    continue;
                var res = ValidateCell(row[colDef.Position - 1], colDef);
                if (!res.IsValid)
                {
                    res = ValidateCell(row[colDef.Position - 1], colDef);
                }
                result.Cells.Add(res);
            }

            return result;
        }

        /// <summary>
        /// Performs the cell validation and returns ImportEntryCell instance
        /// </summary>
        /// <param name="cellValue">Cell value</param>
        /// <param name="columnDefinition">Column definition</param>
        /// <returns>ImportEntryCell instance</returns>
        [IgnoreLogging]
        private ImportEntryCell ValidateCell(object cellValue, ColumnDefinition columnDefinition)
        {
            var result = new ImportEntryCell(cellValue, columnDefinition);

            if (CellValueIsNullOrEmpty(result.Value))
            {
                if (!columnDefinition.IsAllowNulls)
                {
                    result.ErrorMessage = "Null value is not allowed";
                }
                else if (columnDefinition.DefaultValue != null)
                {
                    result.Value = columnDefinition.DefaultValue;
                }
            }
            else
            {
                _typeValidatorsProvider.GetValidator(columnDefinition).PerformValidation(result);
            }

            return result;
        }

        /// <summary>
        /// Determines whether the value of the cell is null or empty
        /// </summary>
        /// <param name="cellValue">ImportEntryCell instance</param>
        /// <returns></returns>
        private bool CellValueIsNullOrEmpty(object cellValue)
        {
            return string.IsNullOrEmpty(Convert.ToString(cellValue));
        }
        #endregion

        /*private IWorkbook CreateSpreadsheetDocument(IEnumerable<ImportEntry> importEntries, DataImportConfig config)
        {
            IWorkbook workbook = new XSSFWorkbook();

            var sheet = workbook.CreateSheet("import result");
            var patr = sheet.CreateDrawingPatriarch();
            var column = 0;
            var line = 0;
            var styleError = workbook.CreateCellStyle();
            styleError.BorderBottom = BorderStyle.Thin;
            styleError.BottomBorderColor = HSSFColor.Red.Index;
            styleError.BorderLeft = BorderStyle.Thin;
            styleError.LeftBorderColor = HSSFColor.Red.Index;
            styleError.BorderRight = BorderStyle.Thin;
            styleError.RightBorderColor = HSSFColor.Red.Index;
            styleError.BorderTop = BorderStyle.Thin;
            styleError.TopBorderColor = HSSFColor.Red.Index;
            var header = sheet.CreateRow(0);
            //create header

            config.ColumnDefinitions.ToList().ForEach(X => {
                header.CreateCell(column).SetCellValue(new XSSFRichTextString(X.TargetFieldName));
                column++;
            });


            foreach (var entry in importEntries)
            {
                line++;
                column = 0;
                var row = sheet.CreateRow(line);
                foreach (var data in entry.Cells)
                {
                    var cell = row.CreateCell(column);
                    cell.SetCellValue(new XSSFRichTextString(Convert.ToString(data.Value)));
                    if (!data.IsValid)
                    {
                        var anchor = new XSSFClientAnchor()
                        {
                            Col1 = column + 1,
                            Col2 = column + 5,
                            Row1 = line,
                            Row2 = line + 3
                        };
                        var comment = patr.CreateCellComment(anchor);
                        comment.String = (new XSSFRichTextString(data.ErrorMessage));
                        comment.Author = "Error";
                        cell.CellComment = comment;
                        cell.CellStyle = styleError;
                    }
                    column++;
                }
            }
            return workbook;
        }*/
    }
}
