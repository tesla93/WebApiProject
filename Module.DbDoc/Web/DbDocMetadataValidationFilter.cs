using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Module.DbDoc.Services;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;

namespace Module.DbDoc.Web
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class DbDocMetadataValidationFilterAttribute : Attribute, IFilterFactory
    {
        public IFilterMetadata CreateInstance(IServiceProvider serviceProvider)
        {
            var metadataService =  serviceProvider.GetService<IDbMetadataService>();
            var modelValidator =  serviceProvider.GetService<IDbModelValidator>();

            return new DbDocMetadataValidationFilter(metadataService, modelValidator);
        }

        public bool IsReusable => false;
    }

    public class DbDocMetadataValidationFilter : IAsyncActionFilter
    {
        private readonly IDbMetadataService _metadataService;
        private readonly IDbModelValidator _modelValidator;

        public DbDocMetadataValidationFilter(IDbMetadataService metadataService, IDbModelValidator modelValidator)
        {
            _metadataService = metadataService;
            _modelValidator = modelValidator;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var method = context.HttpContext.Request.Method;
            if (method == "PUT" || method == "POST")
            {
                var value = context.ActionArguments.FirstOrDefault().Value;
                if (value != null)
                {
                    var valueType = value.GetType();
                    var fields = _metadataService.GetMetadata(valueType);
                    if (fields != null)
                    {
                        foreach (var field in fields)
                        {
                            var property = valueType.GetProperty(field.FieldName, BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.Public);
                            if (property == null) continue;

                            var propertyValue = property.GetValue(value);
                            foreach (var rule in field.ValidationRules)
                            {
                                if (!_modelValidator.Validate(rule, propertyValue))
                                {
                                    context.ModelState.AddModelError(property.Name, rule.ErrorMessage);
                                }
                            }
                        }
                    }
                }
            }

            await next();
        }
    }
}