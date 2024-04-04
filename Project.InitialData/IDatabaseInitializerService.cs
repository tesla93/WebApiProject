using System.Threading.Tasks;

namespace Project.InitialData
{
    public interface IDatabaseInitializerService
    {
        void EnsureInitialData();
    }
}