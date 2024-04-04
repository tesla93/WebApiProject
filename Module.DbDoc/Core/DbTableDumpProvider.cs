using Module.Core.Data;
using Module.Core.Filters;
using Module.DbDoc.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Module.DbDoc.Core
{
    public class DbTableDumpProvider : IDbTableDumpProvider
    {
        private readonly IDbContext dbContext;
        private readonly List<DbTable> dbTables;
        private const StringComparison IgnoreCaseComparison = StringComparison.InvariantCultureIgnoreCase;

        public DbTableDumpProvider(IDbContext dbContext, List<DbTable> dbTables)
        {
            this.dbContext = dbContext;
            this.dbTables = dbTables;
        }

        public List<Tuple<string, string>> GetTableColumnsList(string tableName)
        {
            var entity = dbContext.Model.GetEntityTypes().FirstOrDefault(et =>
                et.FindPrimaryKey() != null && et.ClrType.ToString().Equals(tableName, IgnoreCaseComparison));

            var result = new List<Tuple<string, string>>();

            if (entity == null)
            {
                return null;
            }

            var navs = entity.GetNavigations().Where(n => !n.IsCollection()).ToList();

            var propertiesList = entity.GetProperties().Where(prop => !prop.IsShadowProperty()).ToList();

            foreach (var prop in propertiesList)
            {
                if (prop.IsForeignKey())
                {
                    var nav = navs.FirstOrDefault(n => n.ForeignKey.Properties.Any(x => x.Name.Equals(prop.Name, IgnoreCaseComparison)));
                    if (nav != null)
                    {
                        var navTable = dbContext.Model.FindEntityType(nav.ClrType)?.GetTableName();
                        var dbtable = dbTables.FirstOrDefault(tab => tab.Name.Equals(navTable));
                        if (string.IsNullOrEmpty(dbtable?.Representation))
                        {
                            result.Add(new Tuple<string, string>(prop.Name, prop.Name));
                        }
                    }
                    else
                    {
                        result.Add(new Tuple<string, string>(prop.Name, prop.Name));
                    }
                }
                else
                {
                    result.Add(new Tuple<string, string>(prop.Name, prop.Name));
                }
            }

            foreach (var nav in navs)
            {
                var navTable = dbContext.Model.FindEntityType(nav.ClrType)?.GetTableName();
                var dbtable = dbTables.FirstOrDefault(tab => tab.Name.Equals(navTable, IgnoreCaseComparison));
                if (!string.IsNullOrEmpty(dbtable?.Representation))
                {
                    result.Add(new Tuple<string, string>(nav.Name, nav.ForeignKey.Properties.FirstOrDefault()?.Name));
                }
            }

            return result;
        }

        public PageResult<dynamic> GetTableDump(string tablename, FilterCommand command)
        {
            var entity = dbContext.Model.GetEntityTypes().FirstOrDefault(et =>
                et.FindPrimaryKey() != null && et.ClrType.ToString().Equals(tablename, IgnoreCaseComparison));

            if (entity == null)
            {
                return null;
            }

            var mi = GetType().GetMethod(nameof(GetTableDumpPage));

            object[] args = { entity, command };

            return (PageResult<dynamic>)mi.MakeGenericMethod(entity.ClrType).Invoke(this, args);
        }

        public PageResult<dynamic> GetTableDumpPage<T>(IEntityType entity, FilterCommand command) where T : class
        {
            var query = dbContext.Set<T>().AsNoTracking();
            var navs = entity.GetNavigations().Where(nav => !nav.IsCollection()).ToArray();

            int totalCount = query.Count();

            foreach (var nav in navs)
            {
                var navTable = dbContext.Model.FindEntityType(nav.ClrType)?.GetTableName();
                var dbtable = dbTables.FirstOrDefault(tab => tab.Name.Equals(navTable, IgnoreCaseComparison));
                if (!string.IsNullOrEmpty(dbtable?.Representation))
                {
                    query = query.Include(nav.Name);
                }
            }

            if (command.IsPaginator)
            {
                query = query.Skip(command.Skip).Take(command.Take);
            }

            var result = new PageResult<dynamic>
            {
                Items = query.ToList().Select(x => this.GetItemFormatted(x, navs, dbTables)).ToList(),
                Total = totalCount
            };

            return result;
        }

        private dynamic GetItemFormatted<T>(T item, INavigation[] navs, List<DbTable> dbtables) where T : class
        {
            var itemFormatted = new ExpandoObject() as IDictionary<string, object>;

            var props = item.GetType().GetProperties().ToList();
            foreach (var prop in props)
            {
                var nav = navs.FirstOrDefault(n => n.Name.Equals(prop.Name, IgnoreCaseComparison));
                if (nav != null)
                {
                    var navtable = dbContext.Model.GetEntityTypes().FirstOrDefault(et => et.ClrType == nav.ClrType)
                        ?.GetTableName();
                    var format = dbtables
                        .FirstOrDefault(dbt => dbt.Name.Equals(navtable, IgnoreCaseComparison))
                        ?.Representation;
                    if (!string.IsNullOrEmpty(format))
                    {
                        itemFormatted.Add(prop.Name, GetNestedObjectFormatted(item, prop, format));
                    }
                    else
                    {
                        itemFormatted.Add(prop.Name, prop.GetValue(item));
                    }
                }
                else
                {
                    itemFormatted.Add(prop.Name, prop.GetValue(item));
                }
            }

            return itemFormatted;
        }

        private object GetNestedObjectFormatted<T>(T item, PropertyInfo prop, string format) where T : class
        {
            var value = prop.GetValue(item);
            var valueProps = value.GetType().GetProperties();

            var counter = 0;

            var r = new Regex(@"%(.+?)%");
            var mc = r.Matches(format);

            var newformat = r.Replace(format, m =>
            {
                var res = "{" + counter.ToString() + "}";
                counter++;
                return res;
            });

            var propNames = new List<string>();
            var propValues = new List<string>();

            mc.ToList().ForEach(x => { propNames.Add(x.Groups[1].Value); });

            propNames.ForEach(pn =>
            {
                var property = valueProps.FirstOrDefault(pr => pr.Name.Equals(pn, IgnoreCaseComparison));
                if (property != null)
                {
                    propValues.Add(property.GetValue(value).ToString());
                }
                else
                {
                    propValues.Add(string.Empty);
                }
            });

            var result = string.Format(newformat, propValues.ToArray());

            return result;
        }
    }
}
