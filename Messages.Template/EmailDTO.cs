using Core.Web.ModelBinders;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace Messages.Templates
{
    public class EmailDTO
    {
        [Required]
        public string To { get; set; }

        [Required]
        [ModelBinder(BinderType = typeof(IdBinder))]
        public int EmailTemplateId { get; set; }
    }
}
