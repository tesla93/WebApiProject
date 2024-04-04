using Module.Core.Data;
using Module.DbDoc.Model;
using Module.DbDoc.Model.ValidationMetadata;
using Castle.Core.Internal;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Globalization;
using System.Linq;

namespace Module.DbDoc.Core
{
    public class DbContextScanner : IDbContextScanner
    {
        private readonly IDatabaseScannerProvider databaseScannerProvider;

        public DbContextScanner(IDatabaseScannerProvider databaseScannerProvider)
        {
            this.databaseScannerProvider = databaseScannerProvider;
        }

        /// <summary>
        /// Scan DB Structure
        /// </summary>
        /// <returns>List of tables</returns>
        public List<DbTable> Scan(IDbContext dbContext)
        {
            var result = new List<DbTable>();

            if (dbContext == null)
            {
                return result;
            }

            var entityTypes = dbContext.Model.GetEntityTypesWithPrimaryKey().ToList();

            if (entityTypes.Count == 0)
            {
                return result;
            }

            var props = entityTypes.SelectMany(x => x.GetProperties()).ToList();

            var databaseScanner = databaseScannerProvider.GetScanner(dbContext);

            var columnMetadata = databaseScanner.ScanColumns(entityTypes);
            var tableMetadata = databaseScanner.ScanTables(entityTypes);

            Debug.Assert(entityTypes.Count == tableMetadata.Count);
            Debug.Assert(props.Count == columnMetadata.Count);

            foreach (var entityType in entityTypes)
            {
                result.Add(GetTableMetadata(dbContext, columnMetadata, tableMetadata, entityType));
            }
            return result;
        }

        private static DbTable GetTableMetadata(IDbContext dbContext, List<ColumnMetadataQuery> columnMetadata, List<TableMetadataQuery> tableMetadata, IEntityType entityType)
        {
            var numRecords = tableMetadata.FirstOrDefault(o => o.TableName == entityType.GetTableName())?.NumRecords ?? 0;
            var table = new DbTable
            {
                Id = Guid.NewGuid(),
                Name = entityType.GetTableName(),
                NumRecords = numRecords,
                ClrType = entityType.ClrType.ToString()
            };

            foreach (var propertyType in entityType.GetProperties())
            {
                table.Columns.Add(GetColumnMetadata(dbContext, columnMetadata, table, entityType, propertyType));
            }

            return table;
        }

        private static DbColumn GetColumnMetadata(IDbContext dbContext, List<ColumnMetadataQuery> columnMetadata, DbTable table, IEntityType entityType, IProperty propertyType)
        {
            IEnumerable<IForeignKey> fks = null;
            var refs = new List<DbTableRef>();

            var min = columnMetadata.FirstOrDefault(o => o.Name == propertyType.Name && o.TableName == table.Name)?.Min;
            var max = columnMetadata.FirstOrDefault(o => o.Name == propertyType.Name && o.TableName == table.Name)?.Max;
            var unique = columnMetadata.FirstOrDefault(o => o.Name == propertyType.Name && o.TableName == table.Name)?.Unique ?? 0;

            if (propertyType.IsForeignKey())
            {
                fks = propertyType.GetContainingForeignKeys().ToList();
                fks.ToList().ForEach(fk =>
                {
                    var tablename = dbContext.Model.FindEntityType(fk.PrincipalEntityType.ClrType).GetTableName();
                    refs.Add(new DbTableRef { Name = tablename, ClrType = fk.PrincipalEntityType.ClrType.ToString() });
                });
            }

            if (propertyType.IsPrimaryKey())
            {
                fks = propertyType.FindContainingPrimaryKey().GetReferencingForeignKeys();
                fks.ToList().ForEach(fk =>
                {
                    var tablename = dbContext.Model.FindEntityType(fk.DeclaringEntityType.ClrType).GetTableName();
                    refs.Add(new DbTableRef { Name = tablename, ClrType = fk.DeclaringEntityType.ClrType.ToString() });
                });
            }

            var column = new DbColumn
            {
                Id = Guid.NewGuid(),
                Name = propertyType.GetColumnName(),
                Type = propertyType.GetColumnType(),
                ClrType = propertyType.ClrType.ToString(),
                ClrTypeGroup = propertyType.ClrType.GetTypeGroup(),
                DefaultValue = propertyType.GetDefaultValue()?.ToString(),
                DefaultValueSQL = propertyType.GetDefaultValueSql(),
                AllowNulls = propertyType.IsNullable,
                IsForeignKey = propertyType.IsForeignKey(),
                IsPrimaryKey = propertyType.IsPrimaryKey(),
                IsIndex = propertyType.IsIndex(),
                RefTables = refs,
                MinValue = min,
                MaxValue = max,
                UniqueCount = unique,
                ValidationMetadata = GetValidationMetadata(entityType, propertyType)
            };
            return column;
        }

        private static List<DbColumnValidationMetadata> GetValidationMetadata(IEntityType entityType, IProperty propertyType)
        {
            var result = new List<DbColumnValidationMetadata>();

            // TODO: Should DTO Entities be cover too?
            System.Reflection.PropertyInfo propertyInfo = entityType.ClrType.GetProperty(propertyType.Name);
            if (propertyInfo == null)
            {
                return result;
            }

            var rules = new List<ValidationRule>();

            // process model Fluent API/data annotations
            if (!propertyType.IsColumnNullable())
            {
                rules.Add(new RequiredValidationRule { IsSystem = true, Required = true, ErrorMessage = GetErrorMessage(propertyType.Name, new RequiredAttribute()) });
            }
            if (propertyType.GetMaxLength() != null && propertyType.ClrType == typeof(string))
            {
                rules.Add(new MaxLengthValidationRule { IsSystem = true, MaxLength = propertyType.GetMaxLength().Value, ErrorMessage = GetErrorMessage(new StringLengthAttribute(propertyType.GetMaxLength().Value)) });
            }

            // Process validation attributes
            var validationAttributes = propertyInfo.GetAttributes<ValidationAttribute>();
            
            foreach (var validationAttribute in validationAttributes)
            {
                switch (validationAttribute)
                {
                    case RequiredAttribute requiredAttribute:
                        if (!rules.Any(x => x is RequiredValidationRule))
                        {
                            rules.Add(new RequiredValidationRule { IsSystem = true, Required = true, ErrorMessage = GetErrorMessage(propertyType.Name, requiredAttribute) });
                        }
                        break;
                    case StringLengthAttribute stringLengthAttribute:
                        if (!rules.Any(x => x is MaxLengthValidationRule))
                        {
                            rules.Add(new MaxLengthValidationRule { IsSystem = true, MaxLength = stringLengthAttribute.MaximumLength, ErrorMessage = GetErrorMessage(stringLengthAttribute) });
                        }
                        break;
                    case RangeAttribute rangeAttribute:
                        if (IsNumericType(rangeAttribute.OperandType))
                        {
                            rules.Add(new NumberRangeValidationRule { IsSystem = true, Min = Convert.ToDouble(rangeAttribute.Minimum, CultureInfo.InvariantCulture), Max = Convert.ToDouble(rangeAttribute.Maximum, CultureInfo.InvariantCulture), ErrorMessage = GetErrorMessage(rangeAttribute) });
                        }
                        if (IsDateType(rangeAttribute.OperandType))
                        {
                            rules.Add(new DateRangeValidationRule { IsSystem = true, Min = Convert.ToDateTime(rangeAttribute.Minimum, CultureInfo.InvariantCulture), Max = Convert.ToDateTime(rangeAttribute.Maximum, CultureInfo.InvariantCulture), ErrorMessage = GetErrorMessage(rangeAttribute) });
                        }
                        break;
                    default:
                        break;
                }
            }

            if (rules.Any())
            {
                result.Add(new DbColumnValidationMetadata { Key = "default", Rules = rules });
            }
            return result;
        }

        private static bool IsDateType(Type type)
        {
            return type == typeof(DateTime) || type == typeof(DateTimeOffset);
        }

        private static bool IsNumericType(Type type)
        {
            return type == typeof(int) || type == typeof(decimal) || type == typeof(long) || type == typeof(double);
        }

        private static string GetErrorMessage(RangeAttribute attribute)
        {
            if (!string.IsNullOrWhiteSpace(attribute.ErrorMessage))
            {
                return attribute.ErrorMessage;
            }
            return $"Value should be between {attribute.Minimum} and {attribute.Maximum}";
        }

        private static string GetErrorMessage(StringLengthAttribute attribute)
        {
            if (!string.IsNullOrWhiteSpace(attribute.ErrorMessage))
            {
                return attribute.ErrorMessage;
            }
            return $"Max length is {attribute.MaximumLength}";
        }

        private static string GetErrorMessage(string propertyName, RequiredAttribute attribute)
        {
            if (!string.IsNullOrWhiteSpace(attribute.ErrorMessage))
            {
                return attribute.ErrorMessage;
            }
            return $"{propertyName} is required";
        }
    }
}
