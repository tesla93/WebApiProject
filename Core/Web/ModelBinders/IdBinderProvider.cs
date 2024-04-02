using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;
using System;
using System.Linq;

namespace Core.Web.ModelBinders
{
    public class IdBinderProvider : IModelBinderProvider
    {
        public IModelBinder GetBinder(ModelBinderProviderContext context)
        {
            if (new[] { typeof(int), typeof(Guid), typeof(string) }.Contains(context.Metadata.ModelType))
            {
                return new BinderTypeModelBinder(typeof(IdBinder));
            }

            return null;
        }
    }
}