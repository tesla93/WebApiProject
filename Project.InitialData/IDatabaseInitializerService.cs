using System.Threading.Tasks;

namespace InitialData
{
    public interface IDatabaseInitializerService
    {
        void EnsureInitialData();
    }
}