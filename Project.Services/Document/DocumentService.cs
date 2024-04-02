using AutoMapper;
using Core.Crud;
using Core.Data;
using Core.Filters;
using Microsoft.EntityFrameworkCore;

namespace Project.Services.Document
{
    public class DocumentService : PagedCrudService<Data.Model.Document, Data.DTO.DocumentDTO>, IDocumentService
    {
        IDbContext context;
        private readonly IMapper _mapper;

        public DocumentService(IDbContext context, IMapper mapper) : base(context, mapper)
        {
            this.context = context;
            _mapper = mapper;
        }
        protected override IQueryable<Data.Model.Document> ConfigureDataReader(IQueryable<Data.Model.Document> entities) =>
            entities;

        protected override IQueryable<Data.Model.Document> ApplyFilter(IQueryable<Data.Model.Document> query, Filter filter)
        {

            var searchStringFilter = filter.Filters.FirstOrDefault(x => x.PropertyName == "path");

            if (searchStringFilter != null)
            {
                var searchString = ((StringFilter)searchStringFilter)?.Value;
                if (!string.IsNullOrWhiteSpace(searchString))
                {
                    query = query;//.Where(x => x.Pproj.Description.Contains(searchString) || x.Pproj.Project.Contains(searchString));

                    filter.Filters.Remove(searchStringFilter);
                }
            }

            return base.ApplyFilter(query, filter);
        }

        protected override async Task<Project.Data.DTO.DocumentDTO> BeforeSave(Project.Data.DTO.DocumentDTO dto, CancellationToken cancellationToken)
        {
            if (dto.ProjectId > 0)
            {
                //var pprojDTO = await this._pprojServiceHelper.GetShortDto(dto.ProjectId);
                //pprojDTO?.CheckIsReadOnly();
            }

            return await base.BeforeSave(dto, cancellationToken);
        }

        protected override async Task ProcessBeforeDelete(Data.Model.Document entity, CancellationToken cancellationToken = default)
        {
            if (entity.PprojId.HasValue)
            {
                //var pprojDTO = await this._pprojServiceHelper.GetShortDto((int)entity.PprojId);
                //pprojDTO?.CheckIsReadOnly();
            }

            await base.ProcessBeforeDelete(entity, cancellationToken);
        }

        public async Task CopyDocumentsDependant(int? originalPprojId, int copyPprojId, CancellationToken cancellationToken = default)
        {
            var documentList = await context.Set<Data.Model.Document>().AsNoTracking().Where(p => p.PprojId == originalPprojId).ToListAsync();
            documentList.ForEach(async documentOriginal =>
           {
               context.Entry(documentOriginal).State = EntityState.Detached;
               var newDocument = documentOriginal.Clone();
               newDocument.Id = 0;
               newDocument.PprojId = copyPprojId;
               await context.Set<Data.Model.Document>().AddAsync(newDocument, cancellationToken);
               await context.SaveChangesAsync(cancellationToken);
           });
        }
    }
}
