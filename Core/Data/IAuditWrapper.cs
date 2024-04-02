using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Core.Data
{
    public interface IAuditWrapper
    {
        void OnBeforeSaveChanges(IEnumerable<EntityEntry> entries);

        Task OnAfterSaveChanges();
    }
}