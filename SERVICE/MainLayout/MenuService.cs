using CORE.SERVICE.MainLayout.Module;
using Dapper;
using System.Data.SqlClient;

namespace CORE.SERVICE.MainLayout
{
    public class MenuService
    {
        private readonly string connectionString;

        public MenuService(string connectionString)
        {
            this.connectionString = connectionString;
        }

        public async Task<IEnumerable<MenuItem>> GetMainMenuItemsAsync()
        {
            using (var connection = new SqlConnection(connectionString))
            {
                return await connection.QueryAsync<MenuItem>("SELECT * FROM SchoolManagementSecurity.MainMenu");
            }
        }

        public async Task<IEnumerable<SubMenuItem>> GetSubMenuItemsAsync(int mainMenuId)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                return await connection.QueryAsync<SubMenuItem>("SELECT * FROM SchoolManagementSecurity.SubMenu WHERE MainMenuID = @MainMenuID", new { MainMenuID = mainMenuId });
            }
        }

        public async Task AddMainMenuItemAsync(MenuItem menuItem)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                var query = "INSERT INTO SchoolManagementSecurity.MainMenu (Text, Path, Icon) VALUES (@Text, @Path, @Icon)";
                await connection.ExecuteAsync(query, menuItem);
            }
        }

        public async Task AddSubMenuItemAsync(SubMenuItem subMenuItem)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                var query = "INSERT INTO SchoolManagementSecurity.SubMenu (MainMenuID, Text, Path, Icon) VALUES (@MainMenuID, @Text, @Path, @Icon)";
                await connection.ExecuteAsync(query, subMenuItem);
            }
        }

        public async Task UpdateMainMenuItemAsync(MenuItem menuItem)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                var query = "UPDATE SchoolManagementSecurity.MainMenu SET Text = @Text, Path = @Path, Icon = @Icon WHERE MenuID = @MenuID";
                await connection.ExecuteAsync(query, menuItem);
            }
        }

        public async Task UpdateSubMenuItemAsync(SubMenuItem subMenuItem)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                var query = "UPDATE SchoolManagementSecurity.SubMenu SET Text = @Text, Path = @Path, Icon = @Icon WHERE SubMenuID = @SubMenuID";
                await connection.ExecuteAsync(query, subMenuItem);
            }
        }

        public async Task DeleteMainMenuItemAsync(int menuId)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                var query = "DELETE FROM SchoolManagementSecurity.MainMenu WHERE MenuID = @MenuID";
                await connection.ExecuteAsync(query, new { MenuID = menuId });
            }
        }

        public async Task DeleteSubMenuItemAsync(int subMenuId)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                var query = "DELETE FROM SchoolManagementSecurity.SubMenu WHERE SubMenuID = @SubMenuID";
                await connection.ExecuteAsync(query, new { SubMenuID = subMenuId });
            }
        }
    }
}