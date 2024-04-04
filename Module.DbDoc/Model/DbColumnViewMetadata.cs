using System.ComponentModel.DataAnnotations;

namespace Module.DbDoc.Model
{
    public class DbColumnViewMetadata
    {
        [Required]
        public string Key { get; set; }

        [Required]
        public GridColumnViewDetails GridColumnDetails { get; set; }
    }
}