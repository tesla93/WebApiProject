using System.Collections.Generic;

namespace Core.Filters
{
    public class Filter
    {
        public Filter()
        {
            Filters = new List<FilterInfoBase>();
        }
        public List<FilterInfoBase> Filters { get; set; }
    }
}