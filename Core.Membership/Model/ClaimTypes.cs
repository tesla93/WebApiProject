namespace Core.Membership.Model
{
    public static class ClaimTypes
    {
        public const string BelongsToGroup = "claims.belongs_to_group";

        public static class Impersonation
        {
            public const string OriginalUserId = "claims.impersonate.original_user_id";
            public const string OriginalUserName = "claims.impersonate.original_user_name";
            public const string IsImpersonating = "claims.impersonate.is_impersonating";
        }
    }
}