using Microsoft.AspNetCore.Mvc;
using System;

namespace BBWM.Core.Web.ModelBinders
{
    [AttributeUsage(AttributeTargets.Parameter| AttributeTargets.Property)]
    public class IdBinderAttribute : ModelBinderAttribute
    {
        public IdBinderAttribute() : base(typeof(IdBinder))
        {
        }
    }
}