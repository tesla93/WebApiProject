using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Module.DbDoc.Enums
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum InputFormatType
    {
        [EnumMember(Value = "phone")] Phone,
        [EnumMember(Value = "email")] Email,
        [EnumMember(Value = "url")] Url,
        [EnumMember(Value = "regex")] Regex,
    }
}