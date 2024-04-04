using System.ComponentModel.DataAnnotations;
using Module.DbDoc.Model;

namespace Module.DbDoc.Web
{
    public class DbDocGridColumnWidthValidationAttribute : ValidationAttribute
    {
        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            var details = (GridColumnViewDetails) validationContext.ObjectInstance;
            return details.MinWidth > details.MaxWidth ? new ValidationResult("Max Width should be greater then Min Width") : ValidationResult.Success;
        }
    }
}