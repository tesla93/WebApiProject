using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace Core.Extensions
{
    public static class EnumExtensions
    {
        public static Dictionary<string, object> GetEnumNamesValues(this Type enumType)
        {
            if (!enumType.IsEnum) throw new ArgumentException("The specified type is not an Enum.");

            var names = Enum.GetNames(enumType);
            var values = Enum.GetValues(enumType);

            var result = new Dictionary<string, object>();
            for (var index = 0; index < names.Length; index++)
            {
                var memberInfo = enumType.GetMember(names[index])[0];
                var enumMemberAttribute =
                    memberInfo.GetCustomAttributes(false).OfType<EnumMemberAttribute>().FirstOrDefault();

                result.Add(enumMemberAttribute != null ? enumMemberAttribute.Value : names[index], values.GetValue(index));
            }

            return result;
        }
    }
}
