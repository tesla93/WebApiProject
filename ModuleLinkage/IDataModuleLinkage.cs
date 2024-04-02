using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

namespace ModuleLinkage
{
    public interface IDataModuleLinkage
    {
        Task EnsureInitialData(IServiceScope serviceScope);
    }
}
