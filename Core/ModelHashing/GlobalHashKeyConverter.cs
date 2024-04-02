using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Core.ModelHashing
{
    public class GlobalHashKeyJsonConverter : JsonConverter
    {
        private readonly JsonSerializer _serializerWithoutCustomConverter;
        private readonly IModelHashingService modelHashingService;

        public GlobalHashKeyJsonConverter(JsonSerializer serializerWithoutCustomConverter, IModelHashingService modelHashingService)
        {
            _serializerWithoutCustomConverter = serializerWithoutCustomConverter;
            this.modelHashingService = modelHashingService;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            // To avoid => Self referencing loop detected for property.
            _serializerWithoutCustomConverter.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;

            var token = (JObject)JToken.FromObject(value, _serializerWithoutCustomConverter);

            TraverseJsonObject(token, value.GetType(), HashJsonObjectKeys);

            token.WriteTo(writer);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var token = (JObject)JToken.ReadFrom(reader);

            TraverseJsonObject(token, objectType, ParseJsonObjectKeys);
            var result = token.ToObject(objectType, _serializerWithoutCustomConverter);
            return result;
        }

        public override bool CanConvert(Type objectType)
        {
            if (modelHashingService.GetMaps(objectType) == null)
            {
                return false;
            }
            return objectType.IsClass && (modelHashingService.GetMaps(objectType)?.Any() ?? false);
        }

        private static void TraverseJsonObject(JObject token, Type objectType, Action<JObject, Type> action)
        {
            if (token == null) return;

            foreach (var property in token.Properties().Where(p => p.Value != null).ToArray())
            {
                var modelProperty = objectType.GetProperty(ToUpper(property.Name));
                if (modelProperty == null) continue;
                switch (property.Value.Type)
                {
                    case JTokenType.Array:
                        var objects = GetCollectionItems(property, modelProperty);
                        foreach (var (key, value) in objects)
                        {
                            TraverseJsonObject(key, value, action);
                        }

                        break;
                    case JTokenType.Object:
                        TraverseJsonObject((JObject)property.Value, modelProperty.PropertyType, action);
                        break;
                }
            }

            action(token, objectType);
        }

        private static IEnumerable<KeyValuePair<JObject, Type>> GetCollectionItems(JProperty property, PropertyInfo modelProperty)
        {
            if (!modelProperty.PropertyType.GetInterfaces().Contains(typeof(IEnumerable)) || !modelProperty.PropertyType.IsGenericType) yield break;

            var childType = modelProperty.PropertyType.GetGenericArguments().FirstOrDefault();
            if (childType == null || childType.IsPrimitive) yield break;

            foreach (var child in property.Value.Children().Where(c => c.Type == JTokenType.Object))
            {
                if (child is JObject childObject)
                {
                    yield return new KeyValuePair<JObject, Type>(childObject, childType);
                }
            }
        }

        private void HashJsonObjectKeys(JObject token, Type objectType)
        {
            var maps = modelHashingService.GetMaps(objectType);
            foreach (var map in maps)
            {
                var jProp = token.Property(map.Property);
                if (jProp == null) continue;

                var val = (int?)jProp.Value;
                if (!val.HasValue) continue;

                var strVal = HashingHelper.AppendHashToKey(val.Value, map.Salt);
                jProp.Value = strVal;
                token.Add($"{map.Property}_original", val.Value);
            }
        }

        private void ParseJsonObjectKeys(JObject token, Type objectType)
        {
            var maps = modelHashingService.GetMaps(objectType);
            foreach (var map in maps)
            {
                var val = token.Property(map.Property);
                var modelProperty = objectType.GetProperty(ToUpper(map.Property));
                if (modelProperty == null) continue;

                var defaultValue = modelProperty.PropertyType == typeof(int) ? (int?)0 : null;
                if (val != null)
                {
                    var strVal = (string)val.Value;
                    if (!string.IsNullOrEmpty(strVal))
                    {
                        var intVal = HashingHelper.GetKeyFromHashString((string)val.Value, map.Salt);
                        if (intVal.HasValue)
                        {
                            val.Value = intVal;
                            continue;
                        }
                    }

                    val.Value = defaultValue;
                }
                else
                {
                    token.Add(map.Property, defaultValue);
                }
            }
        }

        private static string ToUpper(string name)
        {
            return char.ToUpperInvariant(name[0]) + name.Substring(1);
        }
    }
}