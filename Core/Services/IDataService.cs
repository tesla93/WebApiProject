using System;
using Core.Data;

namespace Core.Services
{
    public interface IDataService
    {
        void ConfigureDataContext(Action<IDbContext> action);
    }
}