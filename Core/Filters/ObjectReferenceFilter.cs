using Core.Filters.Handlers;
using Core.Web.ModelBinders;
using Microsoft.AspNetCore.Mvc;

namespace Core.Filters
{
    [RelatedHandler(typeof(ObjectReferenceFilterHandler))]
    public class ObjectReferenceFilter : FilterInfoBase
    {
        [ModelBinder(BinderType = typeof(IdBinder))]
        public object Value { get; set; }
    }
}