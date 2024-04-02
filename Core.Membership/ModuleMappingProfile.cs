using System.Collections.Generic;
using AutoMapper;
using Core.Membership.DTO;
using Core.Membership.Model;
using System.Linq;
using Microsoft.AspNetCore.Identity;
using Newtonsoft.Json;

namespace Core.Membership
{
    public class ModuleMappingProfile : Profile
    {
        public ModuleMappingProfile()
        {
            #region Identity
            CreateMap<User, UserDTO>()
                .ForMember(d => d.Claims, m => m.MapFrom<UserClaimsResolver>())
                .ForMember(d => d.Roles, m => m.MapFrom(s => s.UserRoles))
                .ForMember(d => d.Permissions, m => m.MapFrom(s => s.UserPermissions))
                .ForMember(d => d.Groups, m => m.MapFrom(s => s.UserGroups))
                .ForMember(d => d.IsSuperAdmin, m => m.MapFrom(s => s.UserRoles.Any(x => x.Role.Name == Roles.SuperAdminRole)))
                .ForMember(d => d.IsSystemAdmin, m => m.MapFrom(s => s.UserRoles.Any(x => x.Role.Name == Roles.SystemAdminRole)))
                .ForMember(d => d.IsRealUserEditor, m => m.MapFrom(s => s.UserRoles.Any(x => x.Role.Name == Roles.RealUserEditor)))
                .ReverseMap()
                .ForMember(d => d.NormalizedEmail, m => m.MapFrom(s => s.Email.ToUpperInvariant()))
                .ForMember(d => d.NormalizedUserName, m => m.MapFrom(s => s.UserName.ToUpperInvariant()))
                .ForMember(d => d.Company, m => m.Ignore())
                .ForMember(d => d.UserRoles, m => m.Ignore())
                .ForMember(d => d.UserPermissions, m => m.Ignore())
                .ForMember(d => d.UserGroups, m => m.Ignore());

            CreateMap<UserRole, RoleDTO>()
                .ForMember(d => d.Id, m => m.MapFrom(s => s.Role.Id))
                .ForMember(d => d.Name, m => m.MapFrom(s => s.Role.Name))
                .ForMember(d => d.AuthenticatorRequired, m => m.MapFrom(s => s.Role.AuthenticatorRequired))
                .ForMember(d => d.CheckIp, m => m.MapFrom(s => s.Role.CheckIp));

            CreateMap<UserPermission, PermissionDTO>()
                .ForMember(d => d.Id, m => m.MapFrom(s => s.Permission.Id))
                .ForMember(d => d.Name, m => m.MapFrom(s => s.Permission.Name));

            CreateMap<Role, RoleDTO>()
                .ForMember(d => d.Permissions, m => m.MapFrom(s => s.RolePermissions))
                .ReverseMap()
                .ForMember(d => d.NormalizedName, m => m.MapFrom(s => s.Name.ToUpperInvariant()))
                .ForMember(d => d.UserRoles, m => m.Ignore())
                .ForMember(d => d.RolePermissions, m => m.Ignore());

            CreateMap<RolePermission, PermissionDTO>()
                .ForMember(d => d.Id, m => m.MapFrom(s => s.PermissionId))
                .ForMember(d => d.Name, m => m.MapFrom(s => s.Permission.Name));

            CreateMap<Permission, PermissionDTO>().ReverseMap();

            CreateMap<Group, GroupDTO>().ReverseMap();

            CreateMap<UserGroup, GroupDTO>()
                .ForMember(d => d.Id, m => m.MapFrom(s => s.Group.Id))
                .ForMember(d => d.Name, m => m.MapFrom(s => s.Group.Name))
                .ForMember(d => d.CompanyId, m => m.MapFrom(s => s.Group.CompanyId))
                .ForMember(d => d.Company, m => m.MapFrom(s => s.Group.Company));

            CreateMap<UserPasswordFailedHistory, UserPasswordFailedHistoryDTO>().ReverseMap();

            CreateMap<AuthenticationRequest, U2FRegistrationRequestDTO>();
            #endregion

            // Address
            CreateMap<Address, AddressDTO>().ReverseMap();

            // Allowed IP
            CreateMap<AllowedIp, AllowedIpDTO>()
                .ForMember(x => x.Users, y => y.MapFrom(z => z.AllowedIpUsers.Select(a => a.User)))
                .ForMember(x => x.Roles, y => y.MapFrom(z => z.AllowedIpRoles.Select(a => a.Role)))
                .ReverseMap()
                .ForMember(x => x.AllowedIpRoles, y => y.Ignore())
                .ForMember(x => x.AllowedIpUsers, y => y.Ignore());

            // Branding
            CreateMap<Branding, BrandingDTO>()
                .ReverseMap()
                .ForMember(x => x.Company, y => y.Ignore())
                .ForMember(x => x.LogoImage, y => y.Ignore())
                .ForMember(x => x.LogoIcon, y => y.Ignore());

            // Company
            CreateMap<Company, CompanyDTO>().ReverseMap();

            // ActivationToken
            CreateMap<ActivationToken, ActivationTokenDTO>().ReverseMap();

            // Audit Login
            CreateMap<LoginAudit, LoginAuditDTO>().ReverseMap();
        }
    }

    public class UserClaimsResolver : IValueResolver<User, UserDTO, Dictionary<string, string>>
    {
        private readonly UserManager<User> _userManager;

        public UserClaimsResolver() { }
        public UserClaimsResolver(UserManager<User> userManager) => _userManager = userManager;

        public Dictionary<string, string> Resolve(User source, UserDTO destination, Dictionary<string, string> destMember, ResolutionContext context)
        {
            if (_userManager == null) return new Dictionary<string, string>();

            var userClaimsFromDb = (_userManager.GetClaimsAsync(source)).Result;
            return userClaimsFromDb
                .GroupBy(a => a.Type)
                .ToDictionary(k => k.Key, v =>
                {
                    var list = v.Select(a => a.Value).ToList();

                    return list.Count == 1 ? list[0] : JsonConvert.SerializeObject(list);
                });
        }
    }
}