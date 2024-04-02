using System;
using System.Collections.Generic;
using System.Reflection;
using Core.Utils;

namespace Core.Membership.Utils
{
    public static class GroupsExtractor
    {
        public static IEnumerable<string> GetAllGroupNamesOfSolution() =>
            ReflectionHelper.GetAllConstantsValuesFromClassesOfSolution<string>("Groups");

        public static IEnumerable<string> GetAllGroupNamesOfAssembly(Assembly assembly) =>
            ReflectionHelper.GetAllConstantsValuesFromClassesOfAssembly<string>(assembly, "Groups");

        public static IEnumerable<string> GetGroupNamesOfClass(Type classType) =>
            ReflectionHelper.GetAllConstantsValuesOfClass<string>(classType);
    }
}