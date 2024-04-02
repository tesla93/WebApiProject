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
    public abstract class ChildPagedDataControllerBase<TEntityDTO, TKey, TParentKey> : Core.Web.ControllerBase
        where TEntityDTO : class, IDTO<TKey>
        where TKey : IEquatable<TKey>
        where TParentKey : IEquatable<TParentKey>
    {
        protected ChildPagedDataControllerBase(IPagedDataReader<TEntityDTO, TKey> pagedDataReader, ILogger<PagedDataControllerBase<TEntityDTO, TKey>> logger) : base(logger)
        {
            PagedDataReader = pagedDataReader;
        }

        protected IPagedDataReader<TEntityDTO, TKey> PagedDataReader { get; }

        [HttpGet, Route("page")]
        public virtual async Task<IActionResult> GetPage([IdBinder] TParentKey parentId, [FromQuery] FilterCommand command, CancellationToken cancellationToken = default)
        {
            AddFilterByParent(parentId, command);
            return Ok(await PagedDataReader.GetPage(command, cancellationToken));
        }

        [HttpGet, Route("{id}")]
        public virtual async Task<IActionResult> Get([IdBinder] TParentKey parentId, [IdBinder] TKey id, CancellationToken cancellationToken = default)
        {
            var result = await PagedDataReader.Get(id, cancellationToken);

            if (result == null)
                throw new EntityNotFoundException();

            return Ok(result);
        }

        protected async Task<IActionResult> GetPage<TDTO>([IdBinder] TParentKey parentId, FilterCommand command, bool loadNavigationProperties = true, bool readOnly = true, CancellationToken cancellationToken = default) where TDTO : class, IDTO<TKey>
        {
            AddFilterByParent(parentId, command);
            return Ok(await PagedDataReader.GetPage<TDTO>(command, loadNavigationProperties, readOnly, cancellationToken));
        }

        protected async Task<IActionResult> GetPage<TDTO>(FilterCommand command, bool loadNavigationProperties = true, bool readOnly = true, CancellationToken cancellationToken = default) where TDTO : class, IDTO<TKey>
        {
            return Ok(await PagedDataReader.GetPage<TDTO>(command, loadNavigationProperties, readOnly, cancellationToken));
        }

        protected void AddFilterByParent(TParentKey parentId, Filter command)
        {
            command.Filters.Add(new ObjectReferenceFilter { PropertyName = GetParentPropertyName(), Value = parentId });
        }

        protected abstract string GetParentPropertyName();
    }

    public abstract class ChildPagedDataControllerBase<TEntityDTO> : ChildPagedDataControllerBase<TEntityDTO, int, int>
        where TEntityDTO : class, IDTO
    {
        protected ChildPagedDataControllerBase(
            IPagedDataReader<TEntityDTO, int> pagedDataReader,
            ILogger<PagedDataControllerBase<TEntityDTO>> logger) : base(pagedDataReader, logger)
        {
        }
    }
}