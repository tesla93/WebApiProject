using System;
using System.Collections.Generic;

namespace DataProcessing.Export
{
    public class ReportCSVService<T>
    {
        public ReportCSVService()
        {

        }

        public CsvExport GetCsvFile(List<T> datas, List<ColumnSetting<T>> settings, Action<List<ColumnSetting<T>>, CsvExport, List<T>> footer = null)
        {
            var csvExport = new CsvExport();

            //Create Header
            csvExport.AddRow();
            foreach(var column in settings)
            {
                csvExport[column.Header] = column.Header;
            }

            foreach (var row in datas)
            {
                csvExport.AddRow();
                foreach (var column in settings)
                {
                    csvExport[column.Header] = column.GetValue(row);
                }
            }
            //Footer
            if (footer != null)
            {
                csvExport.AddRow();
                footer(settings, csvExport, datas);
                
            }
            return csvExport;
        }
    }
}
