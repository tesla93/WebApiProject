using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace ModuleLinkage
{
    public class ModuleLinker
    {
        public static readonly List<KeyValuePair<Type, string>> LinkedClasses =
            new List<KeyValuePair<Type, string>>();
        public static readonly List<Exception> InvokeExceptions = new List<Exception>();
        public static readonly BlockingCollection<Exception> CommonExceptions = new BlockingCollection<Exception>();
        public static void AddInvokeException(Exception ex) => InvokeExceptions.Add(ex);
        public static void AddCommonException(Exception ex) => CommonExceptions.Add(ex);

        private const string moduleAssemblyNamePrefix = "Module.";
        private const string coreAssemblyNamePrefix = "Module.Core";
        private const string demoAssemblyName = "Module.Demo";
        private const string templateAssemblyNamePrefix = "Project.";

        public static bool IsDemoModuleAssembly(Assembly assembly) => assembly.FullName.StartsWith(demoAssemblyName);
        public static bool IsModuleAssembly(Assembly assembly) => assembly.FullName.StartsWith(moduleAssemblyNamePrefix);
        public static bool IsProjectAssembly(Assembly assembly) => assembly.FullName.StartsWith(templateAssemblyNamePrefix);

        private static bool assembliesPreloaded = false;

        public static IEnumerable<Assembly> GetBbAssemblies() =>
                AppDomain.CurrentDomain.GetAssemblies()
                    .Where(o => o.FullName.StartsWith(moduleAssemblyNamePrefix) || o.FullName.StartsWith(templateAssemblyNamePrefix));

        private static IEnumerable<Assembly> ModulesAssemblies =>
                AppDomain.CurrentDomain.GetAssemblies().Where(o => o.FullName.StartsWith(moduleAssemblyNamePrefix));
        public static List<TInterface> GetInstances<TInterface>()
        {
            if (!assembliesPreloaded)
            {
                PreloadAssemblies();   
                assembliesPreloaded = true;
            }

            var linkers = new List<TInterface>();
            var linkerType = typeof(TInterface);

            var sortedAssemblies = ModulesAssemblies.OrderBy(o => o.FullName, new AssembliesLinkageOrderComparer());

            foreach (var assembly in sortedAssemblies)
            {
                var linkerClasses = assembly.GetTypes().Where(p => p.IsClass && linkerType.IsAssignableFrom(p));

                foreach (var linkerClass in linkerClasses)
                {
                    if (linkerClass != null)
                    {
                        var linkerInstance = (TInterface)assembly.CreateInstance(linkerClass.FullName);
                        linkers.Add(linkerInstance);

                        LinkedClasses.Add(new KeyValuePair<Type, string>(linkerClass, Environment.StackTrace));
                    }
                }
            }

            return linkers;
        }

        // This is a basic solution on how to force the core modules to be initialized first and the demo module last.
        // It's done because non-core modules are supposed to use the core modules' functionality and the demo module
        // in theory may use all other modules for demonstation purposes. 
        // Ideally we should use a smarter approach where the modules are ordered according to their tree of references.
        // So if module B refers to module A then the module A is initialized first.
        private class AssembliesLinkageOrderComparer : IComparer<string>
        {
            public int Compare(string x, string y)
            {
                if (x.StartsWith(coreAssemblyNamePrefix) && !y.StartsWith(coreAssemblyNamePrefix))
                    return -1;
                if (!x.StartsWith(coreAssemblyNamePrefix) && y.StartsWith(coreAssemblyNamePrefix))
                    return 1;
                if (x.StartsWith(demoAssemblyName) && !y.StartsWith(demoAssemblyName))
                    return 1;
                if (!x.StartsWith(demoAssemblyName) && y.StartsWith(demoAssemblyName))
                    return -1;

                return string.CompareOrdinal(x, y);
            }
        }

        private static void PreloadAssemblies()
        {
            foreach (var assembly in GetBbAssemblies())
                LoadReferencedAssembly(assembly);
        } 

        private static void LoadReferencedAssembly(Assembly assembly)
        {
            var refAssembliesNames = assembly.GetReferencedAssemblies().Where(o => o.FullName.StartsWith(moduleAssemblyNamePrefix));

            foreach (AssemblyName name in refAssembliesNames)
            {
                var childAssembly = GetBbAssemblies().FirstOrDefault(a => a.FullName == name.FullName);
                if (childAssembly == null)
                    childAssembly = Assembly.Load(name);

                LoadReferencedAssembly(childAssembly);
            }
        }
    }
}
