using System.Collections.Generic;

namespace DataProcessing.Services
{
    public interface IGridService
    {
        // IWorkbook CreateSpreadsheetDocument<T>(List<T> data, GridData grid);
        byte[] PrintExcel<T>(IEnumerable<T> data, GridData grid);

        byte[] PrintCSV<T>(IEnumerable<T> data, GridData grid);
    }

    public class GridTableColumns
    {
        public string field { get; set; }
        public string header { get; set; }
        public string sortField { get; set; }
    }

    public class GridData
    {
        public List<GridTableColumns> GridTableColumns { get; set; }
        public List<int> Ids { get; set; }
    }
}
