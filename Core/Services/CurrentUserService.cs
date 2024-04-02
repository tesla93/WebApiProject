using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Primitives;

namespace Core.Services
{
    public class CurrentUserService<TUser> : ICurrentUserService
        where TUser : IdentityUser
    {
        private readonly UserManager<TUser> _userManager;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CurrentUserService(UserManager<TUser> userManager, IHttpContextAccessor httpContextAccessor)
        {
            _userManager = userManager;
            _httpContextAccessor = httpContextAccessor;
        }

        public string GetCurrentUserId()
        {
            return _userManager.GetUserId(_httpContextAccessor.HttpContext.User);
        }

        public async Task<string> GetCurrentUserEmail()
        {
            return (await _userManager.GetUserAsync(_httpContextAccessor.HttpContext.User))?.Email;
        }

        public async Task<List<string>> GetCurrentUserRoles()
        {
            var user = await _userManager.GetUserAsync(_httpContextAccessor.HttpContext.User);
            var roles = await _userManager.GetRolesAsync(user);
            return roles.ToList();
        }

        public string GetUserIp()
        {
            try
            {
                var header = _httpContextAccessor.HttpContext.Request.Headers["X-Real-IP"];
                return header != StringValues.Empty
                    ? header.First()
                    : _httpContextAccessor.HttpContext.Connection.RemoteIpAddress.MapToIPv4().ToString();
            }
            catch (Exception)
            {
                // Ignored
            }

            return null;
        }
    }
}