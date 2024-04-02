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
    public abstract class ChildCrudControllerBase<TEntityDTO, TKey, TParentKey> : Core.Web.ControllerBase
        where TEntityDTO : class, IDTO<TKey>
        where TKey : IEquatable<TKey>
        where TParentKey : IEquatable<TParentKey>
    {
        protected readonly IDataReader<TEntityDTO, TKey> DataReader;
        private readonly IDataWriter<TEntityDTO, TKey> DataWriter;
        private readonly IDataRemover<TKey> DataRemover;
        private readonly IDataService DataService;

        protected ICrudService<TEntityDTO, TKey> CrudService { get; }

        protected ChildCrudControllerBase(
            ICrudService<TEntityDTO, TKey> crudService,
            ILogger<ChildCrudControllerBase<TEntityDTO, TKey, TParentKey>> logger) : this(crudService, crudService, crudService, crudService, logger)
        {
            CrudService = crudService;
        }

        private ChildCrudControllerBase(
            IDataReader<TEntityDTO, TKey> dataReader,
            IDataWriter<TEntityDTO, TKey> dataWriter,
            IDataRemover<TKey> dataRemover,
            IDataService dataService,
            ILogger<ChildCrudControllerBase<TEntityDTO, TKey, TParentKey>> logger) : base(logger)
        {
            DataReader = dataReader;
            DataWriter = dataWriter;
            DataRemover = dataRemover;
            DataService = dataService;
        }

        [HttpGet, Route("{id}")]
        public virtual async Task<IActionResult> Get([IdBinder] TParentKey parentId, [IdBinder] TKey id, CancellationToken cancellationToken = default)
        {
            var result = await DataReader.Get(id, cancellationToken);

            if (result == null)
                throw new EntityNotFoundException();

            return Ok(result);
        }


        [HttpPost]
        public virtual async Task<IActionResult> Create([IdBinder] TParentKey parentId, [FromBody] TEntityDTO dto, [FromServices] IModelHashingService modelHashingService, CancellationToken cancellationToken = default)
        {
            SetParent(dto, parentId);
            var result = await DataWriter.Save(dto, cancellationToken);
            await AfterWriteCrudAction();
            return Created<TEntityDTO, TKey>(result, modelHashingService);
        }

        [HttpPut, Route("{id}")]
        public virtual async Task<IActionResult> Update([IdBinder] TParentKey parentId, [FromBody] TEntityDTO dto, [IdBinder] TKey id, CancellationToken cancellationToken = default)
        {
            if (!IsValidDto(dto, id, parentId))
            {
                throw new BusinessException("dto.id != id or dto.parentId != parentId");
            }
            if (!await DataReader.Exists(id, cancellationToken))
                throw new EntityNotFoundException();

            var result = await DataWriter.Save(dto, cancellationToken);
            await AfterWriteCrudAction();
            return Ok(result);
        }

        [HttpDelete, Route("{id}")]
        public virtual async Task<IActionResult> Delete([IdBinder] TParentKey parentId, [IdBinder] TKey id, CancellationToken cancellationToken = default)
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

        protected abstract string GetParentPropertyName();

        protected abstract void SetParent(TEntityDTO dto, TParentKey parentId);
        protected abstract bool IsValidDto(TEntityDTO dto, TKey id, TParentKey parentId);
    }

    public abstract class ChildCrudControllerBase<TEntityDTO> : ChildCrudControllerBase<TEntityDTO, int, int>
        where TEntityDTO : class, IDTO
    {
        protected ChildCrudControllerBase(
            ICrudService<TEntityDTO, int> crudService,
            ILogger<ChildCrudControllerBase<TEntityDTO>> logger) : base(crudService, logger)
        {
        }
    }
}
