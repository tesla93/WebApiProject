using System.Threading;
using System.Threading.Tasks;
using Core.Membership.Model;
using LockIP;

namespace Core.Membership.Services
{
    /// <summary>
    /// Security service interface
    /// </summary>
    public interface ISecurityService
    {
        /// <summary>
        /// Returns the last locking record which have a most far unlocking date greater than now.
        /// </summary>
        /// <param name="ip">The IP address that checking performing for.</param>
        Task<LockedOutIp> GetLongestActiveLockingByIp(string ip, CancellationToken cancellationToken = default);

        /// <summary>
        /// Check IP address rely on audit history and lock it depends on settings and attempts count.
        /// </summary>
        /// <param name="ip">The IP that should be checked.</param>
        Task CheckIpLockOut(string ip, CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks whether IP address been locked.
        /// </summary>
        /// <param name="ip">The IP address that should be checked.</param>
        Task<bool> IsIpAllowed(string ip, CancellationToken cancellationToken = default);

        /// <summary>
        /// Increases the failed attempts count for a specified user and locking him depends on settings and attempts count.
        /// </summary>
        /// <param name="user">The user who has input wrong credentials.</param>
        Task AddFailedAttemptForUser(User user, CancellationToken cancellationToken = default);

        /// <summary>
        /// Increases the failed attempts count for a specified user and locking him depends on settings and attempts count.
        /// </summary>
        /// <param name="userId">The identifier of user who has input wrong credentials.</param>
        Task AddFailedAttemptForUser(string userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Performs user unlocking.
        /// </summary>
        /// <param name="user">The user who should be unlocked.</param>
        Task UnlockUser(User user, CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks whether IP address allowed for specified allowed IP settings of the user or allowed IP settings of user's roles.
        /// </summary>
        /// <param name="ip">The IP address that should be checked.</param>
        /// <param name="userId">The identifier of user who settings should be used.</param>
        Task<bool> IsIpAllowedForUser(string ip, string userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks that new password is valid for specified user.
        /// </summary>
        /// <param name="user">The user which password should be checked.</param>
        /// <param name="newPassword">New password.</param>
        /// <returns>String that describes an error if it occurred, null otherwise.</returns>
        Task<string> CheckUsersNewPassword(User user, string newPassword);

        /// <summary>
        /// Saves specified user's password to history.
        /// </summary>
        /// <param name="user">The user which password should be saved.</param>
        Task SavePasswordToHistory(User user, CancellationToken cancellationToken = default);

        /// <summary>
        /// Hashes specified string.
        /// </summary>
        /// <param name="value">The target string.</param>
        string GetHashedValue(string value);
    }
}
