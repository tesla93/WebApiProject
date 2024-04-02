using Core.Crud.Interfaces;
using Core.DTO;
using Core.Exceptions;
using Core.Filters;
using Core.Web.ModelBinders;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Core.Crud
{
    [ApiController]
    public abstract class PagedDataControllerBase<TEntityDTO, TKey> : Core.Web.ControllerBase
        where TEntityDTO : class, IDTO<TKey>
        where TKey : IEquatable<TKey>
    {
        protected PagedDataControllerBase(IPagedDataReader<TEntityDTO, TKey> pagedDataReader, ILogger<PagedDataControllerBase<TEntityDTO, TKey>> logger) : base(logger)
        {
            PagedDataReader = pagedDataReader;
        }

        protected IPagedDataReader<TEntityDTO, TKey> PagedDataReader { get; }

        [HttpGet, Route("page")]
        public virtual async Task<IActionResult> GetPage([FromQuery] FilterCommand command, CancellationToken cancellationToken = default)
        {
            return Ok(await PagedDataReader.GetPage(command, cancellationToken));
        }

        [HttpGet, Route("{id}")]
        public virtual async Task<IActionResult> Get([IdBinder] TKey id, CancellationToken cancellationToken = default)
        {
            var result = await PagedDataReader.Get(id, cancellationToken);

            if (result == null)
                throw new EntityNotFoundException();

            return Ok(result);
        }

        protected async Task<IActionResult> GetPage<TDTO>(FilterCommand command, bool loadNavigationProperties = true, bool readOnly = true, CancellationToken cancellationToken = default) where TDTO : class, IDTO<TKey>
        {
            return Ok(await PagedDataReader.GetPage<TDTO>(command, loadNavigationProperties, readOnly, cancellationToken));
        }
    }

    public abstract class PagedDataControllerBase<TEntityDTO> : PagedDataControllerBase<TEntityDTO, int>
        where TEntityDTO : class, IDTO
    {
        protected PagedDataControllerBase(
            IPagedDataReader<TEntityDTO, int> pagedDataReader,
            ILogger<PagedDataControllerBase<TEntityDTO>> logger) : base(pagedDataReader, logger)
        {
        }
    }
}