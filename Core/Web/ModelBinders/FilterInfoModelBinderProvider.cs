using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Core.Filters;

namespace Core.Web.ModelBinders
{
    public class FilterInfoModelBinderProvider : IModelBinderProvider
    {
        public IModelBinder GetBinder(ModelBinderProviderContext context)
        {
            if (context.Metadata.ModelType != typeof(FilterInfoBase)) return null;

            var binders = new Dictionary<string, IModelBinder>();

            var filterTypes = Assembly.GetAssembly(typeof(FilterInfoBase)).GetTypes()
                .Where(a => a.GetTypeInfo().IsSubclassOf(typeof(FilterInfoBase)) && !a.IsAbstract);

            foreach (var type in filterTypes)
            {
                var metadata = context.MetadataProvider.GetMetadataForType(type);
                var binder = context.CreateBinder(metadata);
                binders.Add(type.FullName, binder);
            }

            return new FilterInfoModelBinder(context.MetadataProvider, binders);
        }
    }
}