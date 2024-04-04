using Module.DbDoc.Services;

namespace Module.DbDoc.Model.ValidationMetadata
{
    public class RequiredValidationRule : ValidationRule
    {
        public bool Required { get; set; } = true;

        public override bool AcceptValidator(IDbModelValidator validator, object value)
        {
            return validator.Validate(this, value);
        }
    }
}