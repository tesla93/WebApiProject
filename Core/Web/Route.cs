using System.IO;
using System.Linq;

namespace BBWM.Core.Web
{
    public class Route
    {
        public Route(string path, string title, string icon = "")
        {
            Path = path;
            Title = title;
            MenuIcon = icon;
        }

        public string Path { get; set; }

        public string Title { get; set; }

        public string MenuIcon { get; set; }
    }

    public class RouteBuilder
    {
        private readonly string _basePath;
        private readonly RouteBuilder _parent;

        public RouteBuilder(string basePath, RouteBuilder parent = null)
        {
            _basePath = basePath;
            _parent = parent;
        }

        public Route Build(string path, string title, string icon = "")
        {
            string combinedPath = CombinePaths(_basePath, path);

            return _parent != null
                ? _parent.Build(combinedPath, title, icon)
                : new Route(combinedPath, title, icon);
        }

        private string CombinePaths(params string[] pathParts)
            => $"/{string.Join('/', pathParts.Select(x => x.Trim('/', '\\')))}";
    }
}