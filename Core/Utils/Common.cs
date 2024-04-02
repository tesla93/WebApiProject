using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Core.Utils
{
    public class Common
    {
        public static IEnumerable<Type> GetTypesWithAttribute<TAttr>(Assembly assembly)
        {
            foreach (Type type in assembly.GetTypes())
            {
                if (type.GetCustomAttributes(typeof(TAttr), true).Length > 0)
                {
                    yield return type;
                }
            }
        }
        public static IEnumerable<Type> GetClassesInheritedFrom<TAttr>(Assembly assembly)
        {
            foreach (Type type in assembly.GetTypes())
            {
                if (typeof(TAttr).IsAssignableFrom(type))
                {
                    yield return type;
                }
            }
        }
    }
}
