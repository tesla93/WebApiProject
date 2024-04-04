using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Module.DbDoc.Enums
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum CombinedValidationType
    {
        [EnumMember(Value = "and")] And,
        [EnumMember(Value = "or")] Or
    }
}