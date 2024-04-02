using Core.Membership.DTO;

namespace Core.Membership
{
    public static class InitialUsers
    {
        public static UserDTO SuperAdmin = new UserDTO
        {
            Email = "superadmin@bbconsult.co.uk",
            Password = "increase762Value",
            FirstName = "Super",
            LastName = "Admin"
        };

        public static UserDTO SystemAdmin = new UserDTO
        {
            Email = "systemadmin@bbconsult.co.uk",
            Password = "cover918Surface",
            FirstName = "System",
            LastName = "Admin"
        };
    };
}
