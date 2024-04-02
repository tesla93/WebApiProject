using Core.Data;
using Core.Membership.Model;
using Core.Membership.SystemSettings;
using Project.SystemSettings;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Core.Exceptions;
using Core.Services;
using LockIP;
using Core.Membership.Exceptions;

namespace Core.Membership.Services
{
    /// <summary>
    /// Security service implementation.
    /// </summary>
    public class SecurityService : ISecurityService
    {
        private readonly IDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly ISettingsService _settingsService;
        private readonly ILoginAuditService _auditService;


        public SecurityService(
            IDbContext context,
            UserManager<User> userManager,
            ISettingsService settingsService,
            ILoginAuditService auditService)
        {
            _context = context;
            _userManager = userManager;
            _settingsService = settingsService;
            _auditService = auditService;
        }


        /// <summary>
        /// Returns the last locking record which have a most far unlocking date greater than now.
        /// </summary>
        /// <param name="ip">The IP address that checking performing for.</param>
        public async Task<LockedOutIp> GetLongestActiveLockingByIp(string ip, CancellationToken cancellationToken = default) =>
            await _context.Set<LockedOutIp>()
                .Where(x => x.IpAddress == ip && x.LockoutEnd > DateTime.Now)
                .OrderByDescending(x => x.LockoutEnd)
                .FirstOrDefaultAsync(cancellationToken);

        /// <summary>
        /// Check IP address rely on audit history and lock it depends on settings and attempts count.
        /// </summary>
        /// <param name="ip">The IP that should be checked.</param>
        public async Task CheckIpLockOut(string ip, CancellationToken cancellationToken = default)
        {
            var settings = _settingsService.GetSettingsSection<FailedAttemptsPasswordSettings>();
            if (settings == null)
                throw new ConflictException("Settings for failed login attempts not found.");

            if (settings.LockTypeAccount == LockType.NeverLock ||
                await _auditService.GetLastAttemptsCount(ip, DateTimeOffset.Now.AddMinutes(-settings.PasswordAttemptWindow)) <
                settings.MaxInvalidPasswordAttempts) return;

            await LockOutByIp(ip, settings.IntervalInSeconds, cancellationToken);
        }

        /// <summary>
        /// Checks whether IP address been locked.
        /// </summary>
        /// <param name="ip">The IP address that should be checked.</param>
        public async Task<bool> IsIpAllowed(string ip, CancellationToken cancellationToken = default) =>
            await GetLongestActiveLockingByIp(ip, cancellationToken) == null;

        /// <summary>
        /// Increases the failed attempts count for a specified user and locking him depends on settings and attempts count.
        /// </summary>
        /// <param name="userId">The identifier of user who has input wrong credentials.</param>
        public async Task AddFailedAttemptForUser(string userId, CancellationToken cancellationToken = default) =>
            await AddFailedAttemptForUser(await _userManager.FindByIdAsync(userId), cancellationToken);

        /// <summary>
        /// Increases the failed attempts count for a specified user and locking him depends on settings and attempts count.
        /// </summary>
        /// <param name="user">The user who has input wrong credentials.</param>
        public async Task AddFailedAttemptForUser(User user, CancellationToken cancellationToken = default)
        {
            if (user == null) throw new UserNotExistsException();

            var settings = _settingsService.GetSettingsSection<FailedAttemptsPasswordSettings>();
            if (settings == null)
                throw new ConflictException("Settings for failed login attempts not found.");

            if (user.LockoutEnabled)
                throw new ConflictException("User is already locked.");

            if (settings.LockTypeAccount == LockType.NeverLock) return;

            if (user.FirstPasswordFailureDate == null ||
                (DateTimeOffset.Now - (DateTimeOffset)user.FirstPasswordFailureDate).Minutes > settings.PasswordAttemptWindow)
            {
                await _userManager.ResetAccessFailedCountAsync(user);
                user.FirstPasswordFailureDate = DateTimeOffset.Now;
                await _userManager.UpdateAsync(user);
            }

            await _userManager.AccessFailedAsync(user);

            if (user.AccessFailedCount >= settings.MaxInvalidPasswordAttempts)
            {
                await _userManager.SetLockoutEnabledAsync(user, true);
                await _userManager.ResetAccessFailedCountAsync(user);
                if (settings.UnlockTypeAccount == UnlockType.Temporary)
                    await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow.AddSeconds(settings.IntervalInSeconds));
            }
        }

        /// <summary>
        /// Performs user unlocking.
        /// </summary>
        /// <param name="user">The user who should be unlocked.</param>
        public async Task UnlockUser(User user, CancellationToken cancellationToken = default)
        {
            if (user == null) throw new UserNotExistsException();

            await _userManager.SetLockoutEnabledAsync(user, false);
            user.FirstPasswordFailureDate = null;
            await _userManager.UpdateAsync(user);
        }

        /// <summary>
        /// Checks whether IP address allowed for specified allowed IP settings of the user or allowed IP settings of user's roles.
        /// </summary>
        /// <param name="ip">The IP address that should be checked.</param>
        /// <param name="userId">The identifier of the user which settings should be used.</param>
        public async Task<bool> IsIpAllowedForUser(string ip, string userId, CancellationToken cancellationToken = default)
        {
            if (ip == "0.0.0.1") return true; // localhost

            var user = await _userManager.Users
                .Include(u => u.AllowedIpUser)
                .ThenInclude(al => al.AllowedIp)
                .Include(u => u.UserRoles)
                .ThenInclude(us => us.Role)
                .ThenInclude(r => r.AllowedIpRoles)
                .ThenInclude(ipr => ipr.AllowedIp)
                .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
            if (user == null) throw new UserNotExistsException();


            var allowedUserIpRanges = user.AllowedIpUser
                .Select(x => x.AllowedIp)
                .ToArray();

            var allowedUserRolesIpRanges = user.UserRoles
                .Select(x => x.Role)
                .SelectMany(x => x.AllowedIpRoles)
                .Select(x => x.AllowedIp)
                .Distinct()
                .ToArray();

            // If the user or his roles has restrictions of IP ranges then he must be checked that his IP match at least one range
            if (allowedUserIpRanges.Any() || allowedUserRolesIpRanges.Any())
            {
                if (allowedUserIpRanges.Any(allowedIp => IsIpInRange(allowedIp.IpAddressFirst, allowedIp.IpAddressLast, ip)))
                    return true;

                if (allowedUserRolesIpRanges.Any(allowedIp => IsIpInRange(allowedIp.IpAddressFirst, allowedIp.IpAddressLast, ip)))
                    return true;

                return false;
            }


            return true;
        }

        /// <summary>
        /// Checks that the new password is valid for current system settings and existing user.
        /// </summary>
        /// <param name="user">The user which for password should be checked.</param>
        /// <param name="newPassword">New password.</param>
        /// <returns>String that describes an error if it occurred, null otherwise.</returns>
        public async Task<string> CheckUsersNewPassword(User user, string newPassword)
        {
            if (user == null) throw new UserNotExistsException();

            var settings = _settingsService.GetSettingsSection<UserPasswordSettings>();
            if (settings == null)
                throw new ConflictException("Settings for passwords validation not found.");

            if (string.IsNullOrEmpty(newPassword))
                return "Password can not be empty.";

            if (newPassword == GetHashedValue(user.Email.ToLowerInvariant()))
                return "Your new password must be different from your email.";

            if (settings.PasswordReuse != PasswordReuseSettings.MayUse)
            {
                var passwordHistoryObj = FindPasswordInPasswordHistory(user, newPassword, settings);
                if (passwordHistoryObj != null)
                    return $"Your new password must be different from your last {settings.LastPasswordsNumber} records in password history.";
            }

            return null;
        }

        /// <summary>
        /// Saves specified user's password to history.
        /// </summary>
        /// <param name="user">The user which password should be saved.</param>
        public async Task SavePasswordToHistory(User user, CancellationToken cancellationToken = default)
        {
            if (user == null) throw new UserNotExistsException();

            _context.Set<PasswordHistory>().Add(new PasswordHistory
            {
                UserId = user.Id,
                Password = user.PasswordHash,
                CreateDate = DateTimeOffset.Now,
            });

            await _context.SaveChangesAsync(cancellationToken);
        }

        /// <summary>
        /// Hashes specified string.
        /// </summary>
        /// <param name="value">The target string.</param>
        public string GetHashedValue(string value)
        {
            var sb = new StringBuilder();

            using (var shaM = SHA512.Create())
            {
                var data = Encoding.UTF8.GetBytes(value);
                var hashedUserName = shaM.ComputeHash(data);

                // Convert to string
                foreach (var t in hashedUserName)
                {
                    sb.Append(t.ToString("X2"));
                }
            }

            return sb.ToString();
        }


        private static long IpToLong(string ip)
        {
            double num = 0;
            if (string.IsNullOrEmpty(ip)) return (long)num;

            var ipBytes = ip.Split('.');
            for (var i = ipBytes.Length - 1; i >= 0; i--)
            {
                num += int.Parse(ipBytes[i]) % 256 * Math.Pow(256, 3 - i);
            }
            return (long)num;
        }

        private static bool IsIpInRange(string lowRange, string highRange, string ipAddress)
        {
            var lowerRange = IpToLong(lowRange);
            var upperRange = IpToLong(highRange);
            var ipAddressLong = IpToLong(ipAddress);

            return ipAddressLong >= lowerRange && ipAddressLong <= upperRange;
        }

        private async Task LockOutByIp(string ip, int intervalInSeconds, CancellationToken cancellationToken)
        {
            if (intervalInSeconds > 0)
            {
                await _context.Set<LockedOutIp>().AddAsync(
                    new LockedOutIp
                    {
                        IpAddress = ip,
                        LockoutEnd = DateTime.Now.AddSeconds(intervalInSeconds)
                    },
                    cancellationToken);
                await _context.SaveChangesAsync(cancellationToken);
            }
        }

        private PasswordHistory FindPasswordInPasswordHistory(User user, string enteredPassword, UserPasswordSettings reuseSettings)
        {
            var passwordHasher = new PasswordHasher<User>();
            var passwordsHistory = _context.Set<PasswordHistory>().Where(p => p.UserId == user.Id).ToList();

            if (reuseSettings.PasswordReuse == PasswordReuseSettings.MayReUse)
            {
                // Users may use any password that they haven’t used in the last N passwords
                passwordsHistory = passwordsHistory.OrderByDescending(p => p.CreateDate).Take(reuseSettings.LastPasswordsNumber).ToList();
            }

            foreach (var passwordHistoryObj in passwordsHistory)
            {
                if (passwordHasher.VerifyHashedPassword(user, passwordHistoryObj.Password, enteredPassword) ==
                    PasswordVerificationResult.Success)
                {
                    return passwordHistoryObj;
                }
            }

            return null;
        }
    }
}