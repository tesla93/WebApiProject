using System;
using System.ComponentModel.DataAnnotations;
using Module.DbDoc.Enums;
using Module.DbDoc.Services;

namespace Module.DbDoc.Model.ValidationMetadata
{
    public class InputFormatValidationRule : ValidationRule
    {
        [Required]
        public InputFormatType Type { get; set; }
        public string Format { get; set; }

        public override bool AcceptValidator(IDbModelValidator validator, object value)
        {
            try
            {
                var str = Convert.ToString(value);
                return validator.Validate(this, str);
            }
            catch
            {
                return false;
            }
        }
    }
}