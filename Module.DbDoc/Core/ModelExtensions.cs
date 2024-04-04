using Microsoft.EntityFrameworkCore.Metadata;
using System.Collections.Generic;
using System.Linq;

namespace Module.DbDoc.Core
{
    public static class ModelExtensions
    {
        public static IEnumerable<IEntityType> GetEntityTypesWithPrimaryKey(this IModel model)
        {
            return model.GetEntityTypes().Where(entityType => entityType.FindPrimaryKey() != null);
        }
    }
}
