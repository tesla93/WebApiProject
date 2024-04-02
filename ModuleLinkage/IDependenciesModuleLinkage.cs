using Autofac;

namespace ModuleLinkage
{
    public interface IDependenciesModuleLinkage
    {
        void RegisterDependencies(ContainerBuilder builder);
    }
}
