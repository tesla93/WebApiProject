using System.Collections.Generic;

namespace Core.Extensions
{
    public static class EnumerableExtensions
    {
        public static List<T> List<T>(this object obj, params T[] items)
        {
            return new List<T>(items);
        }

        public static IEnumerable<T> Enumerable<T>(this object obj, params T[] items)
        {
            return items;
        }
    }
}