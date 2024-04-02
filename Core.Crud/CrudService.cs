using AutoMapper;
using Core.Crud.Interfaces;
using Core.Data;
using Core.DTO;
using Core.Exceptions;
using Core.Filters;
using Core.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Core.Model;
using Microsoft.AspNetCore.Mvc;

namespace Core.Crud
{
    public class CrudService<TEntity, TEntityDTO, TKey> : DataService, ICrudService<TEntityDTO, TKey>, ISaveImplementationProvider<TEntity, TEntityDTO, TKey>
        where TEntity : class, IEntity<TKey>
        where TEntityDTO : class, IDTO<TKey>
        where TKey : IEquatable<TKey>
    {
        protected readonly IMapper Mapper;
        private IDataReader<TEntityDTO, TKey> DataReader { get; }
        private IDataWriter<TEntityDTO, TKey> DataWriter { get; }
        private IDataRemover<TKey> DataRemover { get; }

        public CrudService(IDbContext context, IMapper mapper, bool useMappingOnQueries = false) : this
            (
                context,
                mapper,
                new DataReader<TEntity, TEntityDTO, TKey>(context, mapper, useMappingOnQueries),
                new DataWriter<TEntity, TEntityDTO, TKey>(context, mapper),
                new DataRemover<TEntity, TKey>(context)
            )
        {
        }

        public CrudService(IDbContext context, IMapper mapper, IDataReader<TEntityDTO, TKey> dataReader, IDataWriter<TEntityDTO, TKey> dataWriter, IDataRemover<TKey> dataRemover) : base(context)
        {
            Mapper = mapper;
            DataReader = dataReader;
            DataWriter = dataWriter;
            DataRemover = dataRemover;
            (DataWriter as IConfigurableDataWriter<TEntity, TEntityDTO, TKey>).SetProvider(this);
        }

        #region Read Methods

        public bool UseMappingOnDb
        {
            get
            {
                return DataReader.UseMappingOnDb;
            }
            set
            {
                DataReader.UseMappingOnDb = value;
            }
        }

        public async Task<TEntityDTO> Get(
            TKey id,
            bool loadNavigationProperties = true,
            bool readOnly = true,
            CancellationToken cancellationToken = default)
        {
            ConfigureDataReaderInternal();
            return await DataReader.Get(id, loadNavigationProperties, readOnly, cancellationToken);
        }

        public async Task<TDTO> Get<TDTO>(TKey id,
            bool loadNavigationProperties = true,
            bool readOnly = true,
            CancellationToken cancellationToken = default) where TDTO : class, IDTO<TKey>
        {
            ConfigureDataReaderInternal();
            return await DataReader.Get<TDTO>(id, loadNavigationProperties, readOnly, cancellationToken);
        }

        public async Task<TEntityDTO> Get(TKey id, CancellationToken cancellationToken) => await Get(id, true, true, cancellationToken);

        public async Task<IEnumerable<TEntityDTO>> GetAll(
            bool loadNavigationProperties = true,
            bool readOnly = true,
            CancellationToken cancellationToken = default)
        {
            ConfigureDataReaderInternal();
            return await DataReader.GetAll(loadNavigationProperties, readOnly, cancellationToken);
        }

        public virtual async Task<IEnumerable<TEntityDTO>> GetBySite(int siteId, CancellationToken cancellationToken)
        {
            return await DataReader.GetBySite(siteId, cancellationToken);
        }

        

        public async Task<IEnumerable<TEntityDTO>> GetAll(CancellationToken cancellationToken)
            => await GetAll(true, true, cancellationToken);

        public async Task<IEnumerable<TEntityDTO>> GetAll(Filter filter, bool loadNavigationProperties = true, bool readOnly = true, CancellationToken cancellationToken = default)
        {
            ConfigureDataReaderInternal(filter);
            return await DataReader.GetAll(filter, loadNavigationProperties, readOnly, cancellationToken);
        }

        public async Task<IEnumerable<TEntityDTO>> GetAll(Filter filter, CancellationToken cancellationToken = default)
            => await GetAll(filter, true, true, cancellationToken);

        public async Task<bool> Exists(TKey id, CancellationToken cancellationToken = default)
            => await DataReader.Exists(id, cancellationToken);

        public async Task<bool> Exists(Expression<Func<TEntityDTO, bool>> expression, CancellationToken cancellationToken = default)
            => await DataReader.Exists(expression, cancellationToken);

        public async Task<TEntityDTO> Get(Expression<Func<TEntityDTO, bool>> expression, CancellationToken cancellationToken = default) =>
            await Get(expression, true, true, cancellationToken);

        public async Task<TEntityDTO> Get(Expression<Func<TEntityDTO, bool>> expression, bool loadNavigationProperties = true, bool readOnly = true, CancellationToken cancellationToken = default)
        {
            ConfigureDataReaderInternal();
            return await DataReader.Get(expression, loadNavigationProperties, readOnly, cancellationToken);
        }

        private void ConfigureDataReaderInternal()
        {
            var query = GetConfigurator().GetQueryable();
            query = ConfigureDataReader(query);
            GetConfigurator().SetQueryable(query);
        }

        private void ConfigureDataReaderInternal(Filter filter)
        {
            var query = GetConfigurator().GetQueryable();
            query = ConfigureDataReader(query);
            query = ApplyFilter(query, filter);
            GetConfigurator().SetQueryable(query);
        }

        private IDataReaderConfigurator<TEntity, TKey> GetConfigurator()
        {
            return (IDataReaderConfigurator<TEntity, TKey>)DataReader;
        }

        protected virtual IQueryable<TEntity> ConfigureDataReader(IQueryable<TEntity> entities) => entities;

        protected virtual IQueryable<TEntity> ApplyFilter(IQueryable<TEntity> query, Filter filter) => query;

        #endregion

        #region Edit Methods

        public async Task<TEntityDTO> Save(TEntityDTO dto, bool saveChanges = true, CancellationToken cancellationToken = default)
        {
            var validationResult = await IsValid(dto, cancellationToken);
            if (validationResult != ValidationResult.Success)
            {
                throw new ValidationException(validationResult, null, dto);
            }

            var result = await DataWriter.Save(await BeforeSave(dto, cancellationToken), saveChanges, cancellationToken);

            await AfterSave(result, cancellationToken);

            return result;
        }

        protected virtual Task<TEntityDTO> BeforeSave(TEntityDTO dto, CancellationToken cancellationToken) => Task.FromResult(dto);
        protected virtual Task AfterSave(TEntityDTO dto, CancellationToken cancellationToken) => Task.CompletedTask;

        protected virtual Task<ValidationResult> IsValid(TEntityDTO dto, CancellationToken cancellationToken) => Task.FromResult(ValidationResult.Success);

        public async Task<TEntityDTO> Save(TEntityDTO dto, CancellationToken cancellationToken) =>
            await Save(dto, true, cancellationToken);

        public virtual async Task Delete(TKey id, CancellationToken cancellationToken = default)
        {
            ConfigureDataRemover();
            try
            {
                await DataRemover.Delete(id, cancellationToken);
            }
            catch (DbUpdateException ex)
            {
                throw new BusinessException($"An error occurred while deleting record with Id: {id}. This record is used elsewhere in the system.", ex);
            }
            catch (Exception ex)
            {
                throw new BusinessException($"Could not delete record with Id: {id}.", ex);
            }
        }

        public async Task DeleteAll(CancellationToken cancellationToken = default)
        {
            ConfigureDataRemover();
            await DataRemover.DeleteAll(cancellationToken);
        }

        protected virtual Task ProcessBeforeDelete(TEntity entity, CancellationToken cancellationToken = default) => Task.CompletedTask;

        private void ConfigureDataRemover() => (DataRemover as IDataRemoverConfigurator<TEntity, TKey>).SetPreprocessor(ProcessBeforeDelete);

        protected virtual Task<TEntity> Save(TEntityDTO dto, TEntity entity, CancellationToken cancellationToken) => Task.FromResult(entity);

        async Task<TEntity> ISaveImplementationProvider<TEntity, TEntityDTO, TKey>.Save(TEntityDTO entityDTO, TEntity entity, CancellationToken cancellationToken)
        {
            return await Save(entityDTO, entity, cancellationToken);
        }

        #endregion
    }

    public class CrudService<TEntity, TEntityDTO> : CrudService<TEntity, TEntityDTO, int>, ICrudService<TEntityDTO>
        where TEntity : class, IEntity
        where TEntityDTO : class, IDTO<int>, IDTO
    {
        public CrudService(IDbContext context, IMapper mapper, bool useMappingOnQueries = false) : base(context, mapper, useMappingOnQueries)
        {
        }
    }
}