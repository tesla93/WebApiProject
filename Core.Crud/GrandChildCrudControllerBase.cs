using Core.Crud.Interfaces;
using Core.DTO;
using Core.Exceptions;
using Core.Filters;
using Core.ModelHashing;
using Core.Services;
using Core.Web.ModelBinders;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Core.Crud
{
    // TODO: parameter validation
    [ApiController]
    public abstract class GrandChildCrudControllerBase<TEntityDTO, TKey, TParentKey, TGrandParentKey> : Core.Web.ControllerBase
        where TEntityDTO : class, IDTO<TKey>
        where TKey : IEquatable<TKey>
        where TParentKey : IEquatable<TParentKey>
        where TGrandParentKey : IEquatable<TParentKey>
    {
        protected readonly IDataReader<TEntityDTO, TKey> DataReader;
        private readonly IDataWriter<TEntityDTO, TKey> DataWriter;
        private readonly IDataRemover<TKey> DataRemover;
        private readonly IDataService DataService;

        protected ICrudService<TEntityDTO, TKey> CrudService { get; }

        protected GrandChildCrudControllerBase(
            ICrudService<TEntityDTO, TKey> crudService,
            ILogger<GrandChildCrudControllerBase<TEntityDTO, TKey, TParentKey, TGrandParentKey>> logger) : this(crudService, crudService, crudService, crudService, logger)
        {
            CrudService = crudService;
        }

        private GrandChildCrudControllerBase(
            IDataReader<TEntityDTO, TKey> dataReader,
            IDataWriter<TEntityDTO, TKey> dataWriter,
            IDataRemover<TKey> dataRemover,
            IDataService dataService,
            ILogger<GrandChildCrudControllerBase<TEntityDTO, TKey, TParentKey, TGrandParentKey>> logger) : base(logger)
        {
            DataReader = dataReader;
            DataWriter = dataWriter;
            DataRemover = dataRemover;
            DataService = dataService;
        }

        [HttpGet, Route("{id}")]
        public virtual async Task<IActionResult> Get([IdBinder] TGrandParentKey grandParentId, [IdBinder] TParentKey parentId, [IdBinder] TKey id, CancellationToken cancellationToken = default)
        {
            var result = await DataReader.Get(id, cancellationToken);

            if (result == null)
                throw new EntityNotFoundException();

            return Ok(result);
        }


        [HttpPost]
        public virtual async Task<IActionResult> Create([IdBinder] TGrandParentKey grandParentId, [IdBinder] TParentKey parentId, [FromBody] TEntityDTO dto, [FromServices] IModelHashingService modelHashingService, CancellationToken cancellationToken = default)
        {
            SetParent(dto, grandParentId, parentId);
            var result = await DataWriter.Save(dto, cancellationToken);
            await AfterWriteCrudAction();
            return Created<TEntityDTO, TKey>(result, modelHashingService);
        }

        [HttpPut, Route("{id}")]
        public virtual async Task<IActionResult> Update([IdBinder] TGrandParentKey grandParentId, [IdBinder] TParentKey parentId, [FromBody] TEntityDTO dto, [IdBinder] TKey id, CancellationToken cancellationToken = default)
        {
            if (!IsValidDto(dto, grandParentId, parentId, id))
            {
                throw new BusinessException("dto.id != id or dto.parentId != parentId or dto.grandParentId != grandParentId");
            }
            if (!await DataReader.Exists(id, cancellationToken))
                throw new EntityNotFoundException();

            var result = await DataWriter.Save(dto, cancellationToken);
            await AfterWriteCrudAction();
            return Ok(result);
        }

        [HttpDelete, Route("{id}")]
        public virtual async Task<IActionResult> Delete([IdBinder] TGrandParentKey grandParentId, [IdBinder] TParentKey parentId, [IdBinder] TKey id, CancellationToken cancellationToken = default)
        {
            if (!await DataReader.Exists(id, cancellationToken))
                throw new EntityNotFoundException();

            await DataRemover.Delete(id, cancellationToken);
            await AfterWriteCrudAction(cancellationToken);
            return Ok(id);
        }

        protected async Task<IActionResult> GetPageFromReader<TDTO>(IPagedDataReader<TEntityDTO, TDTO, TKey> pagedDataReader, FilterCommand command, CancellationToken cancellationToken) where TDTO : class, IDTO<TKey>
        {
            if (pagedDataReader == null)
                throw new ActionNotImplementedException();
            return Ok(await pagedDataReader.GetPage(command, cancellationToken));
        }

        protected virtual async Task AfterWriteCrudAction(CancellationToken cancellationToken = default) => await Task.CompletedTask;

        protected void AddFilterByParent(TParentKey parentId, Filter command)
        {
            command.Filters.Add(new ObjectReferenceFilter { PropertyName = GetParentPropertyName(), Value = parentId });
        }

        protected void AddFilterByGrandParent(TGrandParentKey grandParentId, Filter command)
        {
            command.Filters.Add(new ObjectReferenceFilter { PropertyName = GetGrandParentPropertyName(), Value = grandParentId });
        }

        protected abstract string GetGrandParentPropertyName();
        protected abstract string GetParentPropertyName();

        protected abstract void SetParent(TEntityDTO dto, TGrandParentKey grandParentId, TParentKey parentId);
        protected abstract bool IsValidDto(TEntityDTO dto, TGrandParentKey grandParentId, TParentKey parentId, TKey id);
    }
}
