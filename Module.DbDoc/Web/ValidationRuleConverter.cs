using System;
using System.Collections.Generic;
using System.Linq;
using Module.DbDoc.Model.ValidationMetadata;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Module.DbDoc.Web
{
    public class ValidationRuleConverter : JsonConverter
    {
        public static readonly List<(string Alias, Type ValidatorType)> TypesMap = new List<(string, Type)>
        {
            ("required", typeof(RequiredValidationRule)),
            ("number_range", typeof(NumberRangeValidationRule)),
            ("date_range", typeof(DateRangeValidationRule)),
            ("input_format", typeof(InputFormatValidationRule)),
            ("max_length", typeof(MaxLengthValidationRule))
        };

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var valueType = value.GetType();

            var mapItem = TypesMap.FirstOrDefault(x => x.ValidatorType == valueType);
            if (mapItem.Equals(default((string, Type))))
            {
                throw new Exception("Invalid validation rule's type exception");
            }

            writer.WriteStartObject();

            writer.WritePropertyName("$type");
            serializer.Serialize(writer, mapItem.Alias);

            var props = valueType.GetProperties();
            foreach (var prop in props)
            {
                var val = prop.GetValue(value);
                writer.WritePropertyName(char.ToLowerInvariant(prop.Name[0]) + prop.Name.Substring(1));
                serializer.Serialize(writer, val);
            }

            writer.WriteEndObject();
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var jsonObject = JObject.Load(reader);
            var alias = jsonObject["$type"].Value<string>();
            var mapItem = TypesMap.FirstOrDefault(x => string.Equals(x.Alias, alias, StringComparison.InvariantCultureIgnoreCase));

            if (mapItem.Equals(default((string, Type))))
            {
                throw new Exception("Invalid validation rule's type exception");
            }

            var rule = Activator.CreateInstance(mapItem.ValidatorType);

            serializer.Populate(jsonObject.CreateReader(), rule);
            return rule;
        }

        public override bool CanConvert(Type objectType)
            => objectType == typeof(ValidationRule) || !objectType.IsAbstract && objectType.IsSubclassOf(typeof(ValidationRule));
    }
}