using System;
using System.Collections.Generic;
using Autofac;
using Newtonsoft.Json;
using AutofacExtensions;

namespace Module.SystemSettings
{
    public static class ServiceCollectionExtensions
    {
        internal static Dictionary<Type, string> Sections = new Dictionary<Type, string>();

        internal static JsonSerializerSettings JsonSerializerSettings;


        public static void RegisterSettingsService(this ContainerBuilder builder)
        {
            RegisterSection<ProjectSettings>("ProjectSettings");
            RegisterSection<PwaSettings>("PwaSettings");
            builder.RegisterService<ISettingsService, SettingsService>();
        }

        public static void RegisterSerializerSettings(this JsonSerializerSettings settings) => JsonSerializerSettings = settings;

        public static void RegisterSection<T>(string sectionName) => Sections.Add(typeof(T), sectionName);
    }
}