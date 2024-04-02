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
     * 
    public class HouseholdDTO: BaseDTO
    {
        public int CareHomeId { get; set; }

        [MaxLength(64)]
        [Required]
        public string Name { get; set; }
        public ICollection<FloorDTO> Floors { get; set; }
    }
     * 
     * 
    public class FloorDTO : BaseDTO
    {
        [Required]
        [MaxLength(32)]
        public string Name { get; set; }

        [Required]
        public int HouseholdId { get; set; }

        public HouseholdDTO Household { get; set; }

        public ICollection<TrolleyDTO> Trolleys { get; set; } = new List<TrolleyDTO>();
    }
     * 
     * 
     * 
    [Produces("application/json")]
    [Route("api/care-homes/{grandParentId}/households/{parentId}/floors")]
    [BindTo("grandParentId", nameof(FloorDTO.Household) + "." + nameof(HouseholdDTO.CareHomeId))]
    [BindTo("parentId", nameof(FloorDTO.HouseholdId))]
    public class CareHomeHouseholdFloorsController : GrandChildPagedCrudControllerBase<FloorDTO, FloorDTO, int, int, int>
    {
        public CareHomeHouseholdFloorsController(IFloorService service, ILogger<CareHomeHouseholdFloorsController> logger)
            : base(service, logger)
        {
        }

        protected override string GetGrandParentPropertyName()
        {
            return $"{nameof(Floor.Household)}.{nameof(HouseholdDTO.CareHomeId)}";
        }

        protected override string GetParentPropertyName()
        {
            return nameof(Floor.HouseholdId);
        }

        protected override bool IsValidDto(FloorDTO dto, int grandParentId, int parentId, int id)
        {
            return dto.Id == id && dto.HouseholdId == parentId;
        }

        protected override void SetParent(FloorDTO dto, int grandParentId, int parentId)
        {
            dto.HouseholdId = parentId;
        }
    }
     */
    public abstract class GrandChildPagedCrudControllerBase<TEntityDTO, TEntityListDTO, TKey, TParentKey, TGrandParentKey> : GrandChildCrudControllerBase<TEntityDTO, TKey, TParentKey, TGrandParentKey>
        where TEntityDTO : class, IDTO<TKey>
        where TEntityListDTO : class, IDTO<TKey>
        where TKey : IEquatable<TKey>
        where TParentKey : IEquatable<TParentKey>
        where TGrandParentKey : IEquatable<TParentKey>
    {
        protected IPagedCrudService<TEntityDTO, TEntityListDTO, TKey> PagedCrudService => CrudService as IPagedCrudService<TEntityDTO, TEntityListDTO, TKey>;
        protected GrandChildPagedCrudControllerBase(
            IPagedCrudService<TEntityDTO, TEntityListDTO, TKey> pagedCrudService,
            ILogger<GrandChildPagedCrudControllerBase<TEntityDTO, TEntityListDTO, TKey, TParentKey, TGrandParentKey>> logger)
            : base(pagedCrudService, logger)
        {
        }

        protected IPagedDataReader<TEntityDTO, TEntityListDTO, TKey> PagedDataReader => DataReader as IPagedDataReader<TEntityDTO, TEntityListDTO, TKey>;

        [HttpGet, Route("page")]
        public virtual async Task<IActionResult> GetPage([IdBinder] TGrandParentKey grandParentId, [IdBinder] TParentKey parentId, [FromQuery] FilterCommand command, CancellationToken cancellationToken = default)
        {
            AddFilterByParent(parentId, command);
            AddFilterByGrandParent(grandParentId, command);
            return Ok(await PagedDataReader.GetPage(command, cancellationToken));
        }

        protected async Task<IActionResult> GetPage<TDTO>(TGrandParentKey grandParentId, TParentKey parentId, FilterCommand command, bool loadNavigationProperties = true, bool readOnly = true, CancellationToken cancellationToken = default) where TDTO : class, IDTO<TKey>
        {
            AddFilterByParent(parentId, command);
            AddFilterByGrandParent(grandParentId, command);
            return Ok(await PagedDataReader.GetPage<TDTO>(command, loadNavigationProperties, readOnly, cancellationToken));
        }

        protected async Task<IActionResult> GetPage<TDTO>(FilterCommand command, bool loadNavigationProperties = true, bool readOnly = true, CancellationToken cancellationToken = default) where TDTO : class, IDTO<TKey>
        {
            return Ok(await PagedDataReader.GetPage<TDTO>(command, loadNavigationProperties, readOnly, cancellationToken));
        }
    }
}
