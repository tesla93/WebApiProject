using System;
using System.Linq;
using AutoMapper;
using Core.Data;
using Core.Services;
using Microsoft.AspNetCore.Identity;

namespace Module.Metadata
{
    public class MetadataService<TMetadata, TUser> : IMetadataService
        where TMetadata : MetadataModel<TUser>
        where TUser : IdentityUser
    {
        private readonly IDbContext _context;
        private readonly IMapper _mapper;
        private readonly ICurrentUserService _currentUserService;
        private readonly UserManager<TUser> _userManager;

        public MetadataService(IDbContext context, IMapper mapper, ICurrentUserService currentUserService, UserManager<TUser> userManager)
        {
            _context = context;
            _mapper = mapper;
            _currentUserService = currentUserService;
            _userManager = userManager;
        }

        public MetadataDTO GetByKey(string key)
        {
            var data = _context.Set<TMetadata>().FirstOrDefault(x => x.Key == key);

            if (data == null) return null;

            var metadata = _mapper.Map<MetadataDTO>(data);

            if (!string.IsNullOrEmpty(data.LockedByUserId))
            {
                var user = _userManager.FindByIdAsync(data.LockedByUserId).Result;
                if (user != null)
                    metadata.LockedByUserFullName = user.Email;
            }

            return metadata;
        }

        public MetadataDTO Save(MetadataDTO dto)
        {
            TMetadata entity;
            if (dto.Id == 0)
            {
                entity = _mapper.Map<TMetadata>(dto);
                entity.CreatedOn = DateTimeOffset.Now;
                entity.LastUpdated = DateTimeOffset.Now;
                _context.Set<TMetadata>().Add(entity);
            }
            else
            {
                entity = _context.Set<TMetadata>().Find(dto.Id);
                _mapper.Map(dto, entity);
                entity.LastUpdated = DateTimeOffset.Now;
                _context.Set<TMetadata>().Update(entity);
            }

            _context.SaveChanges();
            return _mapper.Map<MetadataDTO>(entity);
        }

        public MetadataDTO Save(string key, string value)
        {
            var data = _context.Set<TMetadata>().FirstOrDefault(x => x.Key == key);
            var dto = data == null ?
                new MetadataDTO { Id = 0, Key = key } :
                _mapper.Map<MetadataDTO>(data);
            dto.Value = value;
            return Save(dto);
        }

        public void LockUnlockRecord(string key, bool isLocked)
        {
            var userId = _currentUserService.GetCurrentUserId();
            var item = GetByKey(key);
            if (item != null && ((item.IsLocked && item.LockedByUserId == userId) || !item.IsLocked))
            {
                item.IsLocked = isLocked;
                item.LockedByUserId = isLocked ? userId : null;
                Save(item);
            }
        }
    }
}