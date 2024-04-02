using Core.Crud.Interfaces;
using Project.Data.DTO;
using System.Threading;
using System.Threading.Tasks;

namespace Project.Services.Document
{
    public interface IDocumentService : IPagedCrudService<DocumentDTO>
    {
        Task CopyDocumentsDependant(int? origianlPprojId, int copyPprojId, CancellationToken cancellationToken = default);
    }
}
