using CORE.MODEL;
using System.Data.SqlClient;
using System.Data;
using Dapper;
using CORE.SERVICE.MainLayout.Module;

namespace CORE.SERVICE.MainLayout
{
    public class UserService
    {
        private readonly IConfiguration _configuration;

        public UserService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        private IDbConnection CreateConnection()
        {
            return new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
        }

        public async Task<IEnumerable<User>> GetUsersAsync()
        {
            using (var connection = CreateConnection())
            {
                return await connection.QueryAsync<User>("SELECT * FROM SchoolManagement.Users");
            }
        }

        public async Task<IEnumerable<Role>> GetRolesAsync()
        {
            using (var connection = CreateConnection())
            {
                return await connection.QueryAsync<Role>("SELECT * FROM SchoolManagementSecurity.MainSystemRoles");
            }
        }

        public async Task<IEnumerable<MenuAccess>> GetMenuAccessAsync(int roleId)
        {
            using (var connection = CreateConnection())
            {
                var sql = @"
                SELECT ma.RoleID, ma.MenuID, ma.CanAccess, mm.Text AS MenuName 
                FROM SchoolManagementSecurity.MenuAccess ma
                JOIN SchoolManagementSecurity.MainMenu mm ON ma.MenuID = mm.MenuID
                WHERE ma.RoleID = @RoleID";

                return await connection.QueryAsync<MenuAccess>(sql, new { RoleID = roleId });
            }
        }

        public async Task UpdateMenuAccessAsync(MenuAccess menuAccess)
        {
            using (var connection = CreateConnection())
            {
                var sql = @"
                IF EXISTS (SELECT 1 FROM SchoolManagementSecurity.MenuAccess WHERE RoleID = @RoleID AND MenuID = @MenuID)
                BEGIN
                    UPDATE SchoolManagementSecurity.MenuAccess SET CanAccess = @CanAccess WHERE RoleID = @RoleID AND MenuID = @MenuID
                END
                ELSE
                BEGIN
                    INSERT INTO SchoolManagementSecurity.MenuAccess (RoleID, MenuID, CanAccess) VALUES (@RoleID, @MenuID, @CanAccess)
                END";

                await connection.ExecuteAsync(sql, menuAccess);
            }
        }

        public async Task<IEnumerable<MenuItem>> GetMenusAsync()
        {
            try
            {
                using (var connection = CreateConnection())
                {
                    return await connection.QueryAsync<MenuItem>("SELECT * FROM SchoolManagementSecurity.MainMenu");
                }
            }
            catch (Exception ex)
            {
                // Implement logging here
                throw new Exception("An error occurred while fetching menus.", ex);
            }
        }

        public async Task UpdateUserRoleAsync(int userId, int roleId)
        {
            using (var connection = CreateConnection())
            {
                var sql = "UPDATE SchoolManagement.Users SET RoleID = @RoleID WHERE UserID = @UserID";
                await connection.ExecuteAsync(sql, new { UserID = userId, RoleID = roleId });
            }
        }

    }
}
