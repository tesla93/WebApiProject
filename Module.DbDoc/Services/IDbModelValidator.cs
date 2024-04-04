using System;
using Module.DbDoc.Model.ValidationMetadata;

namespace Module.DbDoc.Services
{
    public interface IDbModelValidator
    {
        bool Validate(ValidationRule rule, object value);
        bool Validate(RequiredValidationRule rule, object value);
        bool Validate(NumberRangeValidationRule rule, double value);
        bool Validate(DateRangeValidationRule rule, DateTimeOffset value);
        bool Validate(InputFormatValidationRule rule, string value);
        bool Validate(MaxLengthValidationRule rule, object value);
    }
}