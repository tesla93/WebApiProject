using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Core.Filters;

namespace Core.Web.ModelBinders
{
    public class FilterInfoModelBinder : IModelBinder
    {
        private readonly IModelMetadataProvider _metadataProvider;
        private readonly Dictionary<string, IModelBinder> _binders;

        public FilterInfoModelBinder(IModelMetadataProvider metadataProvider, Dictionary<string, IModelBinder> binders)
        {
            _metadataProvider = metadataProvider;
            _binders = binders;
        }

        public async Task BindModelAsync(ModelBindingContext bindingContext)
        {
            if (bindingContext == null)
            {
                throw new ArgumentNullException(nameof(bindingContext));
            }

            if (bindingContext.ModelType == typeof(FilterInfoBase))
            {
                var type = bindingContext.ValueProvider.GetValue($"{bindingContext.ModelName.ToLowerInvariant()}.$type");
                if (type == ValueProviderResult.None)
                {
                    bindingContext.Result = ModelBindingResult.Failed();
                    return;
                }

                var filterType = Assembly.GetAssembly(typeof(FilterInfoBase)).GetTypes()
                    .FirstOrDefault(a => a.GetTypeInfo().IsSubclassOf(typeof(FilterInfoBase)) && !a.IsAbstract && string.Equals(a.Name, $"{type.FirstValue}Filter", StringComparison.InvariantCultureIgnoreCase));

                if (filterType == null)
                {
                    bindingContext.Result = ModelBindingResult.Failed();
                    return;
                }

                var binder = _binders[filterType.FullName];
                var metadata = _metadataProvider.GetMetadataForType(filterType);

                ModelBindingResult result;
                using (bindingContext.EnterNestedScope(metadata, bindingContext.FieldName, bindingContext.ModelName, null))
                {
                    await binder.BindModelAsync(bindingContext);
                    result = bindingContext.Result;
                }

                bindingContext.Result = result;
            }
        }
    }
}