namespace Core.Membership.SystemSettings
{
    public class UserLoginSettings
    {
        public static readonly string DefaultTwoFaAppName = "BBWT3";

        public string TwoFaAppName { get; set; } = DefaultTwoFaAppName;
    }
}
