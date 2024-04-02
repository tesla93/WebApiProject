using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

namespace DataProcessing.Services
{
    public class GridService: IGridService
    {
        private IWorkbook CreateSpreadsheetDocument<T>(IEnumerable<T> data, GridData grid)
        {
            IWorkbook workbook = new XSSFWorkbook();
            var sheet = workbook.CreateSheet("export table");
            var patr = sheet.CreateDrawingPatriarch();
            var column = 0;
            var line = 0;
            var header = sheet.CreateRow(0);

           // header
            foreach (var h in grid.GridTableColumns)
            {
                header.CreateCell(column).SetCellValue(new XSSFRichTextString(h.header));
                column++;
            }

            foreach (var entry in data)
            {
                line++;
                column = 0;
                var row = sheet.CreateRow(line);

                foreach (var g in grid.GridTableColumns)
                {
                    var cell = row.CreateCell(column);
                    var type = entry.GetType();
                    var field = !string.IsNullOrEmpty(g.sortField) ? g.sortField : g.field;
                    var property = type.GetProperty(field, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
                    var value = property.GetValue(entry, null);

                    if(property.PropertyType == typeof(DateTime) && value != null)
                    {
                        cell.SetCellValue((DateTime)value);
                    }

                    if(property.PropertyType == typeof(double) && value != null)
                    {
                        cell.SetCellValue((double)value);
                    }
                    else
                    {
                        cell.SetCellValue(new XSSFRichTextString(value != null ? value.ToString() : string.Empty));
                    }
                    column++;
                }
            }

            return workbook;
        }

        public byte[] PrintExcel<T>(IEnumerable<T> data, GridData grid)
        {
            var ExcelResult = CreateSpreadsheetDocument<T>(data, grid);
            Stream stream = new MemoryStream();
            ExcelResult.Write(stream);
            return ((MemoryStream)stream).ToArray();
        }

        public byte[] PrintCSV<T>(IEnumerable<T> data, GridData grid)
        {
            var csv = new StringBuilder("");
            grid.GridTableColumns.ForEach(x => csv.AppendFormat("{0},", x.header));
            csv.Append("\r\n");

            foreach(var entry in data)
            {
                foreach(var g in grid.GridTableColumns)
                {
                    var type = entry.GetType();
                    var field = !string.IsNullOrEmpty(g.sortField) ? g.sortField : g.field;
                    if (field == "id_original") field = "id";
                    var property = type.GetProperty(field, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance );

                    var value = property.GetValue(entry, null);
                    csv.AppendFormat("{0},", value);
                }

                csv.Append("\r\n");
            }

            return Encoding.ASCII.GetBytes(csv.ToString());

        }
    }
}
