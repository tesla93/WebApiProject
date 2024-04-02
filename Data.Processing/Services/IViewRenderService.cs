using System.Threading.Tasks;

namespace DataProcessing.Services
{
    public interface IViewRenderService
    {
        Task<string> RenderToString(string viewName, object model);
    }

}