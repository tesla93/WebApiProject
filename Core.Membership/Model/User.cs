using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using Core.Membership.Enums;
using FileStorage;
using Core.Data;

namespace Core.Membership.Model
{

    /// <summary>
    /// User class definition
    /// </summary>
    public class User : IdentityUser, IAuditableEntity<string>
    {
        public override string PhoneNumber { get; set; }


        public string FirstName { get; set; }

        public string LastName { get; set; }

        public AccountStatus AccountStatus { get; set; }

        public AccountStatus? PreviousAccountStatus { get; set; }

        public int? SsoProvider { get; set; }

        public DateTimeOffset? FirstPasswordFailureDate { get; set; }

        public string GravatarImage { get; set; }

        public string GravatarEmail { get; set; }

        public PictureMode PictureMode { get; set; }

        public bool U2fEnabled { get; set; }

        public string RecoveryCode { get; set; }


        public int? CompanyId { get; set; }

        public Company Company { get; set; }

        public int? AvatarImageId { get; set; }

        public FileDetails AvatarImage { get; set; }

        public int? InvitationTokenId { get; set; }

        public ActivationToken InvitationToken { get; set; }

        public int? PasswordResetTokenId { get; set; }

        public ActivationToken PasswordResetToken { get; set; }

        public int? EmailConfirmationTokenId { get; set; }

        public ActivationToken EmailConfirmationToken { get; set; }


        public ICollection<UserGroup> UserGroups { get; set; } = new List<UserGroup>();

        public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();

        public ICollection<UserPermission> UserPermissions { get; set; } = new List<UserPermission>();

        public ICollection<AllowedIpUser> AllowedIpUser { get; set; }

        public ICollection<Device> DeviceRegistrations { get; set; }

        public ICollection<AuthenticationRequest> AuthenticationRequests { get; set; }
    }
}