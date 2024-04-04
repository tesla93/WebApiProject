using System;
using Module.DbDoc.Services;

namespace Module.DbDoc.Model.ValidationMetadata
{
    public class NumberRangeValidationRule : RangeValidationRule<double>
    {
        public override bool AcceptValidator(IDbModelValidator validator, object value)
        {
            try
            {
                var number = Convert.ToDouble(value);
                return validator.Validate(this, number);
            }
            catch
            {
                return false;
            }
        }
    }
}