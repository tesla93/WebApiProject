using Module.Core.Web;

namespace Module.DbDoc
{
    public class Routes
    {
        public static Route DbExplorer = new Route("/app/dbdoc", "Database Explorer");
        public static Route ColumnTypes = new Route("/app/dbdoc/column-types", "Custom Column Types", "view_column");
    }
}
