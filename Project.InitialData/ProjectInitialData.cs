using Core.Data;
using System.Threading.Tasks;

namespace Project.InitialData
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
