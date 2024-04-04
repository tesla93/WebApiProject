using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Module.DbDoc.Model.ValidationMetadata
{
    public class DbColumnValidationMetadata
    {
        [Required]
        public string Key { get; set; }

        [Required]
        public List<ValidationRule> Rules { get; set; } = new List<ValidationRule>();
    }
}