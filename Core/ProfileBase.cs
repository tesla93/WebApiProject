using AutoMapper;
using AutoMapper.Configuration;
using Core.Data;
using Core.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using ModuleLinkage;

namespace Core
{
    /* Inherit all automapper profiles from this. This will allow you to avoid errors when you forget to ignore some compound property and waste
     * time trying to understand what is wrong. Compound properties should always be ignored when mapping dto to entities and preparing database
     * save. Those properties should be saved explicitly - as separate entity or collection.
     *
     * In real projects the amount of such properties can be tens and hundreds (see Charles Trent). And maintaining this manually is very error
     * prone and time consuming. Also bear in mind that automapper mappings is one of the most volatile areas of code.
     *
     * Note1: The rules below get most likely applied even if you don't inherit from the ProfileBase. See Startup.cs.
     */
    public class ProfileBase : Profile
    {
        public ProfileBase()
        {
            ApplyRules(this);
        }

        public static void ApplyRules(IProfileExpression cfg)
        {
            /* Here we Ignore all compond child property mappings in Dto -> Entity direction.
             * It means that they should be saved to database explicitly. As it has always been in T2 projects.
             */
            cfg.ForAllMaps((typeMap, me) =>
            {
                MappingExpression m = me as MappingExpression;
                if (!typeof(IBaseEntity).IsAssignableFrom(m.DestinationType))
                {
                    return;
                }
                //Log.Warning($"Mapping type: {m.DestinationType.FullName}");

                //Exclude potentially too tricky cases in BBWM modules.
                var assembly = m.DestinationType.Assembly;
                if (ModuleLinker.IsModuleAssembly(assembly) && !ModuleLinker.IsDemoModuleAssembly(assembly))
                {
                    //Log.Warning($"Skipping assembly from autoignore: {assName}");
                    return;
                }

                me.ForAllMembers(i =>
                {
                    var pi = i.DestinationMember as System.Reflection.PropertyInfo;
                    if (pi.PropertyType == typeof(string))
                        return;

                    if (typeof(IBaseEntity).IsAssignableFrom(pi.PropertyType))
                    {
                        if (!Attribute.IsDefined(pi, typeof(DoNotAutoignoreAttribute)))
                        {
                            i.Ignore();
                        }
                        /* If we have child entity then it is better to add correspoding Id at once.
                         * This is not very important rule. Feel free to disable it if it doesn't suit your project.
                         */
                        if (!Attribute.IsDefined(pi, typeof(DoNotRequireChildIdAttribute)))
                        {
                            var childIdName = pi.Name + "Id";
                            var childIdProperty = pi.DeclaringType.GetProperty(childIdName);
                            if (childIdProperty == null)
                            {
                                throw new Exception($"Error while mapping {pi.DeclaringType.Name}::{pi.Name}. If you have child Entity with name like 'Child' then please add corresponding Id property with name like 'ChildId'");
                            }
                        }
                    }
                    if (typeof(IEnumerable).IsAssignableFrom(pi.PropertyType))
                    {
                        i.Ignore();
                    }
                });
            });

            // Detect duplicate mappings. They can be quite nasty if they are located in different files and are actully different.
            {
                var alreadyMapped = new List<Tuple<Type, Type>>();
                cfg.ForAllMaps((typeMap, me) =>
                {
                    MappingExpression m = me as MappingExpression;
                    var from = m.SourceType;
                    var to = m.DestinationType;
                    var mapping = alreadyMapped.SingleOrDefault(i => i.Item1 == from && i.Item2 == to);
                    if (mapping != null)
                    {
                        throw new Exception($"Error while mapping {from.Name} to {to.Name}. Mapping already exists. Please remove duplicate to avoid nondeterministic behaviour.");
                    }
                    alreadyMapped.Add(new Tuple<Type, Type>(from, to));
                });
            }
        }

        // Putting automapper mapping side by side with data definitions we significantly decrease code scuttering and increase cohesion.
        // This is good.
        public static void CollectAndRegisterMappings(IMapperConfigurationExpression cfg)
        {
            ApplyRules(cfg);

            var assemblies = ModuleLinker.GetBbAssemblies();
            foreach (var assembly in assemblies)
            {
                var types = Common.GetClassesInheritedFrom<IBaseEntity>(assembly);
                foreach (Type type in types)
                {
                    if (type.IsGenericType) continue;
                    MethodInfo rsm = type.GetMethod("RegisterMap", BindingFlags.Static | BindingFlags.Public);
                    if (rsm == null) continue;

                    //_logger.LogDebug("Auto-Registering type: "+ type.Name);

                    rsm.Invoke(null, new object[] { cfg });
                }
            }
        }

    }

    [AttributeUsage(AttributeTargets.Property)]
    public class DoNotRequireChildIdAttribute : Attribute
    {

    }

    // By default compound properties are ignored (excluded from mapping) automatically by ProfileBase. In some cases for you own risk you
    // can use this attribute to skip auto-ignore option and handle it manually. 
    [AttributeUsage(AttributeTargets.Property)]
    public class DoNotAutoignoreAttribute : Attribute
    {

    }
}
