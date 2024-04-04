using Module.Core.Data;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Module.DbDoc
{
    public interface IDbContextProvider
    {
        void Register(Type type);
        IDbContext[] GetDbContexts(IServiceProvider serviceProvider);
    }
}