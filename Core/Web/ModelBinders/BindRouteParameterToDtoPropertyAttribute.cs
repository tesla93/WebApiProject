using System;

namespace BBWM.Core.Web.ModelBinders
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class BindRouteParameterToDtoPropertyAttribute : Attribute
    {
        public BindRouteParameterToDtoPropertyAttribute(string routeProperty, string dtoProperty)
        {
            RouteProperty = routeProperty;
            DtoProperty = dtoProperty;
        }

        public string RouteProperty { get; }
        public string DtoProperty { get; }
    }
}