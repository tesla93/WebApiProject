using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Runtime.Serialization;

namespace Module.DbDoc.Enums
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum ClrTypeGroup
    {
        [EnumMember(Value = "numeric")] Numeric,
        [EnumMember(Value = "string")] String,
        [EnumMember(Value = "date")] Date,
        [EnumMember(Value = "other")] Other
    }
}