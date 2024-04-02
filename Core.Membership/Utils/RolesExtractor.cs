using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Core.Utils;

namespace Core.Membership.Utils
{
    public static class RolesExtractor
    {
        public static IEnumerable<string> GetAllRolesNamesOfSolution() =>
            ReflectionHelper.GetAllConstantsValuesFromClassesOfSolution<string>("Roles");

        public static IEnumerable<string> GetAllRolesNamesOfAssembly(Assembly assembly) =>
            ReflectionHelper.GetAllConstantsValuesFromClassesOfAssembly<string>(assembly, "Roles");

        public static IEnumerable<string> GetRolesNamesOfClass(Type classType) =>
            ReflectionHelper.GetAllConstantsValuesOfClass<string>(classType);
    }
}
