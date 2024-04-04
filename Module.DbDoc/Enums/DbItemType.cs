using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Module.DbDoc.Enums
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum DbItemType
    {
        [EnumMember(Value = "folder")] Folder,
        [EnumMember(Value = "table")] Table,
        [EnumMember(Value = "column")] Column,
    }
}