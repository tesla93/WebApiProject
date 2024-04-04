using Module.DbDoc.Enums;
using System;

namespace Module.DbDoc
{
    public static class TypeExtensions
    {
        public static ClrTypeGroup GetTypeGroup(this Type type)
        {
            return type switch
            {
                Type t when t == typeof(string) => ClrTypeGroup.String,
                Type t when IsNumeric(t) => ClrTypeGroup.Numeric,
                Type t when IsDate(t) => ClrTypeGroup.Date,
                _ => ClrTypeGroup.Other,
            };
        }

        private static bool IsDate(Type t)
        {
            return t == typeof(DateTimeOffset) || t == typeof(DateTimeOffset?) ||
                    t == typeof(DateTime) || t == typeof(DateTime?) ||
                    t == typeof(TimeSpan) || t == typeof(TimeSpan?);
        }

        private static bool IsNumeric(Type t)
        {
            return t == typeof(short) || t == typeof(short?) ||
                    t == typeof(int) || t == typeof(int?) ||
                    t == typeof(long) || t == typeof(long?) ||
                    t == typeof(decimal) || t == typeof(decimal?) ||
                    t == typeof(float) || t == typeof(float?) ||
                    t == typeof(double) || t == typeof(double?);
        }
    }
}