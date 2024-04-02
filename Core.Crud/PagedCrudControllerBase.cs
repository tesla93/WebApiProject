using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Core.Filters;
using Core.DTO;
using Core.Crud.Interfaces;

namespace Core.Crud
{
    public abstract class PagedCrudControllerBase<TEntityDTO, TListEntityDTO, TKey> : CrudControllerBase<TEntityDTO, TKey>
        where TEntityDTO : class, IDTO<TKey>
        where TListEntityDTO : class, IDTO<TKey>
        where TKey : IEquatable<TKey>
    {
        protected IPagedCrudService<TEntityDTO, TListEntityDTO, TKey> PagedCrudService => CrudService as IPagedCrudService<TEntityDTO, TListEntityDTO, TKey>;
        protected PagedCrudControllerBase(
            IPagedCrudService<TEntityDTO, TListEntityDTO, TKey> pagedCrudService,
            ILogger<PagedCrudControllerBase<TEntityDTO, TListEntityDTO, TKey>> logger)
            :base(pagedCrudService, logger)
        {
        }

        protected IPagedDataReader<TEntityDTO, TListEntityDTO, TKey> PagedDataReader => DataReader as IPagedDataReader<TEntityDTO, TListEntityDTO, TKey>;

        [HttpGet, Route("page")]
        public virtual async Task<IActionResult> GetPage([FromQuery] FilterCommand command, CancellationToken cancellationToken = default)
        {
            return Ok(await PagedDataReader.GetPage(command, cancellationToken));
        }

        protected async Task<IActionResult> GetPage<TDTO>(FilterCommand command, bool loadNavigationProperties = true, bool readOnly = true, CancellationToken cancellationToken = default) where TDTO : class, IDTO<TKey>
        {
            return Ok(await PagedDataReader.GetPage<TDTO>(command, loadNavigationProperties, readOnly, cancellationToken));
        }
    }

    public abstract class PagedCrudControllerBase<TEntityDTO, TKey> : PagedCrudControllerBase<TEntityDTO, TEntityDTO, TKey>
       where TEntityDTO : class, IDTO<TKey>
       where TKey : IEquatable<TKey>
    {
        protected PagedCrudControllerBase(IPagedCrudService<TEntityDTO, TKey> gridService, ILogger<PagedCrudControllerBase<TEntityDTO, TKey>> logger) : base(gridService, logger)
        {
        }
    }

    public abstract class PagedCrudControllerBase<TEntityDTO> : PagedCrudControllerBase<TEntityDTO, int>
        where TEntityDTO : class, IDTO
    {
        protected PagedCrudControllerBase(
            IPagedCrudService<TEntityDTO> pagedCrudService,
            ILogger<PagedCrudControllerBase<TEntityDTO>> logger) : base(pagedCrudService, logger)
        {}
    }
}
