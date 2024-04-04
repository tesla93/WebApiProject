using System;
using System.ComponentModel.DataAnnotations;
using Module.DbDoc.Web;

namespace Module.DbDoc.Model
{
    public class GridColumnViewDetails
    {
        [Required, Range(1, Double.PositiveInfinity)]
        public float MinWidth { get; set; }

        [Range(1, Double.PositiveInfinity), DbDocGridColumnWidthValidation]
        public float? MaxWidth { get; set; }
    }
}