namespace Project.Services
{
    /// NOTE!
    /// - EITHER you add roles into the Roles class if the project is based on hardcoded roles without permissions (simple system)
    /// - OR add permissions into the Permissions class if the project is based on hardcoded permissions (complex system), 
    ///   having roles managed on the Manage Roles page (/app/roles)
    public static class Roles
    {
        // Add project roles here like: public const string SalesManager = "Sales Manager";
        // For naming consistency it's recommended to use spaces beetween words, each word starts with upper case
        // like ("Sales Manager", "Sales Person")
        public const string SampleProjectRole = "Sample Project Role";
    }
}
