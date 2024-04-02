namespace Core.Membership.Model
{
    public static class ClaimTypes
    {
        public const string BelongsToGroup = "bbwt.claims.belongs_to_group";

        public static class Impersonation
        {
            public const string OriginalUserId = "bbwt.claims.impersonate.original_user_id";
            public const string OriginalUserName = "bbwt.claims.impersonate.original_user_name";
            public const string IsImpersonating = "bbwt.claims.impersonate.is_impersonating";
        }
    }
}