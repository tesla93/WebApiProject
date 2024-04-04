using System.Collections.Generic;
using Module.Menu;
using Module.Menu.DTO;

namespace Module.DbDoc
{
    public class MenuModuleLinkage : IMenuModuleLinkage
    {
        public void CreateInitialMenuItems(List<MenuDTO> menu, MenuLinkageRootMenus rootMenus) =>
            rootMenus.TechnicalAdmin.Children.Add(
                new MenuDTO
                {
                    Label = "DB Documenting Tool",
                    Icon = "grid_on",
                    Children = new List<MenuDTO>
                    {
                        new MenuDTO(Routes.DbExplorer),
                        new MenuDTO(Routes.ColumnTypes)
                    }
                });
    }
}
