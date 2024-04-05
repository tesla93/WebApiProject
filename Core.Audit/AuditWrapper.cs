using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Core.Data;

namespace Core.Audit
{
    public class AuditWrapper : IAuditWrapper
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IAuditDataContext _auditDataContext;

        private AuditChangeEntry[] _auditEntries;

        public AuditWrapper(IAuditDataContext auditDataContext = null, IHttpContextAccessor httpContextAccessor = null)
        {
            _httpContextAccessor = httpContextAccessor;
            _auditDataContext = auditDataContext;
        }

        public void OnBeforeSaveChanges(IEnumerable<EntityEntry> entries)
        {
            _auditEntries = GetAuditEntries(entries).ToArray();
        }

        public Task OnAfterSaveChanges()
        {
            if (_auditEntries != null && _auditEntries.Any())
            {
                foreach (var auditEntry in _auditEntries)
                {
                    foreach (var prop in auditEntry.KeyProperties)
                    {
                        if (prop.Metadata.IsPrimaryKey() && prop.CurrentValue is int)
                        {
                            auditEntry.EntityId = (int)prop.CurrentValue;
                        }
                        else
                        {
                            auditEntry.NewValues[prop.Metadata.Name] = prop.CurrentValue;
                        }
                    }

                    var auditChanges = auditEntry.ToAudit();
                    _auditDataContext.ChangeLogs.Add(auditChanges);
                }

                return _auditDataContext.SaveChangesAsync();
            }

            return Task.CompletedTask;
        }


        private IEnumerable<AuditChangeEntry> GetAuditEntries(IEnumerable<EntityEntry> entries)
        {
            foreach (var entry in entries)
            {
                if ((entry.Entity is IAuditableEntity ||
                     entry.Entity.GetType().GetInterfaces().Any(interfaceItem =>
                        interfaceItem.IsGenericType && interfaceItem.GetGenericTypeDefinition() == typeof(IAuditableEntity<>))) &&
                    entry.State != EntityState.Detached &&
                    entry.State != EntityState.Unchanged)
                {
                    var auditEntry = new AuditChangeEntry(entry);
                    auditEntry.TableName = entry.Metadata.GetTableName();
                    auditEntry.EntityName = entry.Entity.GetType().Name;

                    var user = _httpContextAccessor?.HttpContext?.User;
                    if (user != null)
                    {
                        auditEntry.UserName = user.Identity.Name;
                    }

                    foreach (var property in entry.Properties)
                    {
                        var propertyName = property.Metadata.Name;
                        if (property.Metadata.IsPrimaryKey() && property.CurrentValue is int)
                        {
                            auditEntry.EntityId = (int)property.CurrentValue;
                        }
                        else
                        {
                            switch (entry.State)
                            {
                                case EntityState.Added:
                                    auditEntry.NewValues[propertyName] = property.CurrentValue;
                                    break;
                                case EntityState.Deleted:
                                    auditEntry.OldValues[propertyName] = property.OriginalValue;
                                    break;
                                case EntityState.Modified:
                                    if (property.IsModified)
                                    {
                                        auditEntry.OldValues[propertyName] = property.OriginalValue;
                                        auditEntry.NewValues[propertyName] = property.CurrentValue;
                                    }
                                    break;
                                default:
                                    // Other states are not handled
                                    break;
                            }
                        }

                        if (property.Metadata.IsKey() || property.Metadata.IsForeignKey())
                        {
                            auditEntry.KeyProperties.Add(property);
                        }
                    }

                    yield return auditEntry;
                }
            }
        }
    }
}