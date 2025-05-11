using CORE.MODEL;
using System.Data.SqlClient;
using System.Data;
using Dapper;
using CORE.SERVICE.MainLayout.Module;
using Microsoft.Extensions.Caching.Memory;

namespace CORE.SERVICE.MainLayout
{
    public class UserService
    {
        private readonly IConfiguration _configuration;
        private readonly IMemoryCache _cache;
        public UserService(IConfiguration configuration, IMemoryCache cache)
        {
            _configuration = configuration;
            _cache = cache;
        }

        private IDbConnection CreateConnection()
        {
            return new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
        }

        public async Task<IEnumerable<User>> GetUsersAsync()
        {
            return await _cache.GetOrCreateAsync("GetUsers", async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10); // Cache for 10 minutes
                using (var connection = CreateConnection())
                {
                    return await connection.QueryAsync<User>("SELECT * FROM SchoolManagement.Users");
                }
            });
        }


        public async Task<IEnumerable<Role>> GetRolesAsync()
        {
            return await _cache.GetOrCreateAsync("GetRoles", async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10);
                using (var connection = CreateConnection())
                {
                    return await connection.QueryAsync<Role>("SELECT * FROM SchoolManagementSecurity.MainSystemRoles");
                }
            });
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

            _cache.Remove("GetMenus");
        }

        public async Task<IEnumerable<MenuItem>> GetMenusAsync()
        {
            return await _cache.GetOrCreateAsync("GetMenus", async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10);
                using (var connection = CreateConnection())
                {
                    return await connection.QueryAsync<MenuItem>("SELECT * FROM SchoolManagementSecurity.MainMenu");
                }
            });
        }

        public async Task UpdateUserRoleAsync(int userId, int roleId)
        {
            using (var connection = CreateConnection())
            {
                var sql = "UPDATE SchoolManagement.Users SET RoleID = @RoleID WHERE UserID = @UserID";
                await connection.ExecuteAsync(sql, new { UserID = userId, RoleID = roleId });
            }

            _cache.Remove("GetUsers"); // Invalidate cache
        }

    }
}
