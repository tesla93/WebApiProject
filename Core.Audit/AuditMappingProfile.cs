using System;
using System.Collections.Generic;
using System.Linq;
using AutoMapper;
using Microsoft.EntityFrameworkCore;

namespace Core.Audit
{
    public class AuditMappingProfile : Profile
    {
        public AuditMappingProfile()
        {
            CreateMap<ChangeLog, ChangeLogDTO>()
                .ForMember(d => d.State, opt => opt.MapFrom(src => Enum.GetName(typeof(EntityState), src.State)))
                .ForMember(d => d.ChangeLogItemsText, opt => opt.MapFrom(src => GetChangeLogItemsText(src.ChangeLogItems, src.State)));
            CreateMap<ChangeLogItem, ChangeLogItemDTO>();
        }

        private static string GetChangeLogItemsText(IEnumerable<ChangeLogItem> items, EntityState state)
        {
            var textItems = state == EntityState.Modified ?
                items.Select(v => $"\"{v.PropertyName}\" changed from \"{v.OldValue}\" to \"{v.NewValue}\"") :
                state == EntityState.Added ?
                    items.Select(v => $"\"{v.PropertyName}\" assigned a value of \"{v.NewValue}\"") :
                    null;

            return textItems != null ? string.Join("; ", textItems) : null;
        }
    }
}