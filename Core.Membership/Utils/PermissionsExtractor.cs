using System;
using System.Collections.Generic;
using System.Reflection;
using Core.Utils;

namespace Core.Membership.Utils
{
    public static class PermissionsExtractor
    {
        public static IEnumerable<string> GetAllPermissionNamesOfSolution() =>
            ReflectionHelper.GetAllConstantsValuesFromClassesOfSolution<string>("Permissions");

        public static IEnumerable<string> GetAllPermissionNamesOfAssembly(Assembly assembly) =>
            ReflectionHelper.GetAllConstantsValuesFromClassesOfAssembly<string>(assembly, "Permissions");

        public static IEnumerable<string> GetPermissionNamesOfClass(Type classType) =>
            ReflectionHelper.GetAllConstantsValuesOfClass<string>(classType);
    }
}
