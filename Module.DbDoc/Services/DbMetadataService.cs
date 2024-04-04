using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using AutoMapper;
using Module.DbDoc.Core;
using Module.DbDoc.Model;
using Module.DbDoc.Model.ValidationMetadata;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Logging;

namespace Module.DbDoc.Services
{
    public class DbMetadataService : IDbMetadataService
    {
        private readonly IDbContextProvider dbContextProvider;
        private readonly IServiceProvider serviceProvider;
        private readonly IDbDocToolService _dbDocToolService;
        private readonly IMapper _mapper;
        private readonly ILogger<DbMetadataService> logger;

        public DbMetadataService(IDbContextProvider dbContextProvider,
            IServiceProvider serviceProvider,
            IDbDocToolService dbDocToolService,
            IMapper mapper,
            ILogger<DbMetadataService> logger)
        {
            this.dbContextProvider = dbContextProvider;
            this.serviceProvider = serviceProvider;
            _dbDocToolService = dbDocToolService;
            _mapper = mapper;
            this.logger = logger;
        }

        public List<DbTableMetadata> GetAllMetadata()
        {
            var dbStructure = _dbDocToolService.GetDbStructure();

            if (!dbStructure.IsValid)
            {
                dbStructure = _dbDocToolService.GetSyncedStructure();
                _dbDocToolService.SaveDbStructureToDb(dbStructure);
            }

            var result = new List<DbTableMetadata>();
            dbContextProvider.GetDbContexts(serviceProvider).ToList().ForEach(_dbContext =>
            {
                if (_dbContext == null)
                {
                    logger.LogError("DBDoc service was unable to process dataContext due to database connection issue");
                    return;
                }
                try
                {
                    var entityTypes = _dbContext.Model.GetEntityTypesWithPrimaryKey();
                    var tables = dbStructure.Tables.Where(table => entityTypes.Any(entityType => entityType.ClrType.ToString() == table.ClrType)).ToList();
                    result.AddRange(FilterTables(tables, dbStructure.ColumnTypes, entityTypes));
                    logger.LogInformation($"DBDoc service processed '{_dbContext.GetType()}' dataContext successfully.");
                }
                catch(Exception ex)
                {
                    logger.LogError(ex, "DBDoc service was unable to process dataContext due to database connection issue");
                }
            });

            return result;
        }

        private IEnumerable<DbTableMetadata> FilterTables(List<DbTable> tables, IEnumerable<DbColumnType> dbColumnTypes, IEnumerable<IEntityType> entityTypes)
        {
            return from table in tables
                   let entityType = entityTypes.Where(x => x.FindPrimaryKey() != null)
               .FirstOrDefault(x => string.Equals(x.ClrType.ToString(), table.ClrType, StringComparison.InvariantCultureIgnoreCase))
                   let nextItem = new DbTableMetadata
                   {
                       Name = table.Name,
                       ClrType = table.ClrType,
                       DtoDetails = GetAllDtoDetails(entityType?.ClrType),
                       Columns = FilterColumns(table.Columns, dbColumnTypes).ToList()
                   }
                   where nextItem.Columns.Count > 0
                   select nextItem;
        }

        private IEnumerable<DbColumnMetadata> FilterColumns(List<DbColumn> columns, IEnumerable<DbColumnType> dbColumnTypes)
        {
            return from DbColumn column in columns
                   let columnType = GetColumnType(column, dbColumnTypes)
                   let nextItem = new DbColumnMetadata
                   {
                       Name = column.Name,
                       ValidationMetadata = GetValidationMetadata(column, columnType),
                       ViewMetadata = GetViewMetadata(column, columnType),
                   }
                   where nextItem.ValidationMetadata.Count > 0 || nextItem.ViewMetadata.Count > 0
                   select nextItem;
        }

        public List<DbMetadataFieldResult> GetMetadata<DTOType>(string url = null)
        {
            return GetMetadata(typeof(DTOType), url);
        }

        public List<DbMetadataFieldResult> GetMetadata(Type dtoType, string url = null)
        {
            var map = _mapper.ConfigurationProvider.GetAllTypeMaps().FirstOrDefault(x => x.DestinationType == dtoType);
            if (map == null)
            {
                return null;
            }

            var sourceType = map.SourceType;
            var structure = _dbDocToolService.GetDbStructure();

            var entityTypes = new List<IEntityType>();
            dbContextProvider.GetDbContexts(serviceProvider).ToList().ForEach(_dbContext =>
            {
                entityTypes.AddRange(_dbContext.Model.GetEntityTypes());
            });

            var entityType = entityTypes.FirstOrDefault(o => o.Name == sourceType.FullName);
            var table = structure.Tables.FirstOrDefault(o => o.Name == entityType?.GetTableName());

            if (table == null)
            {
                return null;
            }

            var dtoDetails = GetDtoDetails(map);

            var res = table.Columns.Select(col =>
            {
                var columnType = GetColumnType(col, structure.ColumnTypes);
                return new DbMetadataFieldResult
                {
                    FieldName = col.Name,
                    GridColumnViewDetails = ConstructGridColumnViewDetails(GetViewMetadata(col, columnType), dtoType, url),
                    ValidationRules = ConstructValidationDetails(GetValidationMetadata(col, columnType), dtoType, url),
                };
            }).ToList();

            const StringComparison comparison = StringComparison.InvariantCultureIgnoreCase;

            foreach (var prop in dtoDetails.Properties)
            {
                var sourceEntityTable = structure.Tables.FirstOrDefault(x => string.Equals(x.Name, prop.SourceEntityName, comparison));
                var sourceEntityCol = sourceEntityTable?.Columns.FirstOrDefault(x => string.Equals(x.Name, prop.SourceEntityFieldName, comparison));

                if (sourceEntityCol == null) continue;

                var sourceEntityColType = GetColumnType(sourceEntityCol, structure.ColumnTypes);
                var dtoColMetadata = new DbMetadataFieldResult
                {
                    FieldName = prop.PropertyName,
                    GridColumnViewDetails = ConstructGridColumnViewDetails(GetViewMetadata(sourceEntityCol, sourceEntityColType), dtoType, url),
                    ValidationRules = ConstructValidationDetails(GetValidationMetadata(sourceEntityCol, sourceEntityColType), dtoType, url),
                };

                var index = res.FindIndex(x => string.Equals(x.FieldName, dtoColMetadata.FieldName));
                if (index != -1)
                {
                    res[index] = dtoColMetadata;
                }
                else
                {
                    res.Add(dtoColMetadata);
                }
            }

            return res;
        }

        private List<DbDtoDetails> GetAllDtoDetails(Type entityType)
        {
            if (entityType == null) return null;

            var typeMaps = _mapper.ConfigurationProvider.GetAllTypeMaps().Where(x => x.SourceType == entityType).ToList();

            return typeMaps.Select(GetDtoDetails).ToList();
        }

        private static DbDtoDetails GetDtoDetails(TypeMap typeMap)
        {
            var dtoProperties = new List<DbDtoProperty>();
            foreach (var propertyMap in typeMap.PropertyMaps)
            {
                var destinationMember = propertyMap.DestinationMember as PropertyInfo;
                if (destinationMember == null) continue;

                var sourceMember = propertyMap.SourceMembers.LastOrDefault() as PropertyInfo;
                if (sourceMember == null) continue;
                if (sourceMember.PropertyType.IsEnum) continue;

                if (propertyMap.CustomMapExpression != null)
                {
                    var expr = propertyMap.CustomMapExpression;
                    if (expr.Body.NodeType != ExpressionType.MemberAccess) continue;

                    var memberExpr = (MemberExpression)expr.Body;
                    dtoProperties.Add(new DbDtoProperty()
                    {
                        PropertyName = destinationMember.Name,
                        SourceEntityName = memberExpr.Member.DeclaringType.Name,
                        SourceEntityFieldName = memberExpr.Member.Name,
                    });
                }
                else if (destinationMember.Name != sourceMember.Name || destinationMember.PropertyType != sourceMember.PropertyType)
                {
                    dtoProperties.Add(new DbDtoProperty
                    {
                        PropertyName = destinationMember.Name,
                        SourceEntityName = sourceMember.DeclaringType.Name,
                        SourceEntityFieldName = sourceMember.Name,
                    });
                }
            }

            return new DbDtoDetails
            {
                Name = typeMap.DestinationType.Name,
                ClrType = typeMap.DestinationType.ToString(),
                Properties = dtoProperties
            };
        }

        private static DbColumnType GetColumnType(DbColumn col, IEnumerable<DbColumnType> dbColumnTypes)
        {
            return col.DbColumnTypeRef.HasValue ? dbColumnTypes.First(x => x.Id == col.DbColumnTypeRef) : null;
        }

        private static List<DbColumnValidationMetadata> GetValidationMetadata(DbColumn col, DbColumnType columnType)
        {
            if (columnType?.ValidationMetadata == null) return col.ValidationMetadata;

            var defaultMetadata = col.ValidationMetadata.FirstOrDefault(x => x.Key == "default");

            if (defaultMetadata == null)
            {
                defaultMetadata = new DbColumnValidationMetadata
                {
                    Key = "default",
                    Rules = columnType.ValidationMetadata.Rules,
                };
            }

            var columnTypeRulesToAdd = columnType.ValidationMetadata.Rules.Except(defaultMetadata.Rules, new ValidationRuleTypeEqualityComparer());

            defaultMetadata.Rules.AddRange(columnTypeRulesToAdd);

            return new List<DbColumnValidationMetadata> { defaultMetadata }.Concat(col.ValidationMetadata.Where(x => x.Key != "default")).ToList();
        }

        private static List<DbColumnViewMetadata> GetViewMetadata(DbColumn col, DbColumnType columnType)
        {
            if (columnType?.ViewMetadata?.GridColumnDetails == null) return col.ViewMetadata;

            var metadata = new DbColumnViewMetadata
            {
                Key = "default",
                GridColumnDetails = columnType.ViewMetadata.GridColumnDetails,
            };

            return new List<DbColumnViewMetadata> { metadata }.Concat(col.ViewMetadata.Where(x => x.Key != "default")).ToList();
        }

        /// For details on the metadata controlling the display of grid column width please do read
        /// https://wiki.bbconsult.co.uk/display/BLUEB/Field+Widths
        private static GridColumnViewDetails ConstructGridColumnViewDetails(List<DbColumnViewMetadata> metadata, Type dtoType, string url = null)
        {
            GridColumnViewDetails res = null;

            var defaultMetadata = metadata.FirstOrDefault(x => string.Equals(x.Key, "default", StringComparison.InvariantCultureIgnoreCase));
            if (defaultMetadata != null)
            {
                res = defaultMetadata.GridColumnDetails;
            }

            var dtoMetadata = metadata.FirstOrDefault(x => string.Equals(x.Key, dtoType.Name, StringComparison.InvariantCultureIgnoreCase));
            if (dtoMetadata != null)
            {
                res = CombineViewDetails(res, dtoMetadata.GridColumnDetails);
            }

            if (string.IsNullOrWhiteSpace(url)) return res;

            var urlViewMetadata = metadata.FirstOrDefault(x => string.Equals(x.Key, url, StringComparison.InvariantCultureIgnoreCase));
            if (urlViewMetadata != null)
            {
                res = urlViewMetadata.GridColumnDetails;
            }

            return res;

            GridColumnViewDetails CombineViewDetails(GridColumnViewDetails lhs, GridColumnViewDetails rhs)
            {
                return rhs;
            }
        }

        private static List<ValidationRule> ConstructValidationDetails(List<DbColumnValidationMetadata> metadata, Type dtoType, string url = null)
        {
            var res = new List<ValidationRule>();
            var defaultMetadata = metadata.FirstOrDefault(x => string.Equals(x.Key, "default", StringComparison.InvariantCultureIgnoreCase));
            if (defaultMetadata != null)
            {
                res.AddRange(defaultMetadata.Rules);
            }

            var dtoMetadata = metadata.FirstOrDefault(x => string.Equals(x.Key, dtoType.Name, StringComparison.InvariantCultureIgnoreCase));
            if (dtoMetadata != null)
            {
                res = CombineValidationRules(res, dtoMetadata.Rules);
            }

            if (string.IsNullOrWhiteSpace(url)) return res;

            var urlMetadata = metadata.FirstOrDefault(x => string.Equals(x.Key, url, StringComparison.InvariantCultureIgnoreCase));
            if (urlMetadata != null)
            {
                res = CombineValidationRules(res, urlMetadata.Rules);
            }

            return res;

            List<ValidationRule> CombineValidationRules(List<ValidationRule> lhs, List<ValidationRule> rhs)
            {
                var lhsRules = lhs.Where(l => rhs.All(r => r.GetType() != l.GetType()));
                return lhsRules.Union(rhs).ToList();
            }
        }

        private class ValidationRuleTypeEqualityComparer : IEqualityComparer<ValidationRule>
        {
            public bool Equals([AllowNull] ValidationRule x, [AllowNull] ValidationRule y)
            {
                return x.GetType() == y.GetType();
            }

            public int GetHashCode([DisallowNull] ValidationRule obj)
            {
                return obj.GetType().GetHashCode();
            }
        }
    }
}