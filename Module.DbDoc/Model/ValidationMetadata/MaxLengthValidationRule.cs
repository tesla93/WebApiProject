using System.ComponentModel.DataAnnotations;
using Module.DbDoc.Services;

namespace Module.DbDoc.Model.ValidationMetadata
{
    public class MaxLengthValidationRule : ValidationRule
    {
        [Required, Range(1, double.PositiveInfinity)]
        public int MaxLength { get; set; }

        public override bool AcceptValidator(IDbModelValidator validator, object value)
        {
            return validator.Validate(this, value);
        }
    }
}