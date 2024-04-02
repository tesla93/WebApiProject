using Core.Data;
using System.Threading.Tasks;

namespace InitialData
{
    internal static class ProjectInitialData
    {
        public static void EnsureInitialData(IDbContext context)
        {
            // Add project's data initialization here.
            // Initialization is completed with: context.SaveChanges();
        }
    }
}
