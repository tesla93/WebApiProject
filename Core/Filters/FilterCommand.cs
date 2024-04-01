using System;

namespace Core.Filters
{
    public class FilterCommand : Filter, IPager, ISorter
    {
        private int _rows;
        private int _first;
        private string _sortField;

        public FilterCommand() : base()
        {
            SortField = "Id";
            SortOrder = OrderDirection.Desc;
            MaxRows = 100000;
            First = 0;
            Rows = 10;
            IsPaginator = true;
        }

        /// <summary>
        /// Maximum Count of items per Page
        /// </summary>
        protected int MaxRows;

        /// <summary>
        /// Total items count
        /// </summary>
        public decimal Total { get; set; }

        /// <summary>
        /// Count of Rows per Page
        /// </summary>
        public int Rows
        {
            get => _rows;
            set => _rows = value < 0 ? 0 : value > MaxRows ? MaxRows : value;
        }

        /// <summary>
        /// First element
        /// </summary>
        public int First
        {
            get => _first;
            set => _first = value < 0 ? 0 : value;
        }

        /// <summary>
        /// Number of Pages
        /// </summary>
        public int Pages => IsPaginator ? (int)Math.Ceiling(Total / Rows) : 1;

        /// <summary>
        /// Number of records to Skip
        /// </summary>
        public int Skip => First;

        /// <summary>
        /// Number of records to Take
        /// </summary>
        public int Take => Rows;

        /// <summary>
        /// Order By
        /// </summary>
        public string SortField
        {
            get => _sortField;
            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    return;
                }

                // Upper-casing first letters
                var array = value.ToCharArray();

                if (array.Length >= 1)
                {
                    if (char.IsLower(array[0]))
                    {
                        array[0] = char.ToUpper(array[0]);
                    }
                }

                for (var i = 1; i < array.Length; i++)
                {
                    if (array[i - 1] != '.') continue;
                    if (char.IsLower(array[i]))
                    {
                        array[i] = char.ToUpper(array[i]);
                    }
                }
                _sortField = new string(array);
            }
        }

        /// <summary>
        /// Sort order (asc, desc).
        /// </summary>
        public OrderDirection SortOrder { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to use pagination
        /// </summary>
        public bool IsPaginator { get; set; }
    }
}