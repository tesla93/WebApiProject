using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Newtonsoft.Json;
using Module.DbDoc.Model;
using Module.DbDoc.Enums;
using Module.DbDoc.Model.ValidationMetadata;
using Module.GitLab;
using Module.Metadata;
using Module.DbDoc.Core;
using Microsoft.Extensions.Logging;

namespace Module.DbDoc.Services
{
    public class DbDocToolService : IDbDocToolService
    {
        private const string DbDocMetadataKey = "DBDOCUMENT";

        #region private readonly

        private readonly IDbContextProvider dbContextProvider;
        private readonly IServiceProvider serviceProvider;
        private readonly IMetadataService metadataService;
        private readonly IDbContextScanner dbContextScanner;
        private readonly IGitLabService gitlabService;

        public IWebHostEnvironment Environment { get; }

        private readonly ILogger<DbDocToolService> logger;

        #endregion

        #region constructor

        public DbDocToolService(
            IDbContextProvider dbContextProvider,
            IServiceProvider serviceProvider,
            IMetadataService metadataService,
            IDbContextScanner dbContextScanner,
            IGitLabService gitlabService,
            ILogger<DbDocToolService> logger,
            IWebHostEnvironment env)
        {
            this.dbContextProvider = dbContextProvider;
            this.serviceProvider = serviceProvider;
            this.metadataService = metadataService;
            this.dbContextScanner = dbContextScanner;
            this.gitlabService = gitlabService;
            Environment = env;
            this.logger = logger;
        }

        #endregion

        #region serialization

        /// <summary>
        /// Serializes DBStructureObj to json string
        /// </summary>
        /// <param name="obj"></param>
        /// <returns>Json string</returns>
        private string SerializeDBStructureObj(DbStructure obj) => JsonConvert.SerializeObject(obj);

        /// <summary>
        /// Deserializes json string to DBStructureObj
        /// </summary>
        /// <param name="obj"></param>
        /// <returns>DBStructure object</returns>
        private DbStructure DeserializeDBStructureJson(string obj)
        {
            try
            {
                return (DbStructure)JsonConvert.DeserializeObject(obj, typeof(DbStructure));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "DBDoc service cannot restore a stored metadata");
                return new DbStructure();
            }
        }

        #endregion

        #region io.operations

        /// <summary>
        /// Get Json File Contents
        /// </summary>
        /// <returns>string</returns>
        private string GetJsonFileContents(string path) => File.ReadAllText(path);

        #endregion

        #region get

        /// <summary>
        /// Gets DBStructure from saved json file
        /// </summary>
        /// <returns>DBStructure object</returns>
        public DbStructure GetDbStructureFromJson(string path)
        {
            var json = GetJsonFileContents(path);
            return DeserializeDBStructureJson(json);
        }

        /// <summary>
        /// Gets DBStructure from saved json file
        /// </summary>
        /// <returns>DBStructure object</returns>
        private DbStructure GetDbStructureFromDb()
        {
            var value = metadataService.GetByKey(DbDocMetadataKey)?.Value;
            return value == null ? new DbStructure() : DeserializeDBStructureJson(value);
        }

        public DbStructure GetDbStructure() => GetDbStructureFromDb();

        public List<DbColumnType> GetColumnTypes() => GetDbStructureFromDb()?.ColumnTypes;

        #endregion

        #region save

        private void CleanObjBeforeSave(DbStructure obj) =>
            obj.Tables.ForEach(tbl => tbl.Columns.ForEach(col =>
                {
                    col.MinValue = null;
                    col.MaxValue = null;
                    col.UniqueCount = 0;
                }));

        /// <summary>
        /// Save DB Structure to DB
        /// </summary>
        /// <param name="obj"></param>
        public void SaveDbStructureToDb(DbStructure obj)
        {
            CleanObjBeforeSave(obj);
            obj.Timestamp = DateTime.UtcNow;
            var json = SerializeDBStructureObj(obj);
            metadataService.Save(DbDocMetadataKey, json);
        }

        #endregion

        #region sync

        /// <summary>
        ///
        /// </summary>
        /// <returns></returns>
        public DbStructure GetSyncedStructure(DbStructure obj = null)
        {
            var existingJson = obj ?? GetDbStructureFromDb();
            if (!existingJson.IsValid)
            {
                existingJson = DbStructure.GetCleanDBStructure();
            }

            var columnTypes = new List<DbColumnType>();

            if(obj != null)
            {
                var dbObj = GetDbStructureFromDb();
                if(dbObj != null)
                {
                    columnTypes = dbObj.ColumnTypes;
                }
            }
            else
            {
                columnTypes = existingJson.ColumnTypes;
            }

            var syncedDbTables = GetSyncedDbTables(existingJson.Tables);

            ValidateValidationMetadata(syncedDbTables);

            UpdateDbTableRefs(syncedDbTables);

            var syncedTree = GetSyncedTree(existingJson.Tree, existingJson.Folders, syncedDbTables);

            return new DbStructure()
            {
                Tables = syncedDbTables,
                ColumnTypes = columnTypes,
                Folders = existingJson.Folders,
                Tree = syncedTree
            };
        }

        private void UpdateDbTableRefs(List<DbTable> syncedDbTables)
        {
            syncedDbTables
                .SelectMany(table => table.Columns.SelectMany(column => column.RefTables))
                .ToList()
                .ForEach(dbTableRef => {
                    dbTableRef.RefId = syncedDbTables.First(x => x.ClrType == dbTableRef.ClrType).Id;
                });
        }

        private DbTable GetSyncedTable(DbTable existing, DbTable scanned) =>
            new DbTable
            {
                Anonymisation = existing.Anonymisation,
                Description = existing.Description,
                Name = scanned.Name,
                ClrType = scanned.ClrType,
                Id = existing.Id,
                NumRecords = scanned.NumRecords,
                Representation = existing.Representation,
                Columns = GetSyncedDbColumns(existing.Columns, scanned.Columns)
            };

        private DbColumn GetSyncedColumn(DbColumn existing, DbColumn scanned) =>
            new DbColumn
            {
                Description = existing.Description,
                Name = scanned.Name,
                Id = existing.Id,
                AllowNulls = scanned.AllowNulls,
                ClrType = scanned.ClrType,
                ClrTypeGroup = scanned.ClrTypeGroup,
                DefaultValue = scanned.DefaultValue,
                DefaultValueSQL = scanned.DefaultValueSQL,
                IsForeignKey = scanned.IsForeignKey,
                IsIndex = scanned.IsIndex,
                IsPrimaryKey = scanned.IsPrimaryKey,
                DbColumnTypeRef = existing.DbColumnTypeRef,
                Type = scanned.Type,
                AnonRule = existing.AnonRule,
                RefTables = scanned.RefTables,
                MaxValue = scanned.MaxValue,
                MinValue = scanned.MinValue,
                UniqueCount = scanned.UniqueCount,
                ViewMetadata = existing.ViewMetadata,
                ValidationMetadata = GetSyncedValidationMetadata(existing.ValidationMetadata, scanned.ValidationMetadata)
            };

        private List<DbColumnValidationMetadata> GetSyncedValidationMetadata(List<DbColumnValidationMetadata> existing, List<DbColumnValidationMetadata> scanned)
        {
            if (!existing.Any())
            {
                return scanned;
            }

            if (!scanned.Any())
            {
                return existing;
            }

            var result = new List<DbColumnValidationMetadata>();

            if (!existing.Any(x => x.Key == "default"))
            {
                result.AddRange(scanned);
                result.AddRange(existing);
                return result;
            }

            var combinedRules = new List<ValidationRule>();

            var existingRules = existing.Single(x => x.Key == "default").Rules;
            var scannedRules = scanned.Single(x => x.Key == "default").Rules;
            var scannedRuleTypes = scanned.Single(x => x.Key == "default").Rules.Select(rule => rule.GetType());

            combinedRules.AddRange(scannedRules.Select(rule => {
                var existingRule = existingRules.FirstOrDefault(e => e.GetType() == rule.GetType() && e.IsSystem);
                if (existingRule != null)
                {
                    rule.ErrorMessage = existingRule.ErrorMessage;
                }
                return rule;
            }));

            combinedRules.AddRange(existingRules.Where(rule => !scannedRuleTypes.Contains(rule.GetType())));

            result.Add(new DbColumnValidationMetadata { Key = "default", Rules = combinedRules });
            result.AddRange(existing.Where(x => x.Key != "default"));            
            return result;
        }

        private List<DbTable> GetSyncedDbTables(List<DbTable> existingList)
        {
            var db = dbContextProvider.GetDbContexts(serviceProvider).ToList();
            var scannedDbTables = new List<DbTable>();
            db.ForEach(dbContext =>
            {
                scannedDbTables.AddRange(dbContextScanner.Scan(dbContext));                
            });

            var result = new List<DbTable>();
            scannedDbTables.ForEach(table =>
            {
                var existingTable = existingList.FirstOrDefault(ex => ex.Name.ToLowerInvariant().Equals(table.Name.ToLowerInvariant()) && ex.ClrType.Equals(table.ClrType));
                result.Add(existingTable != null ? GetSyncedTable(existingTable, table) : table);
            });

            return result;
        }

        private List<DbColumn> GetSyncedDbColumns(List<DbColumn> existingList, List<DbColumn> scannedList)
        {
            var result = new List<DbColumn>();

            scannedList.ForEach(column =>
            {
                var existingColumn = existingList.FirstOrDefault(ec => ec.Name.ToLowerInvariant().Equals(column.Name.ToLowerInvariant()));
                result.Add(existingColumn != null ? GetSyncedColumn(existingColumn, column) : column);
            });
            return result;
        }

        //private void RecalcOrderNumbers(DbStructure dbStructure)
        //{
        //    var Order = 1;
        //    foreach (var folder in dbStructure.Folders.OrderBy(x => x.VisibilitySettings.Order))
        //    {
        //        folder.VisibilitySettings.Order = Order++;
        //        var tables = dbStructure.Tables.Where(t => t.FolderId == folder.Id);
        //        var orderForNewItems = tables.Max(x => x.VisibilitySettings.Order);
        //        tables.Where(x => x.VisibilitySettings.Order == 0).OrderBy(x => x.Name).ToList().ForEach(x => x.VisibilitySettings.Order = ++orderForNewItems);
        //        tables.OrderBy(x => x.VisibilitySettings.Order).ToList().ForEach(table =>
        //        {
        //            table.VisibilitySettings.Order = Order++;
        //            var orderForNewColumns = table.Columns.Max(x => x.VisibilitySettings.Order);
        //            table.Columns.Where(x => x.VisibilitySettings.Order == 0).OrderBy(x => x.Name).ToList().ForEach(x => x.VisibilitySettings.Order = ++orderForNewColumns);
        //            tables.OrderBy(x => x.VisibilitySettings.Order).ToList().ForEach(table =>
        //            {
        //                table.VisibilitySettings.Order = Order++;
        //            });
        //        });
        //    }
        //}

        /// <summary>
        /// Sync DBStructure obj and save
        /// </summary>
        /// <param name="obj">DBStructure</param>
        public void SyncAndSaveStructure(DbStructure obj)
        {
            var syncedStruct = GetSyncedStructure(obj);
            SetMetadata(obj, syncedStruct);
            SaveDbStructureToDb(syncedStruct);
        }

        public void SaveItemTypes(List<DbColumnType> items)
        {
            var obj = GetSyncedStructure();
            SetColumnTypes(obj, items);
            SaveDbStructureToDb(obj);
        }

        private void SetColumnTypes(DbStructure obj, List<DbColumnType> items)
        {
            foreach (var item in items)
            {
                if (item.ValidationMetadata != null)
                {
                    item.ValidationMetadata.Key = "default";
                    item.ValidationMetadata.Rules = item.ValidationMetadata.Rules?
                        .Where(x => ValidationRuleIsValid(x, item.Group)).ToList();
                    if (!item.ValidationMetadata.Rules?.Any() ?? false)
                    {
                        item.ValidationMetadata = null;
                    }
                }

                if (item.ViewMetadata != null)
                {
                    item.ViewMetadata.Key = "default";
                }
            }
            var removedItems = obj.ColumnTypes.Except(items).ToList();
            foreach(var removedItem in removedItems)
            {
                foreach(var table in obj.Tables)
                {
                    foreach(var column in table.Columns)
                    {
                        if (column.DbColumnTypeRef.HasValue && column.DbColumnTypeRef == removedItem.Id)
                        {
                            column.DbColumnTypeRef = null;
                            column.AnonRule = null;
                        }
                    }
                }
            }

            obj.ColumnTypes = items;
        }

        private void SetMetadata(DbStructure source, DbStructure target)
        {
            var columnTypes = target.ColumnTypes;

            var strComparer = StringComparison.InvariantCultureIgnoreCase;
            var fromDb = GetDbStructureFromDb();
            if (!fromDb.IsValid)
            {
                fromDb = DbStructure.GetCleanDBStructure();
            }

            HandleTables(source.Tables, target.Tables, fromDb.Tables);

            void HandleTables(List<DbTable> sourceTables, List<DbTable> targetTables, List<DbTable> fromDbTables)
            {
                foreach (var sourceTable in sourceTables)
                {
                    var targetTable = targetTables.FirstOrDefault(x => string.Equals(x.Name, sourceTable.Name, strComparer));
                    if (targetTable == null) continue;

                    var fromDbTable = fromDbTables.FirstOrDefault(x => string.Equals(x.Name, sourceTable.Name, strComparer));

                    HandleColumns(sourceTable.Columns, targetTable.Columns, fromDbTable?.Columns);
                }
            }

            void HandleColumns(List<DbColumn> sourceColumns, List<DbColumn> targetColumns,
                List<DbColumn> fromDbColumns = null)
            {
                foreach (var sourceColumn in sourceColumns)
                {
                    var targetColumn = targetColumns.FirstOrDefault(x => string.Equals(x.Name, sourceColumn.Name, strComparer));
                    if (targetColumn == null) continue;

                    var fromDbColumn = fromDbColumns?.FirstOrDefault(x => string.Equals(x.Name, sourceColumn.Name, strComparer));

                    HandleViewMetadata(sourceColumn, targetColumn, fromDbColumn);
                    HandleValidationMetadata(sourceColumn, targetColumn, fromDbColumn);
                }
            }

            void HandleViewMetadata(DbColumn sourceColumn, DbColumn targetColumn, DbColumn fromDbColumn = null)
            {
                targetColumn.ViewMetadata = sourceColumn.ViewMetadata;
                if (targetColumn.ViewMetadata.All(x => x.Key != "default") && targetColumn.DbColumnTypeRef.HasValue)
                {
                    var colType = columnTypes.Find(x => x.Id == targetColumn.DbColumnTypeRef);
                    if (colType.ViewMetadata != null)
                    {
                        targetColumn.ViewMetadata.Insert(0, colType.ViewMetadata);
                    }
                }
            }

            void HandleValidationMetadata(DbColumn sourceColumn, DbColumn targetColumn, DbColumn fromDbColumn = null)
            {
                targetColumn.ValidationMetadata.RemoveAll(x =>
                    !string.Equals(x.Key, "default", strComparer) &&
                    sourceColumn.ValidationMetadata.All(y => !string.Equals(x.Key, y.Key, strComparer))
                );

                foreach (var sourceMetadata in sourceColumn.ValidationMetadata)
                {
                    var rules = sourceMetadata.Rules.Where(x => ValidationRuleIsValid(x, targetColumn.ClrTypeGroup, targetColumn.AllowNulls)).ToList();
                    if (!rules.Any())
                    {
                        var fromDbMetadata = fromDbColumn?.ValidationMetadata.FirstOrDefault(x =>
                            string.Equals(x.Key, sourceMetadata.Key, strComparer));
                        if (fromDbMetadata != null)
                        {
                            rules = fromDbMetadata.Rules.Where(x => ValidationRuleIsValid(x, targetColumn.ClrTypeGroup, targetColumn.AllowNulls)).ToList();
                        }
                    }

                    var targetMetadata = targetColumn.ValidationMetadata.FirstOrDefault(x => string.Equals(x.Key, sourceMetadata.Key, strComparer));
                    if (rules.Any())
                    {
                        if (targetMetadata == null)
                        {
                            targetColumn.ValidationMetadata.Add(new DbColumnValidationMetadata
                            {
                                Key = sourceMetadata.Key,
                                Rules = rules
                            });
                        }
                        else
                        {
                            targetMetadata.Rules = rules;
                        }
                    }
                    else if (targetMetadata != null && !string.Equals(targetMetadata.Key, "default"))
                    {
                        targetColumn.ValidationMetadata.Remove(targetMetadata);
                    }
                }

                if (targetColumn.ValidationMetadata.All(x => x.Key != "default") && targetColumn.DbColumnTypeRef.HasValue)
                {
                    var colType = columnTypes.Find(x => x.Id == targetColumn.DbColumnTypeRef);
                    if (colType.ValidationMetadata != null)
                    {
                        targetColumn.ValidationMetadata.Insert(0, colType.ValidationMetadata);
                    }
                }
            }
        }

        private void ValidateValidationMetadata(List<DbTable> tables)
        {
            foreach (var col in tables.SelectMany(x => x.Columns))
            {
                foreach (var metadata in col.ValidationMetadata)
                {
                    metadata.Rules.RemoveAll(x => !ValidationRuleIsValid(x, col.ClrTypeGroup, col.AllowNulls));
                }

                col.ValidationMetadata.RemoveAll(x => !x.Rules.Any());
            }
        }

        private bool ValidationRuleIsValid(ValidationRule rule, ClrTypeGroup typeGroup, bool? isRequired = null)
        {
            switch (rule)
            {
                case RequiredValidationRule required:                      
                    return !isRequired.HasValue || required.IsSystem || required.Required == isRequired;
                case NumberRangeValidationRule _:
                    return typeGroup == ClrTypeGroup.Numeric;
                case DateRangeValidationRule _:
                    return typeGroup == ClrTypeGroup.Date;
                case InputFormatValidationRule _:
                    return typeGroup == ClrTypeGroup.String;
                case MaxLengthValidationRule _:
                    return typeGroup == ClrTypeGroup.String;
                default:
                    return false;
            }
        }

        private List<VirtualTreeNode> GetSyncedTree(List<VirtualTreeNode> folderNodes, List<DbDocToolEntity> folders, List<DbTable> syncedDbTables)
        {
            return folderNodes.Select(folder => GetSyncedFolder(folder, syncedDbTables, folder.ObjectRef == folders.First(x => x.Name == DbStructure.DefaultFolderName).Id)).ToList();
        }

        private VirtualTreeNode GetSyncedFolder(VirtualTreeNode folder, List<DbTable> syncedDBTables, bool isDefaultFolder)
        {
            folder.Children = GetSyncedTableNodes(folder.Children, syncedDBTables, isDefaultFolder);
            return folder;
        }

        private List<VirtualTreeNode> GetSyncedTableNodes(List<VirtualTreeNode> tables, List<DbTable> syncedDBTables, bool isDefault)
        {
            var result = new List<VirtualTreeNode>();

            tables.ForEach(table =>
            {
                var syncedTable = syncedDBTables.FirstOrDefault(x => x.Id.Equals(table.ObjectRef) && table.ItemType == DbItemType.Table);
                if (syncedTable != null)
                {
                    table.Children = GetSyncedColumnNodes(table.Children, syncedTable.Columns);
                    result.Add(table);
                }
            });

            if (isDefault)
            {
                result.AddRange(syncedDBTables.Where(x => !tables.Any(y => y.ObjectRef.Equals(x.Id))).Select(t => t.AsTreeNode()));
            }

            return result;
        }

        private List<VirtualTreeNode> GetSyncedColumnNodes(List<VirtualTreeNode> columns, List<DbColumn> syncedDBColumns)
        {
            var result = new List<VirtualTreeNode>();

            columns.ForEach(col =>
            {
                var syncedColumn = syncedDBColumns.FirstOrDefault(x => x.Id.Equals(col.ObjectRef) && col.ItemType == DbItemType.Column);
                if (syncedColumn != null)
                {
                    result.Add(col);
                }
            });

            result.AddRange(syncedDBColumns.Where(x => !columns.Any(y => y.ObjectRef.Equals(x.Id))).Select(x => x.AsTreeNode()));

            return result;
        }

        #endregion

        #region git

        public async Task<bool> SendToGit(string email, CancellationToken cancellationToken)
        {
            var obj = GetDbStructureFromDb();
            var json = SerializeDBStructureObj(obj);
            await gitlabService.Push("data/dbdoc/dbdoc", json, email, cancellationToken);
            return true;
        }

        #endregion

        #region get table dump provider

        public IDbTableDumpProvider GetDbTableDumpProvider(string clrType)
        {
            var dbContext = dbContextProvider.GetDbContexts(serviceProvider).FirstOrDefault(dbContext => dbContext != null && dbContext.Model.GetEntityTypesWithPrimaryKey().Any(x => x.ClrType.FullName == clrType));
            if (dbContext == null)
            {
                throw new ApplicationException($"DbDoc: There is no DbContext for the type {clrType}");
            }
            return new DbTableDumpProvider(dbContext, GetDbStructureFromDb().Tables);
        }

        #endregion
    }
}