using AutoMapper;
using Core.Filters;
using Core.ModelHashing;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;
using Microsoft.Extensions.Logging;
using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Core.Web.ModelBinders
{
    public class IdBinder : IModelBinder
    {
        private readonly Regex regex = new Regex("-([0-9]|[A-F]){16}$");
        private readonly IModelHashingService modelHashingService;

        public IdBinder(ILoggerFactory loggerFactory, IModelHashingService modelHashingService)
        {
            LoggerFactory = loggerFactory;
            this.modelHashingService = modelHashingService;
        }

        public IMapper Mapper { get; }
        public ILoggerFactory LoggerFactory { get; }

        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            if (bindingContext == null)
            {
                throw new ArgumentNullException(nameof(bindingContext));
            }

            Type dtoType = GetDtoType(bindingContext);

            if (dtoType == null)
            {
                var binder = new SimpleTypeModelBinder(bindingContext.ModelType, LoggerFactory);
                return binder.BindModelAsync(bindingContext);
            }

            var modelName = GetModelName(bindingContext);
            var propertyName = GetDtoPropertyName(bindingContext);

            if (propertyName == null)
            {
                var binder = new SimpleTypeModelBinder(bindingContext.ModelType, LoggerFactory);
                return binder.BindModelAsync(bindingContext);
            }

            var valueProviderResult = bindingContext.ValueProvider.GetValue(modelName);
            if (valueProviderResult == ValueProviderResult.None)
            {
                return Task.CompletedTask;
            }

            bindingContext.ModelState.SetModelValue(modelName, valueProviderResult);

            if (regex.IsMatch(valueProviderResult.FirstValue))
            {
                var unhashedProperty = modelHashingService.UnHashProperty(GetPropertyDeclaringType(dtoType, propertyName), GetPropertyOfDeclaringType(propertyName), valueProviderResult.FirstValue);
                if (!unhashedProperty.HasValue)
                {
                    bindingContext.Result = ModelBindingResult.Failed();
                    return Task.CompletedTask;
                }

                bindingContext.Result = ModelBindingResult.Success(unhashedProperty.Value);

                return Task.CompletedTask;
            }
            else
            {
                var propertyType = GetPropertyDeclaringType(dtoType, propertyName)
                    .GetProperties()
                    .SingleOrDefault(x => x.Name.Equals(GetPropertyOfDeclaringType(propertyName), StringComparison.InvariantCultureIgnoreCase))
                    .PropertyType;

                if ((propertyType == typeof(int) || propertyType == typeof(int?)) && int.TryParse(valueProviderResult.FirstValue, out int intValue))
                {
                    bindingContext.Result = ModelBindingResult.Success(intValue);
                    return Task.CompletedTask;
                }

                if ((propertyType == typeof(Guid) || propertyType == typeof(Guid?)) && Guid.TryParse(valueProviderResult.FirstValue, out Guid guidValue))
                {
                    bindingContext.Result = ModelBindingResult.Success(guidValue);
                    return Task.CompletedTask;
                }

                if (propertyType == typeof(string))
                {
                    bindingContext.Result = ModelBindingResult.Success(valueProviderResult.FirstValue);
                    return Task.CompletedTask;
                }
            }
            return Task.CompletedTask;
        }

        private Type GetDtoType(ModelBindingContext bindingContext)
        {
            var dtoType = (bindingContext.ActionContext as ControllerContext)?.ActionDescriptor?.ControllerTypeInfo?.BaseType?.GetGenericArguments().FirstOrDefault();

            if (dtoType == null)
            {
                // EmailSenderController.SendEmail
                dtoType = bindingContext.ModelMetadata.ContainerType;
            }

            return dtoType;
        }

        private string GetModelName(ModelBindingContext bindingContext)
        {
            // The "Name" property of the ModelBinder attribute can be used to specify the
            // route parameter name when the action parameter name is different from the route parameter name.
            if (!string.IsNullOrEmpty(bindingContext.BinderModelName))
            {
                return bindingContext.BinderModelName;
            }
            return bindingContext.ModelName;
        }

        private string GetDtoPropertyName(ModelBindingContext bindingContext)
        {
            // Action parameter
            var propertyName = GetModelName(bindingContext);

            // Filter model
            if (propertyName.EndsWith(".Value"))
            {
                // Car color vs person organization
                if ($"{bindingContext.ValueProvider.GetValue(propertyName.Replace(".Value", ".$type")).FirstValue}Filter".Equals(typeof(ObjectReferenceFilter).Name, StringComparison.InvariantCultureIgnoreCase))
                {
                    var result = bindingContext.ValueProvider.GetValue(propertyName.Replace(".Value", ".PropertyName")).FirstValue.ToLowerInvariant();
                    return result.EndsWith("id") ? result : $"{result}id";
                }
            }

            var type = (bindingContext.ActionContext as ControllerContext)?.ActionDescriptor?.ControllerTypeInfo?.UnderlyingSystemType;
            var attributes = type?.GetCustomAttributes(typeof(BindRouteParameterToDtoPropertyAttribute), true)?.Cast<BindRouteParameterToDtoPropertyAttribute>();
            var attribute = attributes?.FirstOrDefault(attr => propertyName.Equals(attr.RouteProperty, StringComparison.InvariantCultureIgnoreCase));

            // overriden by BindToAttribute
            if (attribute != null)
            {
                return attribute.DtoProperty;
            }

            // parameter named as a dto property 
            return GetDtoType(bindingContext).GetProperties().SingleOrDefault(x => x.Name.Equals(propertyName, StringComparison.InvariantCultureIgnoreCase)) == null ? null : propertyName;
        }

        private Type GetPropertyDeclaringType(Type dtoType, string propertyName)
        {
            var propertyContrainerType = dtoType;
            var propertyNames = propertyName.Split(".");

            PropertyInfo propertyInfo = null;

            foreach (var property in propertyNames)
            {
                propertyInfo = propertyContrainerType.GetProperties().SingleOrDefault(x => x.Name.Equals(property, StringComparison.InvariantCultureIgnoreCase));

                if (propertyInfo == null)
                {
                    throw new ValidationException(new ValidationResult($"{property} not found in {propertyContrainerType.Name}", new[] { propertyName }), null, null);
                }

                propertyContrainerType = propertyInfo?.PropertyType;
            }

            return propertyInfo.ReflectedType;
        }

        private string GetPropertyOfDeclaringType(string propertyName)
        {
            return propertyName.Split(".").Last();
        }
    }
}