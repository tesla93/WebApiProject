using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Core.Exceptions;
using Core.Filters;
using Core.ModelHashing;
using Core.Web.ModelBinders;
using Core.Services;
using Core.DTO;
using Core.Crud.Interfaces;

namespace Core.Crud
{
    [ApiController]
    public abstract class CrudControllerBase<TEntityDTO, TKey> : Core.Web.ControllerBase
        where TEntityDTO : class, IDTO<TKey>
        where TKey : IEquatable<TKey>
    {
        protected readonly IDataReader<TEntityDTO, TKey> DataReader;
        private readonly IDataWriter<TEntityDTO, TKey> DataWriter;
        private readonly IDataRemover<TKey> DataRemover;
        private readonly IDataService DataService;

        protected ICrudService<TEntityDTO, TKey> CrudService { get; }

        protected CrudControllerBase(
            ICrudService<TEntityDTO, TKey> crudService,
            ILogger<CrudControllerBase<TEntityDTO, TKey>> logger) : this(crudService, crudService, crudService, crudService, logger)
        {
            CrudService = crudService;
        }

        private CrudControllerBase(
            IDataReader<TEntityDTO, TKey> dataReader,
            IDataWriter<TEntityDTO, TKey> dataWriter,
            IDataRemover<TKey> dataRemover,
            IDataService dataService,
            ILogger<CrudControllerBase<TEntityDTO, TKey>> logger) : base(logger)
        {
            DataReader = dataReader;
            DataWriter = dataWriter;
            DataRemover = dataRemover;
            DataService = dataService;
        }

        [HttpGet("GetBySite/{siteId}")]
        public virtual async Task<IActionResult> GetBySite(int siteId, CancellationToken cancellationToken = default)
        {
            var result = await DataReader.GetBySite(siteId, cancellationToken);

            if (result == null)
                throw new EntityNotFoundException();

            return Ok(result);
        }

       
        [HttpGet]
        public virtual async Task<IActionResult> Get(CancellationToken cancellationToken = default)
        {
            var result = await DataReader.GetAll(cancellationToken);

            if (result == null)
                throw new EntityNotFoundException();

            return Ok(result);
        }

        [HttpGet, Route("{id}")]
        public virtual async Task<IActionResult> Get([IdBinder] TKey id, CancellationToken cancellationToken = default)
        {
            var result = await DataReader.Get(id, cancellationToken);

            if (result == null)
                throw new EntityNotFoundException();

            return Ok(result);
        }

        [HttpPost]
        public virtual async Task<IActionResult> Create([FromBody] TEntityDTO dto, [FromServices] IModelHashingService modelHashingService, CancellationToken cancellationToken = default)
        {
            var result = await DataWriter.Save(dto, cancellationToken);
            await AfterWriteCrudAction();
            return Created<TEntityDTO, TKey>(result, modelHashingService);
        }

        [HttpPut, Route("{id}")]
        public virtual async Task<IActionResult> Update([FromBody] TEntityDTO dto, [IdBinder] TKey id, CancellationToken cancellationToken = default)
        {
            if (!await DataReader.Exists(id, cancellationToken))
                throw new EntityNotFoundException();

            var result = await DataWriter.Save(dto, cancellationToken);
            await AfterWriteCrudAction();
            return Ok(result);
        }

        [HttpDelete, Route("{id}")]
        public virtual async Task<IActionResult> Delete([IdBinder] TKey id, CancellationToken cancellationToken = default)
        {
            if (!await DataReader.Exists(id, cancellationToken))
                throw new EntityNotFoundException();

            await DataRemover.Delete(id, cancellationToken);
            await AfterWriteCrudAction(cancellationToken);
            return Ok(id);
        }

        protected async Task<IActionResult> GetPageFromReader<TDTO>(IPagedDataReader<TEntityDTO, TDTO, TKey> pagedDataReader, FilterCommand command, CancellationToken cancellationToken) where TDTO: class, IDTO<TKey>
        {
            if (pagedDataReader == null)
                throw new ActionNotImplementedException();
            return Ok(await pagedDataReader.GetPage(command, cancellationToken));
        }

        protected virtual async Task AfterWriteCrudAction(CancellationToken cancellationToken = default) => await Task.CompletedTask;
    }

    public abstract class CrudControllerBase<TEntityDTO> : CrudControllerBase<TEntityDTO, int>
        where TEntityDTO : class, IDTO
    {
        protected CrudControllerBase(
            ICrudService<TEntityDTO, int> crudService,
            ILogger<CrudControllerBase<TEntityDTO>> logger) : base(crudService, logger)
        {
        }
    }
}