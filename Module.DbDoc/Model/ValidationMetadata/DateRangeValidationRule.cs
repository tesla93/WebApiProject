using System;
using Module.DbDoc.Services;

namespace Module.DbDoc.Model.ValidationMetadata
{
    public class DateRangeValidationRule : RangeValidationRule<DateTimeOffset>
    {
        public override bool AcceptValidator(IDbModelValidator validator, object value)
        {
            try
            {
                var date = Convert.ToDateTime(value);
                return validator.Validate(this, date);
            }
            catch
            {
                return false;
            }
        }
    }
}