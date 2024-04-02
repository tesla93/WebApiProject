using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Core.Filters;
using Core.Web.ModelBinders;
using Core.DTO;
using Core.Crud.Interfaces;

namespace Core.Crud
{
    /*
    The server-side sample:

    [BindRouteParameterToDtoProperty("parentId", nameof(TrolleyDTO.CareHomeId))]
    [Produces("application/json")]
    [Route("api/care-homes/{parentId}/trolleys")]
    public class CareHomeTrolleysController : ChildPagedCrudControllerBase<TrolleyDTO>
    {
        public CareHomeTrolleysController(ITrolleyService service, ILogger<CareHomeTrolleysController> logger)
            : base(service, logger)
        {
        }

        protected override string GetParentPropertyName()
        {
            return nameof(TrolleyDTO.CareHomeId);
        }

        protected override bool IsValidDto(TrolleyDTO dto, int id, int parentId)
        {
            return dto.Id == id && dto.CareHomeId == parentId;
        }

        protected override void SetParent(TrolleyDTO dto, int parentId)
        {
            dto.CareHomeId = parentId;
        }
    }

     */
    public abstract class ChildPagedCrudControllerBase<TEntityDTO, TEntityListDTO, TKey, TParentKey> : ChildCrudControllerBase<TEntityDTO, TKey, TParentKey>
        where TEntityDTO : class, IDTO<TKey>
        where TEntityListDTO : class, IDTO<TKey>
        where TKey : IEquatable<TKey>
        where TParentKey : IEquatable<TParentKey>
    {
        protected IPagedCrudService<TEntityDTO, TEntityListDTO, TKey> PagedCrudService => CrudService as IPagedCrudService<TEntityDTO, TEntityListDTO, TKey>;
        protected ChildPagedCrudControllerBase(
            IPagedCrudService<TEntityDTO, TEntityListDTO, TKey> pagedCrudService,
            ILogger<ChildPagedCrudControllerBase<TEntityDTO, TEntityListDTO, TKey, TParentKey>> logger)
            : base(pagedCrudService, logger)
        {
        }

        protected IPagedDataReader<TEntityDTO, TEntityListDTO, TKey> PagedDataReader => DataReader as IPagedDataReader<TEntityDTO, TEntityListDTO, TKey>;

        [HttpGet, Route("page")]
        public virtual async Task<IActionResult> GetPage([IdBinder] TParentKey parentId, [FromQuery] FilterCommand command, CancellationToken cancellationToken = default)
        {
            AddFilterByParent(parentId, command);
            return Ok(await PagedDataReader.GetPage(command, cancellationToken));
        }

        protected async Task<IActionResult> GetPage<TDTO>(TParentKey parentId, FilterCommand command, bool loadNavigationProperties = true, bool readOnly = true, CancellationToken cancellationToken = default) where TDTO : class, IDTO<TKey>
        {
            AddFilterByParent(parentId, command);
            return Ok(await PagedDataReader.GetPage<TDTO>(command, loadNavigationProperties, readOnly, cancellationToken));
        }

        protected async Task<IActionResult> GetPage<TDTO>(FilterCommand command, bool loadNavigationProperties = true, bool readOnly = true, CancellationToken cancellationToken = default) where TDTO : class, IDTO<TKey>
        {
            return Ok(await PagedDataReader.GetPage<TDTO>(command, loadNavigationProperties, readOnly, cancellationToken));
        }
    }

    public abstract class ChildPagedCrudControllerBase<TEntityDTO, TKey, TParentKey> : ChildPagedCrudControllerBase<TEntityDTO, TEntityDTO, TKey, TParentKey>
       where TEntityDTO : class, IDTO<TKey>
       where TKey : IEquatable<TKey>
       where TParentKey : IEquatable<TParentKey>
    {
        protected ChildPagedCrudControllerBase(IPagedCrudService<TEntityDTO, TKey> gridService, ILogger<ChildPagedCrudControllerBase<TEntityDTO, TKey, TParentKey>> logger) : base(gridService, logger)
        {
        }
    }

    public abstract class ChildPagedCrudControllerBase<TEntityDTO, TEntityListDTO> : ChildPagedCrudControllerBase<TEntityDTO, TEntityListDTO, int, int>
       where TEntityDTO : class, IDTO<int>, IDTO
       where TEntityListDTO : class, IDTO<int>, IDTO
    {
        protected ChildPagedCrudControllerBase(
            IPagedCrudService<TEntityDTO, TEntityListDTO, int> pagedCrudService,
            ILogger<ChildPagedCrudControllerBase<TEntityDTO, TEntityListDTO>> logger) : base(pagedCrudService, logger)
        { }
    }

    public abstract class PagedChildCrudControllerBase<TEntityDTO> : ChildPagedCrudControllerBase<TEntityDTO, int, int>
        where TEntityDTO : class, IDTO
    {
        protected PagedChildCrudControllerBase(
            IPagedCrudService<TEntityDTO> pagedCrudService,
            ILogger<PagedChildCrudControllerBase<TEntityDTO>> logger) : base(pagedCrudService, logger)
        { }
    }
}
