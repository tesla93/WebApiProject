using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Module.DbDoc.Enums
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum DbAnonRule
    {
        [EnumMember(Value = "ROMAN_NAME")] RomanName,
        [EnumMember(Value = "ELVEN_NAME")] ElvenName,
        [EnumMember(Value = "DATE")] Date,
        [EnumMember(Value = "EMAIL_ADDRESS")] EmailAddress,
        [EnumMember(Value = "RANDOM_CHARACTERS")] RandomCharacters,
        [EnumMember(Value = "UUID")] Uuid,
        [EnumMember(Value = "PREFETCH_CHARACTERS")]PrefetchCharacters,
        [EnumMember(Value = "RANDOM_DIGITS")] RandomDigits,
        [EnumMember(Value = "STRING")] String
    }
}