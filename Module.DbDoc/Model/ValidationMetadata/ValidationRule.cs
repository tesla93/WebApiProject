using Module.DbDoc.Services;
using Module.DbDoc.Web;
using Newtonsoft.Json;

namespace Module.DbDoc.Model.ValidationMetadata
{
    [JsonConverter(typeof(ValidationRuleConverter))]
    public abstract class ValidationRule
    {
        public bool IsSystem { get; set; }
        public string ErrorMessage { get; set; }
        public abstract bool AcceptValidator(IDbModelValidator validator, object value);
    }
}