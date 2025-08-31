
using Dapper;
using CORE.MODEL;
using System.Data.SqlClient;
using CORE.Pages.LESSON_NOTE;
using CORE.SERVICE.MainLayout.Module;
using static CORE.MODEL.Teachers_Time_Table;
using static CORE.Pages.COURSES.View_Teacher_Subject_Assign_ByID;
using System.Data;
using System.Data.Common;
using CORE.MODEL.Expenses;
using CORE.MODEL.Bank;
using CORE.MODEL.Term;
using CORE.MODEL.Department;
using CORE.MODEL.Bank.LOG;
using CORE.MODEL.Bank.Transaction_Logs;
using CORE.MODEL.Set_Exams;
using CORE.MODEL.LeaveManagement;
using CORE.MODEL.LeaveStatus;
using CORE.MODEL.Students_Attendance;
using CORE.Pages.HR.SALARY_PAYMENT_HISTORY;
using CORE.MODEL.Salary_Payment_History;
using Microsoft.Extensions.Caching.Memory;
using System.Text;
using static CORE.Pages.FEES.STUDENT_FEES_VIEWING.Students_Fees_Viewing;
using Microsoft.AspNetCore.Connections;
using CORE.MODEL.Login_Name_Display;
using Microsoft.Extensions.Configuration;
using CORE.Pages.LOGIN_SCREEN;
using CORE.SERVICE.Caching;
using CORE.MODEL.DASHBOARD_AMOUNT_SUM;
using CORE.MODEL.Fees_Statement_Summary;
using CORE.MODEL.FEEDING_FEE;
using System;

namespace CORE.SERVICE
{
    // AuthService.cs
    public class AuthService
    {
        private readonly IMemoryCache _cache;
        private readonly string connectionString;
        private Timer _timer;
        private readonly CacheService _cacheService;
        public AuthService(string connectionString, IMemoryCache memoryCache, CacheService cacheService)
        {
            this.connectionString = connectionString;
            _cache = memoryCache;
            _cacheService = cacheService;
            StartAutoTransfer();          
        }

        public async Task ResetPasswordAsync(int userId, string defaultPassword)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                var query = "UPDATE SchoolManagement.Users SET Password = @Password, UpdatedOn = GETDATE() WHERE UserID = @UserID";
                await connection.ExecuteAsync(query, new { Password = defaultPassword, UserID = userId });
            }
        }

        public async Task<bool> ChangePasswordAsync(string defaultPassword, string newPassword)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                var query = "SELECT UserID FROM SchoolManagement.Users WHERE Password = @DefaultPassword";
                var userId = await connection.QueryFirstOrDefaultAsync<int?>(query, new { DefaultPassword = defaultPassword });

                if (userId.HasValue)
                {
                    var updateQuery = "UPDATE SchoolManagement.Users SET Password = @NewPassword, UpdatedOn = GETDATE() WHERE UserID = @UserID";
                    await connection.ExecuteAsync(updateQuery, new { NewPassword = newPassword, UserID = userId.Value });
                    return true;
                }
                return false;
            }
        }


        public async Task<User> GetUserByUsernameAsync(string username)
        {
            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    var query = "SELECT TOP (1) UserID, UserName, Password FROM SchoolManagement.Users WHERE UserName = @Username";
                    using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Username", username);

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                return new User
                                {
                                    UserID = reader.GetInt32(0),
                                    UserName = reader.GetString(1),
                                    Password = reader.GetString(2)
                                };
                            }
                        }
                    }
                }
            }
            catch (SqlException ex)
            {
                // Log SQL exceptions
                Console.WriteLine($"SQL Exception: {ex.Message}");
            }
            catch (Exception ex)
            {
                // Log other exceptions
                Console.WriteLine($"Exception: {ex.Message}");
            }

            return null;
        }
        //FOR CHECKING USER ROLE ND IT MENU ITEM
        public async Task<UserRoleAndMenuAccess> GetUserRoleAndMenuAccessAsync(int userId)
        {
            string cacheKey = $"UserRoleAndMenuAccess_{userId}";

            return await _cacheService.GetOrSetAsync(cacheKey, async () =>
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    var result = new UserRoleAndMenuAccess();

                    // Get user role
                    result.Role = await connection.QuerySingleAsync<string>(
                        "SELECT r.RoleName FROM SchoolManagementSecurity.MainSystemRoles r " +
                        "JOIN SchoolManagement.Users u ON r.RoleID = u.RoleID WHERE u.UserID = @UserID",
                        new { UserID = userId });

                    // Get menu items
                    result.MenuItems = (await connection.QueryAsync<MenuItem>(
                        "SELECT m.* FROM SchoolManagementSecurity.MainMenu m " +
                        "JOIN SchoolManagementSecurity.MenuAccess a ON m.MenuID = a.MenuID " +
                        "WHERE a.RoleID = (SELECT RoleID FROM SchoolManagement.Users WHERE UserID = @UserID) AND a.CanAccess = 1",
                        new { UserID = userId })).ToList();

                    return result;
                }
            }, minutes: 15); // Cache for 15 minutes
        }


        //Userlogs on when user logs in and out of the main system//
        public async Task LogUserEventAsync(int userId, string eventName)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                var query = "INSERT INTO SchoolManagement.UserLog (UserId, EventName, Timestamp) VALUES (@UserId, @EventName, GETDATE())";
                await connection.ExecuteAsync(query, new { UserId = userId, EventName = eventName });
            }
        }

        public async Task<IEnumerable<(string FeeTypeName, decimal Amount)>> GetFeeAmountsByClassIDAsync(string classId)
        {
            using var connection = new SqlConnection(connectionString);
            return await connection.QueryAsync<(string FeeTypeName, decimal Amount)>(
                "GetFeeAmountsByClassID",
                new { ClassID = classId },
                commandType: CommandType.StoredProcedure);
        }

        //--- For company LoginLayout display--///
        public async Task<string> GetSoftwareVersionAsync()
        {
            if (_cache.TryGetValue("SoftwareVersion", out string version))
            {
                return version;
            }

            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            using var command = connection.CreateCommand();
            command.CommandText = "SELECT SoftWareVerssion FROM SchoolManagement.LoginScreenDetails";
            version = (string)await command.ExecuteScalarAsync();

            _cache.Set("SoftwareVersion", version, TimeSpan.FromHours(1));
            return version;
        }

        public async Task<(string, string)> GetLoginScreenDetailsAsync()
        {
            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            using var command = connection.CreateCommand();
            command.CommandText = "SELECT SchoolName, CompanyRegisteredName FROM SchoolManagement.LoginScreenDetails";

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                string schoolName = reader.GetString(reader.GetOrdinal("SchoolName"));
                string companyName = reader.GetString(reader.GetOrdinal("CompanyRegisteredName"));
                return (schoolName, companyName);
            }

            return (null, null);
        }

        public int GetCompanyRegisteredYear()
        {
            // Use current year as company registered date
            return DateTime.Now.Year;
        }

        //----------------------For Admin_Security ------------------------------------//
        public void AddUser(User user)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();

                string query = "INSERT INTO SchoolManagement.Users (UserName, Password, FullName, RoleID) VALUES (@UserName, @Password, @FullName, @RoleID)";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@UserName", user.UserName);
                    command.Parameters.AddWithValue("@Password", user.Password);
                    command.Parameters.AddWithValue("@FullName", user.FullName);
                    command.Parameters.AddWithValue("@RoleID", user.RoleID);

                    command.ExecuteNonQuery();
                }
            }
        }

        public List<User> GetUsers()
        {
            List<User> users = new List<User>();

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();

                string query = "SELECT UserID, UserName, Password FROM SchoolManagement.Users";

                using (SqlCommand command = new SqlCommand(query, connection))
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        User user = new User
                        {
                            UserID = reader.GetInt32(0),
                            UserName = reader.GetString(1),
                            Password = reader.GetString(2),
                            FullName = reader.GetString(3)
                        };

                        users.Add(user);
                    }
                }
            }

            return users;
        }

        public void DeleteUser(int userId)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();

                // Delete related records in UserLog
                using (SqlCommand deleteLogsCommand = new SqlCommand("DELETE FROM SchoolManagement.UserLog WHERE UserId = @UserId", connection))
                {
                    deleteLogsCommand.Parameters.AddWithValue("@UserId", userId);
                    deleteLogsCommand.ExecuteNonQuery();
                }

                // Now delete the user
                using (SqlCommand deleteUserCommand = new SqlCommand("DELETE FROM SchoolManagement.Users WHERE UserID = @UserID", connection))
                {
                    deleteUserCommand.Parameters.AddWithValue("@UserID", userId);

                    int rowsAffected = deleteUserCommand.ExecuteNonQuery();

                    if (rowsAffected == 0)
                    {
                        throw new Exception($"User with ID {userId} not found.");
                    }
                }
            }
        }

        //------------------------END--------------------------------------//

        //----Assign mobile Roles--------//


        // Fetches the list of users with their roles
        public async Task<List<UserRoleViewModel>> GetUsersAsync()
        {
            if (_cache.TryGetValue("CachedUsersList", out List<UserRoleViewModel> cachedUsers))
                return cachedUsers;

            List<UserRoleViewModel> users = new List<UserRoleViewModel>();

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();

                using (SqlCommand command = new SqlCommand("SELECT UserID, UserName FROM SchoolManagement.Users", connection))
                using (SqlDataReader reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        users.Add(new UserRoleViewModel
                        {
                            UserID = reader.GetInt32(0),
                            UserName = reader.GetString(1)
                        });
                    }
                }
            }

            _cache.Set("CachedUsersList", users, TimeSpan.FromMinutes(10)); // cache for 10 minutes
            return users;
        }

        // Method to get all users
        public async Task<IEnumerable<User>> GetMUsersAsync()
        {
            if (_cache.TryGetValue("CachedMUsersList", out IEnumerable<User> cachedMUsers))
                return cachedMUsers;

            using (var connection = new SqlConnection(connectionString))
            {
                var result = await connection.QueryAsync<User>("SELECT UserID, UserName FROM SchoolManagement.Users");
                _cache.Set("CachedMUsersList", result, TimeSpan.FromMinutes(10));
                return result;
            }
        }

        // Method to get all roles
        public async Task<IEnumerable<Role>> GetRolesAsync()
        {
            if (_cache.TryGetValue("CachedRoles", out IEnumerable<Role> cachedRoles))
                return cachedRoles;

            using (var connection = new SqlConnection(connectionString))
            {
                var roles = await connection.QueryAsync<Role>("SELECT RoleName FROM SchoolManagement.Roles");
                _cache.Set("CachedRoles", roles, TimeSpan.FromMinutes(30));
                return roles;
            }
        }

        // Method to get all menu items
        public async Task<IEnumerable<MobileMenuItem>> GetMenuItemsAsync()
        {
            if (_cache.TryGetValue("CachedMenuItems", out IEnumerable<MobileMenuItem> cachedMenuItems))
                return cachedMenuItems;

            using (var connection = new SqlConnection(connectionString))
            {
                var menuItems = await connection.QueryAsync<MobileMenuItem>(
                    "SELECT CategoryName, MenuItem AS ItemName FROM SchoolManagement.MobileAppMenuDisplay");
                _cache.Set("CachedMenuItems", menuItems, TimeSpan.FromMinutes(30));
                return menuItems;
            }
        }

        // Method to get distinct category names
        public async Task<IEnumerable<string>> GetCategoryNamesAsync()
        {
            if (_cache.TryGetValue("CachedCategoryNames", out IEnumerable<string> cachedCategories))
                return cachedCategories;

            using (var connection = new SqlConnection(connectionString))
            {
                var categories = await connection.QueryAsync<string>(
                    "SELECT DISTINCT CategoryName FROM SchoolManagement.MobileAppMenuDisplay");
                _cache.Set("CachedCategoryNames", categories, TimeSpan.FromMinutes(30));
                return categories;
            }
        }

        // Method to update a role  will use for editing featuer implementation
        //public async Task UpdateRoleAsync(MobileAppRole role)
        //{
        //    using (var connection = new SqlConnection(connectionString))
        //    {
        //        await connection.ExecuteAsync(
        //            "UPDATE SchoolManagement.MobileAppRoles SET RoleName = @RoleName, MenuItem = @MenuItem, CategoryName = @CategoryName, Enable = @Enable WHERE UserRoleID = @UserRoleID",
        //            role);
        //    }
        //}

        // Method to delete a role will use for editing 
        //public async Task DeleteRoleAsync(int userRoleId)
        //{
        //    using (var connection = new SqlConnection(connectionString))
        //    {
        //        await connection.ExecuteAsync(
        //            "DELETE FROM SchoolManagement.MobileAppRoles WHERE UserRoleID = @UserRoleID",
        //            new { UserRoleID = userRoleId });
        //    }
        //}


        // Method to get roles assigned to a user
        public async Task<IEnumerable<MobileAppRole>> GetUserRolesAsync(int userId)
        {
            string cacheKey = $"UserRoles_{userId}";

            if (_cache.TryGetValue(cacheKey, out IEnumerable<MobileAppRole> cachedRoles))
                return cachedRoles;

            using (var connection = new SqlConnection(connectionString))
            {
                var roles = await connection.QueryAsync<MobileAppRole>(
                    "SELECT * FROM SchoolManagement.MobileAppRoles WHERE UserID = @UserID",
                    new { UserID = userId });

                _cache.Set(cacheKey, roles, TimeSpan.FromMinutes(10)); // or less depending on role update frequency

                return roles;
            }
        }

        // Method to assign a role to a user
        public async Task AssignRoleAsync(int userId, string roleName, bool enable, string menuItem, string categoryName)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                await connection.ExecuteAsync(
                    "INSERT INTO SchoolManagement.MobileAppRoles (UserID, RoleName, Enable, MenuItem, CategoryName) VALUES (@UserID, @RoleName, @Enable, @MenuItem, @CategoryName)",
                    new { UserID = userId, RoleName = roleName, Enable = enable, MenuItem = menuItem, CategoryName = categoryName });
            }

            _cache.Remove($"UserRoles_{userId}"); // Invalidate cache after insert
        }

        // Method to update role enable status
        public async Task UpdateRoleEnableStatusAsync(int userRoleId, bool enable)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                await connection.ExecuteAsync(
                    "UPDATE SchoolManagement.MobileAppRoles SET Enable = @Enable WHERE UserRoleID = @UserRoleID",
                    new { Enable = enable, UserRoleID = userRoleId });
            }
        }    

    //------END-----------//


    //logout service
    public async Task LogEvent(int userId, string eventName)
        {
            const string query = "INSERT INTO SchoolManagement.UserLog (UserId, EventName, Timestamp) VALUES (@UserId, @EventName, GETDATE())";

            using (var connection = new SqlConnection(connectionString))
            using (var command = new SqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@UserId", userId);
                command.Parameters.AddWithValue("@EventName", eventName);

                await connection.OpenAsync();

                await command.ExecuteNonQueryAsync();
            }
        }


        //Add Student
        public async Task<int> AddStudentAsync(Student student)
        {
            try
            {
                // Additional validation for required fields
                if (string.IsNullOrEmpty(student.StudentFirstName) ||
                    string.IsNullOrEmpty(student.StudentLastName))
                {
                    throw new ArgumentException("First and last name are required.");
                }

                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    var query = @"INSERT INTO SchoolManagement.Students 
                          (StudentFirstName, StudentLastName, StudentDateOfBirth, StudentGender, StudentAddress, 
                           StudentPhoneNumber, StudentEmail, ImageData, ClassID,
                           GuardianFullName, GuardianGender, GuardianHouseAddress, GuardianWorkAddress, 
                           GuardianEmail, GuardianFirstContact, GuardianSecondContact, EnableSwitch, StudentMedicalReport) 
                          VALUES (@FirstName, @LastName, @DateOfBirth, @Gender, @Address, @PhoneNumber, @Email, 
                                  @ImageData, @ClassID, @GuardianFullName, @GuardianGender, @GuardianHouseAddress, 
                                  @GuardianWorkAddress, @GuardianEmail, @GuardianFirstContact, @GuardianSecondContact, 
                                  @EnableSwitch, @StudentMedicalReport); 
                          SELECT SCOPE_IDENTITY();";

                    using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@FirstName", student.StudentFirstName);
                        command.Parameters.AddWithValue("@LastName", student.StudentLastName);
                        command.Parameters.AddWithValue("@DateOfBirth", student.StudentDateOfBirth ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@Gender", student.StudentGender ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@Address", student.StudentAddress ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@PhoneNumber", student.StudentPhoneNumber ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@Email", student.StudentEmail ?? (object)DBNull.Value);
                        command.Parameters.Add("@ImageData", SqlDbType.VarBinary).Value = student.ImageData ?? (object)DBNull.Value;
                        command.Parameters.AddWithValue("@ClassID", student.ClassID);
                        command.Parameters.AddWithValue("@GuardianFullName", student.GuardianFullName ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@GuardianGender", student.GuardianGender ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@GuardianHouseAddress", student.GuardianHouseAddress ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@GuardianWorkAddress", student.GuardianWorkAddress ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@GuardianEmail", student.GuardianEmail ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@GuardianFirstContact", student.GuardianFirstContact ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@GuardianSecondContact", student.GuardianSecondContact ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@EnableSwitch", student.EnableSwitch);
                        command.Parameters.AddWithValue("@StudentMedicalReport", student.StudentMedicalReport ?? (object)DBNull.Value);

                        var result = await command.ExecuteScalarAsync();

                        if (result != null)
                        {
                            int newId = Convert.ToInt32(result);

                            // Insert into SyncLog
                            string logQuery = @"INSERT INTO SchoolManagement.SyncLog 
                                        (TableName, RecordID, ActionType) 
                                        VALUES ('Students', @StudentID, 'INSERT')";
                            await connection.ExecuteAsync(logQuery, new { StudentID = newId });

                            return newId;
                        }
                        else
                        {
                            return 0;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in AddStudentAsync: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> UpdateStudent(Student student)
        {
            var query = @"
                UPDATE SchoolManagement.Students
                SET 
                    StudentFirstName = @StudentFirstName,
                    StudentLastName = @StudentLastName,
                    StudentDateOfBirth = @StudentDateOfBirth,
                    StudentGender = @StudentGender,
                    StudentAddress = @StudentAddress,
                    StudentPhoneNumber = @StudentPhoneNumber,
                    StudentEmail = @StudentEmail,
                    ClassID = @ClassID,
                    GuardianFullName = @GuardianFullName,
                    GuardianGender = @GuardianGender,
                    GuardianHouseAddress = @GuardianHouseAddress,
                    GuardianWorkAddress = @GuardianWorkAddress,
                    GuardianEmail = @GuardianEmail,
                    GuardianFirstContact = @GuardianFirstContact,
                    GuardianSecondContact = @GuardianSecondContact,
                    StudentMedicalReport = @StudentMedicalReport
                WHERE StudentID = @StudentID";

            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();
                var result = await connection.ExecuteAsync(query, student);
                if (result > 0)
                {
                    // Log the change for sync
                    string logQuery = @"INSERT INTO SchoolManagement.SyncLog 
                                (TableName, RecordID, ActionType) 
                                VALUES ('Students', @StudentID, 'UPDATE')";
                    await connection.ExecuteAsync(logQuery, new { student.StudentID });
                }
                return result > 0;
            }
        }

        //Add Teachers
        public async Task<int> AddTeachersAsync(TeachersRegistration teachersRegistration)
        {
            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    var query = "INSERT INTO SchoolManagement.Teacher (TeacherFirstName, TeacherLastName, TeacherDateOfBirth, TeacherGender, TeacherAddress, TeacherPhoneNumber, TeacherEmail, ImageData, DateHired, SSNIT, BasicSalary, PAYE, SSNITTIER2, VotersID, HealthInsurance, GhanaCard, Bank, AccountNumber, Remarks, SSNITNumber, CategoryName, EmploymentStatus) " +
                                "VALUES (@FirstName, @LastName, @DateOfBirth, @Gender, @Address, @PhoneNumber, @Email, @ImageData, @DateHired, @SSNIT, @BasicSalary, @PAYE, @SSNITTIER2, @VotersID, @HealthInsurance, @GhanaCard, @Bank, @AccountNumber, @Remarks, @SSNITNumber, @CategoryName, @EmploymentStatus); " +
                                "SELECT SCOPE_IDENTITY();";

                    using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@FirstName", teachersRegistration.TeacherFirstName);
                        command.Parameters.AddWithValue("@LastName", teachersRegistration.TeacherLastName);
                        command.Parameters.AddWithValue("@DateOfBirth", teachersRegistration.TeacherDateOfBirth ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@Gender", teachersRegistration.TeacherGender ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@Address", teachersRegistration.TeacherAddress ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@PhoneNumber", teachersRegistration.TeacherPhoneNumber ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@Email", teachersRegistration.TeacherEmail ?? (object)DBNull.Value);

                        var imageParameter = command.Parameters.Add("@ImageData", SqlDbType.VarBinary, -1);
                        imageParameter.Value = teachersRegistration.ImageData ?? (object)DBNull.Value;

                        command.Parameters.AddWithValue("@DateHired", teachersRegistration.DateHired ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@SSNIT", teachersRegistration.SSNIT ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@BasicSalary", teachersRegistration.BasicSalary ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@PAYE", teachersRegistration.PAYE ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@SSNITTIER2", teachersRegistration.SSNITTIER2 ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@VotersID", teachersRegistration.VotersID ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@HealthInsurance", teachersRegistration.HealthInsurance ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@GhanaCard", teachersRegistration.GhanaCard ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@Bank", teachersRegistration.Bank ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@AccountNumber", teachersRegistration.AccountNumber ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@Remarks", teachersRegistration.Remarks ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@SSNITNumber", teachersRegistration.SSNITNumber ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@CategoryName", teachersRegistration.CategoryName ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@EmploymentStatus", teachersRegistration.EmploymentStatus);

                        var result = await command.ExecuteScalarAsync();
                        return result != null ? Convert.ToInt32(result) : 0;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in AddTeacherAsync: {ex.Message}");
                throw; // Rethrow the exception after logging/handling if needed
            }
        }

        public async Task<bool> UpdateTeachersRegistrationData(TeachersRegistration teachersRegistration)
        {
            var query = @"UPDATE SchoolManagement.Teacher
                  SET   
                       TeacherFirstName = @TeacherFirstName,
                       TeacherLastName = @TeacherLastName,
                       TeacherGender = @TeacherGender,
                       TeacherAddress = @TeacherAddress,
                       TeacherPhoneNumber = @TeacherPhoneNumber,
                       TeacherEmail = @TeacherEmail,
                       TeacherDateOfBirth = @TeacherDateOfBirth,
                       DateHired = @DateHired,
                       SSNIT = @SSNIT,
                       BasicSalary  = @BasicSalary,
                       PAYE = @PAYE,
                       SSNITTIER2 = @SSNITTIER2,
                       VotersID = @VotersID,
                       ImageData = @ImageData,
                       HealthInsurance = @HealthInsurance,
                       GhanaCard = @GhanaCard,
                       Bank = @Bank,
                       AccountNumber = @AccountNumber,
                       Remarks = @Remarks,
                       SSNITNumber = @SSNITNumber,
                       CategoryName = @CategoryName,
                       RegisteredBy = @RegisteredBy,
                       EditedBy = @EditedBy
                       WHERE TeacherID = @TeacherID";

            using(var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();
                var result = await connection.ExecuteAsync(query, teachersRegistration);
                return result > 0;
            }
        }

        //Service that connect to the database to add NonTeachingStaffsRegistration
        public async Task<int> AddNonTeachingStaffsAsync(NonTeachingStaffsRegistration nonteachingRegistration)
        {
            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    var query = "INSERT INTO SchoolManagement.NonTeachingStaffs (NonTeachingStaffsFirstName, NonTeachingStaffsLastName, NonTeachingStaffsDateOfBirth, NonTeachingStaffsGender," +
                        " NonTeachingStaffsAddress, NonTeachingStaffsPhoneNumber, NonTeachingStaffsEmail, ImageData) " +
                                "VALUES (@FirstName, @LastName, @DateOfBirth, @Gender, @Address, @PhoneNumber, @Email, @ImageData); " +
                                "SELECT SCOPE_IDENTITY();";

                    using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@FirstName", nonteachingRegistration.NonTeachingStaffsFirstName);
                        command.Parameters.AddWithValue("@LastName", nonteachingRegistration.NonTeachingStaffsLastName);
                        command.Parameters.AddWithValue("@DateOfBirth", nonteachingRegistration.NonTeachingStaffsDateOfBirth);
                        command.Parameters.AddWithValue("@Gender", nonteachingRegistration.NonTeachingStaffsGender);
                        command.Parameters.AddWithValue("@Address", nonteachingRegistration.NonTeachingStaffsAddress);
                        command.Parameters.AddWithValue("@PhoneNumber", nonteachingRegistration.NonTeachingStaffsPhoneNumber);
                        command.Parameters.AddWithValue("@Email", nonteachingRegistration.NonTeachingStaffsEmail);

                        // Add parameter for ImageData
                        command.Parameters.AddWithValue("@ImageData", nonteachingRegistration.ImageData ?? (object)DBNull.Value);

                        // ExecuteScalarAsync returns the identity of the new record (StudentID)
                        var result = await command.ExecuteScalarAsync();

                        // Check if the insertion was successful
                        return result != null ? Convert.ToInt32(result) : 0;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in AddTeacherAsync: {ex.Message}");
                // Handle the exception as needed (log, throw, etc.)
                throw; // Rethrow the exception after logging/handling if needed
            }
        }

        //Register Students
        public async Task<int> RegisterStudentAsync(Student student)
        {
            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    var query = "INSERT INTO SchoolManagement.Students (StudentFirstName, StudentLastName, StudentDateOfBirth, StudentGender, StudentAddress, StudentPhoneNumber, StudentEmail, ImageData) " +
                                "VALUES (@FirstName, @LastName, @DateOfBirth, @Gender, @Address, @PhoneNumber, @Email, @ImageData); " +
                                "SELECT SCOPE_IDENTITY();";

                    using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@FirstName", student.StudentFirstName);
                        command.Parameters.AddWithValue("@LastName", student.StudentLastName);
                        command.Parameters.AddWithValue("@DateOfBirth", student.StudentDateOfBirth);
                        command.Parameters.AddWithValue("@Gender", student.StudentGender);
                        command.Parameters.AddWithValue("@Address", student.StudentAddress);
                        command.Parameters.AddWithValue("@PhoneNumber", student.StudentPhoneNumber);
                        command.Parameters.AddWithValue("@Email", student.StudentEmail);

                        // Add parameter for ImageData
                        command.Parameters.AddWithValue("@ImageData", student.ImageData ?? (object)DBNull.Value);

                        // ExecuteScalarAsync returns the identity of the new record (StudentID)
                        var result = await command.ExecuteScalarAsync();

                        // Check if the insertion was successful
                        return result != null ? Convert.ToInt32(result) : 0;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in RegisterStudentAsync: {ex.Message}");
                // Handle the exception as needed (log, throw, etc.)
                throw; // Rethrow the exception after logging/handling if needed
            }
        }

        //Register StudentsCourse
        public async Task<bool> RegisterStudentForCourseAsync(int studentID, int courseID)
        {
            try
            {
                Console.WriteLine($"RegisterStudentForCourseAsync - StudentID: {studentID}, CourseID: {courseID}"); // Log the values

                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    // Check if the StudentID and CourseID exist in the referenced tables
                    var studentExists = await CheckRecordExistsAsync(connection, "SchoolManagement.Students", "StudentID", studentID);
                    var courseExists = await CheckRecordExistsAsync(connection, "SchoolManagement.Courses", "CourseID", courseID);

                    if (!studentExists || !courseExists)
                    {
                        Console.WriteLine("Invalid StudentID or CourseID. Registration failed.");
                        return false; // Return false to indicate failure
                    }

                    var query = "INSERT INTO SchoolManagement.StudentCourseRegistrations (StudentID, CourseID, RegistrationDate) " +
                                "VALUES (@StudentID, @CourseID, @RegistrationDate);";

                    using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@StudentID", studentID);
                        command.Parameters.AddWithValue("@CourseID", courseID);
                        command.Parameters.AddWithValue("@RegistrationDate", DateTime.Now);

                        await command.ExecuteNonQueryAsync();
                    }

                    Console.WriteLine("Registration successful."); // Log success

                    // Return true to indicate successful registration
                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in RegisterStudentForCourseAsync: {ex.Message}");
                // Handle the exception as needed (log, throw, etc.)
                return false; // Return false to indicate failure
            }
        }


        // Helper method to check if a record exists in a table
        private async Task<bool> CheckRecordExistsAsync(SqlConnection connection, string tableName, string idColumnName, int idValue)
        {
            var query = $"SELECT COUNT(1) FROM {tableName} WHERE {idColumnName} = @ID";

            using (var command = new SqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@ID", idValue);
                var count = (int)await command.ExecuteScalarAsync();

                return count > 0;
            }
        }


        //Get Student by Id
        public async Task<Student> GetStudentByIdAsync(string studentId)
        {
            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    var query = "SELECT TOP(1) StudentID, StudentFirstName, StudentLastName, StudentGender " +
                                "FROM SchoolManagement.Students " +
                                "WHERE StudentID = @StudentId";

                    using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@StudentId", studentId);

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                return new Student
                                {
                                    StudentID = reader.GetInt32(0),
                                    StudentFirstName = reader.GetString(1),
                                    StudentLastName = reader.GetString(2),
                                    StudentGender = reader.GetString(3)
                                };
                            }
                        }
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetStudentByIdAsync: {ex.Message}");
                // Handle the exception as needed (log, throw, etc.)
                throw;
            }
        }

        //Get Student by Name
        public async Task<Student> GetStudentByNameAsync(string firstName, string lastName)
        {
            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    var query = "SELECT TOP(1) StudentID, StudentFirstName, StudentLastName, StudentGender " +
                                "FROM SchoolManagement.Students " +
                                "WHERE StudentFirstName = @FirstName AND StudentLastName = @LastName";

                    using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@FirstName", firstName);
                        command.Parameters.AddWithValue("@LastName", lastName);

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                return new Student
                                {
                                    StudentID = reader.GetInt32(0),
                                    StudentFirstName = reader.GetString(1),
                                    StudentLastName = reader.GetString(2),
                                    StudentGender = reader.GetString(3)
                                };
                            }
                        }
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetStudentByNameAsync: {ex.Message}");
                throw;
            }
        }

        //Get All Course
        public async Task<List<Course>> GetAllCoursesAsync()
        {
            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    var query = "SELECT CourseID, CourseName, Description FROM SchoolManagement.Courses";

                    using (var command = new SqlCommand(query, connection))
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        var courses = new List<Course>();

                        while (await reader.ReadAsync())
                        {
                            var course = new Course
                            {
                                CourseID = reader.GetInt32(0),
                                CourseName = reader.GetString(1),
                                Description = reader.GetString(2)
                            };

                            courses.Add(course);
                        }

                        return courses;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetAllCoursesAsync: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// //////////////////////.................
        /// </summary>
        /// <returns></returns>

        //for school course
        public async Task<List<SchoolCourse>> GetSchoolCoursesAsync()
        {
            return await _cacheService.GetOrSetAsync("AllSchoolCourses", async () =>
            {
                try
                {
                    using (var connection = new SqlConnection(connectionString))
                    {
                        await connection.OpenAsync();

                        var query = "SELECT SCID, SchoolCourse FROM SchoolManagement.SchoolCourse";

                        using (var command = new SqlCommand(query, connection))
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            var schoolCourses = new List<SchoolCourse>();

                            while (await reader.ReadAsync())
                            {
                                var schoolCourse = new SchoolCourse
                                {
                                    SCID = reader.IsDBNull(0) ? null : reader.GetString(0),
                                    SchoolCourseName = reader.IsDBNull(1) ? null : reader.GetString(1)
                                };

                                schoolCourses.Add(schoolCourse);
                            }

                            return schoolCourses;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error in GetSchoolCoursesAsync: {ex.Message}");
                    throw;
                }
            }, minutes: 30); // Cache for 30 minutes
        }



        public async Task<int> AddSchoolCourseAsync(SchoolCourse schoolCourse)
        {
            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    var query = "INSERT INTO SchoolManagement.SchoolCourse (SCID, SchoolCourse) VALUES (@SCID, @SchoolCourse);";

                    using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@SCID", (object)schoolCourse.SCID ?? DBNull.Value);
                        command.Parameters.AddWithValue("@SchoolCourse", (object)schoolCourse.SchoolCourseName ?? DBNull.Value);

                        var result = await command.ExecuteNonQueryAsync();

                        // Invalidate cache after successful insert
                        _cacheService.Invalidate("AllSchoolCourses");

                        return result;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in AddSchoolCourseAsync: {ex.Message}");
                throw;
            }
        }


        public async Task<int> UpdateSchoolCourseAsync(SchoolCourse schoolCourse)
        {
            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    var query = "UPDATE SchoolManagement.SchoolCourse " +
                                "SET SchoolCourse = @SchoolCourse " +
                                "WHERE SCID = @SCID;";

                    using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@SCID", schoolCourse.SCID);
                        command.Parameters.AddWithValue("@SchoolCourse", schoolCourse.SchoolCourseName);

                        var result = await command.ExecuteNonQueryAsync();

                        // Invalidate cache after update
                        _cacheService.Invalidate("AllSchoolCourses");

                        return result;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in UpdateSchoolCourseAsync: {ex.Message}");
                throw;
            }
        }

        public async Task<int> DeleteSchoolCourseAsync(string scid)
        {
            try
            {
                await using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    var query = "DELETE FROM SchoolManagement.SchoolCourse WHERE SCID = @SCID;";

                    await using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@SCID", scid);

                        var result = await command.ExecuteNonQueryAsync();

                        // Invalidate course cache
                        _cacheService.Invalidate("AllSchoolCourses");

                        return result;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in DeleteSchoolCourseAsync: {ex.Message}");
                throw;
            }
        }



        /////////////////////////////////////////////////////////////////////////////////////
        ///

        //Assign Teachers 
        public async Task<List<TeachersRegistration>> GetAllTeachersAsync()
        {
            return await _cacheService.GetOrSetAsync("AllTeachers", async () =>
            {
                try
                {
                    using (var connection = new SqlConnection(connectionString))
                    {
                        await connection.OpenAsync();

                        var query = "SELECT * FROM SchoolManagement.Teacher";
                        var result = await connection.QueryAsync<TeachersRegistration>(query);

                        return result.AsList();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error in GetAllTeachersAsync: {ex.Message}");
                    throw;
                }
            }, minutes: 20); // Cache for 20 minutes
        }

        public async Task<int> AssignTeacherToClassAsync(int teacherId, string classId)
        {
            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    // Fetch the teacher information
                    var teacher = await connection.QueryFirstOrDefaultAsync<TeachersRegistration>(
                        "SELECT TeacherFirstName, TeacherLastName, TeacherPhoneNumber FROM SchoolManagement.Teacher WHERE TeacherID = @TeacherId",
                        new { TeacherId = teacherId }
                    );

                    if (teacher == null)
                        throw new Exception("Teacher not found.");

                    // Insert data into the Classes table with a fixed class name
                    var query = "INSERT INTO SchoolManagement.Classes (ClassID, ClassName, TeacherFirstName, TeacherLastName, TeacherPhoneNumber) " +
                                "VALUES (@ClassId, 'DefaultClass', @TeacherFirstName, @TeacherLastName, @TeacherPhoneNumber)";

                    var parameters = new DynamicParameters();
                    parameters.Add("@ClassId", classId);
                    parameters.Add("@TeacherFirstName", teacher.TeacherFirstName);
                    parameters.Add("@TeacherLastName", teacher.TeacherLastName);
                    parameters.Add("@TeacherPhoneNumber", teacher.TeacherPhoneNumber);

                    var result = await connection.ExecuteAsync(query, parameters);

                    // Invalidate related caches
                    _cacheService.Invalidate("AllClasses");
                    _cacheService.Invalidate($"TeacherClassAssignments_{teacherId}");

                    return result;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in AssignTeacherToClassAsync: {ex.Message}");
                throw;
            }
        }



        public async Task<int> AddAssignmentAsync(int teacherId, string classId)
        {
            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    var query = "INSERT INTO SchoolManagement.Classes (TeacherID, ClassID) VALUES (@TeacherId, @ClassId)";
                    var parameters = new { TeacherId = teacherId, ClassId = classId };

                    var result = await connection.ExecuteAsync(query, parameters);

                    // Invalidate related cache
                    _cacheService.Invalidate("AllClasses");
                    _cacheService.Invalidate($"ClassAssignmentsByTeacher_{teacherId}");

                    return result;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in AddAssignmentAsync: {ex.Message}");
                throw;
            }
        }


        public async Task<int> DeleteAssignmentAsync(int teacherId, string classId)
        {
            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    var query = "DELETE FROM SchoolManagement.Classes WHERE TeacherID = @TeacherId AND ClassID = @ClassId";
                    var parameters = new { TeacherId = teacherId, ClassId = classId };

                    var result = await connection.ExecuteAsync(query, parameters);

                    // Invalidate cache after deletion
                    _cacheService.Invalidate("AllClasses");
                    _cacheService.Invalidate($"ClassAssignmentsByTeacher_{teacherId}");

                    return result;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in DeleteAssignmentAsync: {ex.Message}");
                throw;
            }
        }

        public async Task<List<Class>> GetAllClassAsync() ///This Service is been use by assignteachers page and studentsregistration page for the dropdown of classid and is being used by Exams page 
        {
            return await _cacheService.GetOrSetAsync("AllClasses", async () =>
            {
                try
                {
                    using (var connection = new SqlConnection(connectionString))
                    {
                        await connection.OpenAsync();

                        var query = "SELECT * FROM SchoolManagement.Class";
                        var result = await connection.QueryAsync<Class>(query);

                        return result.AsList();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error in GetAllClassAsync: {ex.Message}");
                    throw;
                }
            }, minutes: 30); // Cached for 30 minutes
        }

        //TeacherAssignClassService
        public async Task<List<TeachersAssignClasses>> GetAllClassesAsync()
        {
            return await _cacheService.GetOrSetAsync("AllAssignedClasses", async () =>
            {
                try
                {
                    using (var connection = new SqlConnection(connectionString))
                    {
                        await connection.OpenAsync();

                        var query = "SELECT ClassID, ClassName, TeacherFirstName, TeacherLastName, TeacherPhoneNumber FROM SchoolManagement.Classes";

                        return (await connection.QueryAsync<TeachersAssignClasses>(query)).AsList();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error in GetAllClassesAsync: {ex.Message}");
                    throw;
                }
            }, minutes: 20); // Cached for 20 minutes
        }

        public async Task<bool> DeleteClassAsync(string classId)
        {
            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    var query = "DELETE FROM SchoolManagement.Classes WHERE ClassID = @ClassID";

                    await connection.ExecuteAsync(query, new { ClassID = classId });

                    // Invalidate all class-related cache keys
                    _cacheService.Invalidate("AllClasses");           // Used by dropdowns etc.
                    _cacheService.Invalidate("AllAssignedClasses");   // Used by teacher/class listing page

                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in DeleteClassAsync: {ex.Message}");
                return false;
            }
        }
        //Ends


        //StudentProfile Service
        public async Task<Student> GetStudentByNameAsync(string studentName)
        {
            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    var query = @"SELECT TOP (1) StudentID, StudentFirstName, StudentLastName, StudentDateOfBirth, StudentGender, 
                          StudentAddress, StudentPhoneNumber, StudentEmail, ImageData, ClassID, GuardianFullName, GuardianGender, 
                          GuardianHouseAddress, GuardianWorkAddress, GuardianEmail, GuardianFirstContact, GuardianSecondContact, 
                          StudentMedicalReport 
                          FROM SchoolManagement.Students 
                          WHERE CONCAT(StudentFirstName, ' ', StudentLastName) LIKE @StudentName";

                    var result = await connection.QueryFirstOrDefaultAsync<Student>(query, new { StudentName = $"%{studentName}%" });

                    return result;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetStudentByNameAsync: {ex.Message}");
                throw;
            }
        }

        public async Task<IEnumerable<string>> SearchStudentsForKYCAsync(string keyword)
        {
            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    var query = @"SELECT CONCAT(StudentFirstName, ' ', StudentLastName, ' - ', ClassID) AS DisplayName
                          FROM SchoolManagement.Students
                          WHERE CONCAT(StudentFirstName, ' ', StudentLastName) LIKE @Keyword";

                    var result = await connection.QueryAsync<string>(query, new { Keyword = $"%{keyword}%" });

                    return result;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in SearchStudentsAsync: {ex.Message}");
                throw;
            }
        }


        //Uploading Results for Students
        public async Task<int> UploadScoreAsync(ClassScore classScore)
        {
            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    // Check if the provided StudentID exists in the Students table
                    var studentExists = await connection.ExecuteScalarAsync<bool>(
                        "SELECT TOP 1 1 FROM SchoolManagement.Students WHERE StudentID = @StudentID",
                        new { StudentID = classScore.StudentID }
                    );

                    if (!studentExists)
                    {
                        // Handle the case where the provided StudentID does not exist
                        return -1; // You can use a specific error code or throw an exception
                    }

                    // Insert into ClassScores table
                    var query = "INSERT INTO schoolmanagement.classscores (StudentID, StudentFirstName, StudentLastName, ClassName, SchoolCourse, Score) " +
                                "VALUES (@StudentID, @StudentFirstName, @StudentLastName, @ClassName, @SchoolCourse, @Score);";

                    return await connection.ExecuteAsync(query, classScore);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in UploadScoreAsync: {ex.Message}");
                throw;
            }
        }

        // GetStudentByNameOrIdAsync method

        public async Task<Student> GetStudentByNameOrIdAsyncs(string searchInput)
        {
            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    // Check if the search input is a numeric value (assumed to be StudentID)
                    if (int.TryParse(searchInput, out int studentId))
                    {
                        var query = "SELECT * FROM SchoolManagement.Students WHERE StudentID = @StudentId";
                        return await connection.QueryFirstOrDefaultAsync<Student>(query, new { StudentId = studentId });
                    }
                    else
                    {
                        // If not numeric, assume it's a name
                        // Split the search input into first and last names
                        var names = searchInput.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                        if (names.Length >= 2)
                        {
                            // Search for FirstName LastName or LastName FirstName
                            var query = "SELECT * FROM SchoolManagement.Students " +
                                        "WHERE (StudentFirstName LIKE @FirstName AND StudentLastName LIKE @LastName) " +
                                        "OR (StudentFirstName LIKE @LastName AND StudentLastName LIKE @FirstName)";
                            return await connection.QueryFirstOrDefaultAsync<Student>(query, new
                            {
                                FirstName = $"%{names[0]}%",
                                LastName = $"%{names[1]}%"
                            });
                        }
                        else
                        {
                            // Search for FirstName or LastName
                            var query = "SELECT * FROM SchoolManagement.Students " +
                                        "WHERE StudentFirstName LIKE @SearchInput OR StudentLastName LIKE @SearchInput";
                            return await connection.QueryFirstOrDefaultAsync<Student>(query, new { SearchInput = $"%{searchInput}%" });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetStudentByNameOrIdAsync: {ex.Message}");
                throw;
            }
        }

        //get course name
        public async Task<List<ClassScore>> GetCourseNamesAsync()
        {
            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    var query = "SELECT SchoolCourse FROM SchoolManagement.SchoolCourse";
                    return (await connection.QueryAsync<ClassScore>(query)).AsList();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetCourseNamesAsync: {ex.Message}");
                throw;
            }
        }

        // Inside the AuthService or relevant service class
        public async Task<ClassScore> CheckExistingScoreAsync(ClassScore classScore)
        {
            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    var query = "SELECT TOP 1 * FROM schoolmanagement.classscores " +
                                "WHERE StudentID = @StudentID " +
                                "AND ClassName = @ClassName " +
                                "AND SchoolCourse = @SchoolCourse;";

                    return await connection.QueryFirstOrDefaultAsync<ClassScore>(query, classScore);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in CheckExistingScoreAsync: {ex.Message}");
                throw;
            }
        }
        //-------------Students Attendance----------------//
        public async Task<List<Student>> GetStudentsByClassAsync(string classID)
        {
            return await _cacheService.GetOrSetAsync($"StudentsByClass_{classID}", async () =>
            {
                try
                {
                    using (var connection = new SqlConnection(connectionString))
                    {
                        await connection.OpenAsync();

                        var query = "SELECT StudentFirstName, StudentLastName, ClassID, EnableSwitch " +
                                    "FROM SchoolManagement.Students " +
                                    "WHERE ClassID = @ClassID";

                        var parameters = new { ClassID = classID };

                        return (await connection.QueryAsync<Student>(query, parameters)).ToList();
                    }
                }
                catch (SqlException ex)
                {
                    Console.WriteLine($"SQL Exception: {ex.Message}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Exception: {ex.Message}");
                }

                return null;
            }, minutes: 15); // Cache for 15 minutes
        }


        public async Task SaveAttendanceAsync(List<Student> students, string classID, int userID, int termID, DateTime attendanceDate)
        {
            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    // Step 1: Check if the attendance date is in the future
                    if (attendanceDate > DateTime.Now)
                    {
                        throw new InvalidOperationException("Attendance cannot be taken for a future date.");
                    }

                    // Step 2: Check if attendance has already been taken for the class and date
                    const string checkAttendanceQuery = @"
                SELECT COUNT(*)
                FROM SchoolManagement.StudentsAttendance
                WHERE ClassID = @ClassID AND AttendanceDate = @AttendanceDate AND TermID = @TermID";

                    var checkParams = new
                    {
                        ClassID = classID,
                        AttendanceDate = attendanceDate.Date,
                        TermID = termID
                    };

                    var existingAttendanceCount = await connection.ExecuteScalarAsync<int>(checkAttendanceQuery, checkParams);

                    if (existingAttendanceCount > 0)
                    {
                        // If attendance already exists, notify user and prevent further action
                        throw new InvalidOperationException("Attendance has already been taken for this class on the selected date.");
                    }

                    // Step 3: Insert attendance for students if no existing records are found
                    const string insertAttendanceQuery = @"
                INSERT INTO SchoolManagement.StudentsAttendance
                (StudentFirstName, StudentLastName, ClassID, EnableSwitch, UserID, TermID, AttendanceDate)
                VALUES
                   (@StudentFirstName, @StudentLastName, @ClassID, @EnableSwitch, @UserID, @TermID, @AttendanceDate)";

                    foreach (var student in students)
                    {
                        var parameters = new
                        {
                            StudentFirstName = student.StudentFirstName,
                            StudentLastName = student.StudentLastName,
                            ClassID = classID,
                            EnableSwitch = student.EnableSwitch,
                            UserID = userID,
                            TermID = termID,
                            AttendanceDate = attendanceDate.Date
                        };

                        await connection.ExecuteAsync(insertAttendanceQuery, parameters);
                    }
                }
            }
            catch (InvalidOperationException ex)
            {
                // Handle specific error like future attendance or already taken attendance
                throw new Exception(ex.Message);  // Can pass the error message to notify user
            }
            catch (SqlException ex)
            {
                Console.WriteLine($"SQL Exception: {ex.Message}");
                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception: {ex.Message}");
                throw;
            }
        }



        //---------End Of Students Attendance---------------//

        //----------------For Teacher Attendance-Clock-In----------//      
        public async Task<List<TeachersRegistration>> GetTeachersAsync()
        {
            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    var query = "SELECT TeacherID, TeacherFirstName, TeacherLastName, EnableSwitch FROM SchoolManagement.Teacher";

                    using (var command = new SqlCommand(query, connection))
                    {
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            var teachers = new List<TeachersRegistration>();
                            while (await reader.ReadAsync())
                            {
                                var teacher = new TeachersRegistration
                                {
                                    TeacherID = reader.GetInt32(0),
                                    TeacherFirstName = reader.GetString(1),
                                    TeacherLastName = reader.GetString(2),
                                    EnableSwitch = DBNull.Value.Equals(reader["EnableSwitch"]) ? false : reader.GetBoolean(3)
                                };

                                teachers.Add(teacher);
                            }

                            return teachers;
                        }
                    }
                }
            }
            catch (SqlException ex)
            {
                // Log SQL exceptions
                Console.WriteLine($"SQL Exception: {ex.Message}");
            }
            catch (Exception ex)
            {
                // Log other exceptions
                Console.WriteLine($"Exception: {ex.Message}");
            }

            return new List<TeachersRegistration>(); // Return an empty list in case of an exception
        }


        public async Task<bool> SaveTeachersAttendanceAsync(TeacherAttendance attendanceRecord)
        {
            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    var query = @"
                INSERT INTO SchoolManagement.TeachersAttendance 
                (TeacherFirstName, TeacherLastName, EnableSwitch, ClockIN, UserID, RecDateCreated)
                VALUES 
                (@TeacherFirstName, @TeacherLastName, @EnableSwitch, @ClockIN, @UserID, @RecDateCreated)
            ";

                    using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@TeacherFirstName", attendanceRecord.TeacherFirstName);
                        command.Parameters.AddWithValue("@TeacherLastName", attendanceRecord.TeacherLastName);
                        command.Parameters.AddWithValue("@EnableSwitch", attendanceRecord.EnableSwitch);
                        command.Parameters.AddWithValue("@ClockIN", attendanceRecord.ClockIN);
                        command.Parameters.AddWithValue("@UserID", attendanceRecord.UserID);  // Include UserID parameter
                        command.Parameters.AddWithValue("@RecDateCreated", DateTime.Now);

                        int rowsAffected = await command.ExecuteNonQueryAsync();
                        return rowsAffected > 0;
                    }
                }
            }
            catch (SqlException ex)
            {
                Console.WriteLine($"SQL Exception: {ex.Message}");
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception: {ex.Message}");
                return false;
            }
        }

        /*This  a method to retrieve teachers who clocked in today*/
        public async Task<List<TeacherAttendance>> GetTodaysAttendanceAsync()
        {
            using (var connection = new SqlConnection(connectionString))
            {
                string query = @"
            SELECT TeacherID, TeacherFirstName, TeacherLastName, EnableSwitch, ClockIN 
            FROM SchoolManagement.TeachersAttendance
            WHERE CAST(ClockIN AS DATE) = CAST(GETDATE() AS DATE)";

                return (await connection.QueryAsync<TeacherAttendance>(query)).ToList();
            }
        }


        //----------------For Teacher Attendance-Clock-In-End ----------//


        public async Task<List<TeacherAttendance>> GetTeacherAttendanceAsync()
        {
            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    var query = "SELECT TeachersAttendanceID, TeacherFirstName, TeacherLastName, EnableSwitch, ClockIN FROM SchoolManagement.TeachersAttendance";

                    using (var command = new SqlCommand(query, connection))
                    {
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            var teachers = new List<TeacherAttendance>();
                            while (await reader.ReadAsync())
                            {
                                var teacher = new TeacherAttendance
                                {
                                    TeachersAttendanceID = reader.GetInt32(0),
                                    TeacherFirstName = reader.GetString(1),
                                    TeacherLastName = reader.GetString(2),
                                    EnableSwitch = DBNull.Value.Equals(reader["EnableSwitch"]) ? false : reader.GetBoolean(3),
                                    ClockIN = reader.GetDateTime(4)
                                };

                                teachers.Add(teacher);
                            }

                            return teachers;
                        }
                    }
                }
            }
            catch (SqlException ex)
            {
                Console.WriteLine($"SQL Exception: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception: {ex.Message}");
            }

            return new List<TeacherAttendance>();
        }


        public async Task<bool> SaveTeachersAttendanceOutAsync(TeachersAttendanceOut attendanceRecord)
        {
            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    var query = @"
                    INSERT INTO SchoolManagement.TeachersAttendanceOut 
                    (TeacherFirstName, TeacherLastName, EnableSwitch, ClockOUT, UserID)
                    VALUES 
                    (@TeacherFirstName, @TeacherLastName, @EnableSwitch, @ClockOUT, @UserID)
                ";

                    using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@TeacherFirstName", attendanceRecord.TeacherFirstName);
                        command.Parameters.AddWithValue("@TeacherLastName", attendanceRecord.TeacherLastName);
                        command.Parameters.AddWithValue("@EnableSwitch", attendanceRecord.EnableSwitch);                        
                        command.Parameters.AddWithValue("@ClockOUT", attendanceRecord.ClockOUT);
                        command.Parameters.AddWithValue("@UserID", attendanceRecord.UserID);  // Include UserID parameter

                        int rowsAffected = await command.ExecuteNonQueryAsync();
                        return rowsAffected > 0;
                    }
                }
            }
            catch (SqlException ex)
            {
                Console.WriteLine($"SQL Exception: {ex.Message}");
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception: {ex.Message}");
                return false;
            }
        }

        /*View All Attendance*/
        public async Task<IEnumerable<TeacherAttendanceViewModel>> GetTeacherAttendanceViewAsync()
        {
            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();

                // Updated query to join on TeacherFirstName
                var query = @"
            SELECT 
                t.TeacherID,
                t.TeacherFirstName,
                t.TeacherLastName,
                t.ClockIN,
                t.EnableSwitch,
                tao.ClockOUT
            FROM 
                SchoolManagement.TeachersAttendance t
            LEFT JOIN 
                SchoolManagement.TeachersAttendanceOut tao 
                ON t.TeacherFirstName = tao.TeacherFirstName
        ";

                // Execute the query and map the result to TeacherAttendanceViewModel
                return await connection.QueryAsync<TeacherAttendanceViewModel>(query);
            }
        }


        /*to filter the data in the view atttendance*/
        public async Task<IEnumerable<TeacherAttendanceViewModel>> GetFilteredTeacherAttendanceViewAsync(string teacherFirstName, DateTime startDate, DateTime endDate)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();

                var query = @"
            SELECT 
                t.TeacherID,
                t.TeacherFirstName,
                t.TeacherLastName,
                t.ClockIN,
                t.EnableSwitch,
                tao.ClockOUT
            FROM 
                SchoolManagement.TeachersAttendance t
            LEFT JOIN 
                SchoolManagement.TeachersAttendanceOut tao 
                ON t.TeacherFirstName = tao.TeacherFirstName
            WHERE 
                t.TeacherFirstName LIKE @TeacherFirstName
                AND t.ClockIN BETWEEN @StartDate AND @EndDate
        ";

                // Use Dapper to execute the query with the parameters
                return await connection.QueryAsync<TeacherAttendanceViewModel>(query, new
                {
                    TeacherFirstName = $"%{teacherFirstName}%",
                    StartDate = startDate,
                    EndDate = endDate
                });
            }
        }


        //---------Exams--------------//

        public async Task<List<string>> GetSchoolExamsCoursesAsync()
        {
            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    var query = "SELECT SchoolCourse FROM SchoolManagement.SchoolCourse";
                    return (await connection.QueryAsync<string>(query)).ToList();
                }
            }
            catch (SqlException ex)
            {
                // Log SQL exceptions
                Console.WriteLine($"SQL Exception: {ex.Message}");
            }
            catch (Exception ex)
            {
                // Log other exceptions
                Console.WriteLine($"Exception: {ex.Message}");
            }

            return new List<string>();
        }

        // Method to get students by class and its been used by Fees page
        public async Task<List<Student>> GetStudentsByClass(string classId)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                var query = "SELECT * FROM SchoolManagement.Students WHERE ClassID = @ClassID";
                var students = await connection.QueryAsync<Student>(query, new { ClassID = classId });
                return students.AsList();
            }
        }

        public async Task<List<string>> GetClassesAsync()
        {
            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    var query = "SELECT ClassID FROM SchoolManagement.Class";
                    return (await connection.QueryAsync<string>(query)).ToList();
                }
            }
            catch (SqlException ex)
            {
                // Log SQL exceptions
                Console.WriteLine($"SQL Exception: {ex.Message}");
            }
            catch (Exception ex)
            {
                // Log other exceptions
                Console.WriteLine($"Exception: {ex.Message}");
            }

            return new List<string>();
        }

        public async Task<int> InsertExamAsync(Exam exam, int userId)
        {
            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    using (var transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            // Insert Exam Record
                            var insertExamQuery = @"
                        INSERT INTO SchoolManagement.SchoolExams 
                        (StudentName, ClassName, AcademicYear, VacationDate, PromotedTo, NumberOnRoll, 
                         TermID, Position, NextTermsBegins, AttendanceOut, AttendanceIn, SchoolCourse, ClassScore, 
                         ExamsScore, TotalScore, SubjectsPositions, Grade, TeachersRemarks, Conduct, HeadmasterRemark, 
                         SchoolInformation, TeachersSignature, HeadMasterSignature, UserID) 
                        VALUES 
                        (@StudentName, @ClassName, @AcademicYear, @VacationDate, @PromotedTo, @NumberOnRoll, 
                         @TermID, @Position, @NextTermsBegins, @AttendanceOut, @AttendanceIn, @SchoolCourse, @ClassScore, 
                         @ExamsScore, @TotalScore, @SubjectsPositions, @Grade, @TeachersRemarks, @Conduct, @HeadmasterRemark, 
                         @SchoolInformation, @TeachersSignature, @HeadMasterSignature, @UserID);
                        SELECT SCOPE_IDENTITY();";

                            var examId = await connection.ExecuteScalarAsync<int>(
                                insertExamQuery, exam, transaction);

                            // Get StudentID and Current ClassID
                            var studentInfo = await connection.QueryFirstOrDefaultAsync<(int StudentID, string ClassID)>(@"
                            SELECT StudentID, ClassID 
                              FROM SchoolManagement.Students 
                               WHERE CONCAT(StudentFirstName, ' ', StudentLastName) = @StudentName",
                                     new { exam.StudentName }, transaction);

                            if (studentInfo.StudentID == 0)
                                throw new Exception("Student not found.");


                            // Update Student Class
                            var updateClassQuery = @"
                        UPDATE SchoolManagement.Students
                        SET ClassID = @PromotedTo
                        WHERE StudentID = @StudentID";

                            await connection.ExecuteAsync(updateClassQuery, new
                            {
                                PromotedTo = exam.PromotedTo,
                                StudentID = studentInfo.StudentID
                            }, transaction);

                            // Log Class Change in StudentClassHistory
                            var logHistoryQuery = @"
                        INSERT INTO SchoolManagement.StudentClassHistory 
                        (StudentID, PreviousClassID, NewClassID, UpdatedByUserID, ReasonForChange)
                        VALUES 
                        (@StudentID, @PreviousClassID, @NewClassID, @UpdatedByUserID, @ReasonForChange)";

                            await connection.ExecuteAsync(logHistoryQuery, new
                            {
                                StudentID = studentInfo.StudentID,
                                PreviousClassID = studentInfo.ClassID,
                                NewClassID = exam.PromotedTo,
                                UpdatedByUserID = userId,
                                ReasonForChange = "Promotion after exam"
                            }, transaction);

                            transaction.Commit();

                            return examId;
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            Console.WriteLine($"Transaction failed: {ex.Message}");
                            throw;
                        }
                    }
                }
            }
            catch (SqlException ex)
            {
                Console.WriteLine($"SQL Exception: {ex.Message}");
                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception: {ex.Message}");
                throw;
            }
        }


        // Method to get the list of students for a specific class
        public async Task<IEnumerable<Student>> GetStudentsByClassInExams(string classId)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();
                var sqlQuery = "SELECT * FROM SchoolManagement.Students WHERE ClassID = @ClassID";

                return await connection.QueryAsync<Student>(sqlQuery, new { ClassID = classId });
            }
        }

        // Method to get assessments for a specific student
        public async Task<IEnumerable<TeachersAssessment>> GetAssessmentByStudentAsync(string studentName, int termID)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();

                var sqlQuery = @"
            SELECT * 
            FROM SchoolManagement.TeachersAssesment 
            WHERE StudentName = @StudentName AND TermID = @TermID";

                return await connection.QueryAsync<TeachersAssessment>(sqlQuery, new { StudentName = studentName, TermID = termID });
            }
        }


        //-------Display All Teachers In School---------//
        public async Task<List<TeachersRegistration>> GetAllSchoolTeachersAsync()
        {
            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    var query = "SELECT * FROM SchoolManagement.Teacher";
                    var result = await connection.QueryAsync<TeachersRegistration>(query);

                    return result.AsList();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetAllSchoolTeachersAsync: {ex.Message}");
                throw;
            }
        }

        public async Task UpdateEmploymentStatusAsync(int teacherId, string newStatus, int changedByUserId)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();

                // Get old status
                var oldStatus = await connection.ExecuteScalarAsync<string>(
                    "SELECT EmploymentStatus FROM SchoolManagement.Teacher WHERE TeacherID = @TeacherID",
                    new { TeacherID = teacherId });

                // Update teacher table
                await connection.ExecuteAsync(
                    "UPDATE SchoolManagement.Teacher SET EmploymentStatus = @Status WHERE TeacherID = @TeacherID",
                    new { Status = newStatus, TeacherID = teacherId });

                // Insert into audit table
                await connection.ExecuteAsync(@"
            INSERT INTO SchoolManagement.TeacherEmploymentStatusHistory 
            (TeacherID, OldStatus, NewStatus, ChangedByUserID)
            VALUES (@TeacherID, @OldStatus, @NewStatus, @ChangedByUserID)",
                    new
                    {
                        TeacherID = teacherId,
                        OldStatus = oldStatus,
                        NewStatus = newStatus,
                        ChangedByUserID = changedByUserId
                    });
            }
        }


        //-------Display All Students------//
        public async Task<List<Student>> GetAllStudents()
        {
            return await _cacheService.GetOrSetAsync("AllStudents", async () =>
            {
                try
                {
                    using (var connection = new SqlConnection(connectionString))
                    {
                        await connection.OpenAsync();

                        var query = @"
                    SELECT s.*,
                           d.DiscountType,
                           d.DiscountValue
                    FROM SchoolManagement.Students s
                    LEFT JOIN SchoolManagement.StudentDiscounts d 
                        ON s.StudentID = d.StudentID AND d.IsActive = 1
                    ORDER BY s.StudentFirstName ASC, s.StudentLastName ASC";

                        var result = await connection.QueryAsync<Student>(query);

                        return result.AsList();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error in GetAllStudents: {ex.Message}");
                    throw;
                }
            }, minutes: 20);
        }


        //-----Total Number of Students displayed in the Card---//
        public async Task<int> GetTotalStudentsCount()
        {
            return await _cacheService.GetOrSetAsync("TotalStudentCount", async () =>
            {
                int totalStudents = 0;

                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    string query = "SELECT COUNT(*) FROM SchoolManagement.Students";
                    SqlCommand command = new SqlCommand(query, connection);

                    try
                    {
                        await connection.OpenAsync();
                        totalStudents = (int)await command.ExecuteScalarAsync();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error in GetTotalStudentsCount: {ex.Message}");
                        throw;
                    }
                }

                return totalStudents;
            }, minutes: 10); // Cache for 10 minutes (adjust as needed)
        }

        public async Task<decimal> GetTotalBankTransferredAsync()
        {
            return await _cacheService.GetOrSetAsync("TotalBankTransferred", async () =>
            {
                using var connection = new SqlConnection(connectionString);
                var query = "SELECT ISNULL(SUM(AmountTransferred), 0) FROM SchoolManagement.Bank";
                return await connection.ExecuteScalarAsync<decimal>(query);
            }, minutes: 10); // Cache for 10 minutes (adjust as needed)
        }

        public async Task SetCurrentTermAsync(int termId, DateTime endDate, int userId)
        {
            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync(); // Open the connection before starting transaction
            using var transaction = connection.BeginTransaction();

            try
            {
                // Reset current term
                await connection.ExecuteAsync(
                    "UPDATE SchoolManagement.SchoolTerm SET IsCurrentTerm = 0",
                    transaction: transaction);

                // Set new current term
                await connection.ExecuteAsync(@"
            UPDATE SchoolManagement.SchoolTerm
            SET IsCurrentTerm = 1, TermEndDate = @EndDate
            WHERE TermID = @TermID",
                    new { TermID = termId, EndDate = endDate },
                    transaction: transaction);

                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public async Task<SchoolTerm> GetCurrentTermAsync()
        {
            using var connection = new SqlConnection(connectionString);
            return await connection.QueryFirstOrDefaultAsync<SchoolTerm>(
                "SELECT * FROM SchoolManagement.SchoolTerm WHERE IsCurrentTerm = 1");
        }

        public async Task<int> GetTotalTeachersCount()
        {
            return await _cacheService.GetOrSetAsync("TotalTeachersCount", async () =>
            {
                int totalTeachers = 0;

                using (var connection = new SqlConnection(connectionString))
                {
                    const string query = "SELECT COUNT(*) FROM SchoolManagement.Teacher";
                    var command = new SqlCommand(query, connection);

                    try
                    {
                        await connection.OpenAsync();
                        totalTeachers = (int)await command.ExecuteScalarAsync();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error in GetTotalTeachersCount: {ex.Message}");
                        throw;
                    }
                }

                return totalTeachers;
            }, minutes: 15); // Cache duration can be adjusted as needed
        }
        public async Task<int> GetStaffsCount()
        {
            int totalStudents = 0;

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                string query = "SELECT COUNT(*) FROM SchoolManagement.Staff";
                SqlCommand command = new SqlCommand(query, connection);

                try
                {
                    connection.Open();
                    totalStudents = (int)command.ExecuteScalar();
                }
                catch (Exception ex)
                {
                    // Handle exception
                }
            }

            return totalStudents;
        }

        //-- Display Logs of intries into the system--//
        public async Task<IEnumerable<UserLog>> GetUserLogsAsync()
        {
            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    var query = @"SELECT LogId, UserId, EventName, Timestamp FROM SchoolManagement.UserLog";
                    return await connection.QueryAsync<UserLog>(query);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetUserLogsAsync: {ex.Message}");
                throw;
            }
        }

        //------Report Page------//
        public async Task<List<ReportViewPage>> GetAllReportViewPagesAsync()
        {
            List<ReportViewPage> reportViewPages = new List<ReportViewPage>();

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                string query = "SELECT ReportViewPageID, Title, Description FROM SchoolManagement.ReportViewPage";
                SqlCommand command = new SqlCommand(query, connection);
                await connection.OpenAsync();
                SqlDataReader reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    ReportViewPage reportViewPage = new ReportViewPage
                    {
                        ReportViewPageID = reader.GetInt32(0),
                        Title = reader.GetString(1),
                        Description = reader.GetString(2)
                    };
                    reportViewPages.Add(reportViewPage);
                }
            }

            return reportViewPages;
        }

        //-------FOR LESSON NOTE-----------//
        public async Task<int> AddLessonNoteAsync(LessonNote lessonNote)
        {
            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    var query = @"INSERT INTO SchoolManagement.TEACHERSLESSONNOTES 
                (ClassID, UserId, SchoolCourse, Strand, SubStrand, ContentStandard, Indicator, 
                 TeachingLearningResources, TeachingLearningResourcePreparationNotes, 
                 SourcesLearningResources, LearningGroup, LearnerExpectation, 
                 ImportantGradeExpectation, LearningOutcomes, FormofAssessment, 
                 LearnerEntryBehavior, SequenceofLesson, TermID, LessonNoteDate, Time_Period, Status, UpdatedBy, UpdatedOn) 
                VALUES (@ClassID, @UserId, @SchoolCourse, @Strand, @SubStrand, @ContentStandard, 
                        @Indicator, @TeachingLearningResources, @TeachingLearningResourcePreparationNotes, 
                        @SourcesLearningResources, @LearningGroup, @LearnerExpectation, 
                        @ImportantGradeExpectation, @LearningOutcomes, @FormofAssessment, 
                        @LearnerEntryBehavior, @SequenceofLesson, @TermID, @LessonNoteDate, @Time_Period, @Status, @UpdatedBy, @UpdatedOn)";

                    return await connection.ExecuteAsync(query, new
                    {
                        lessonNote.ClassID,
                        lessonNote.UserId,
                        lessonNote.SchoolCourse,
                        lessonNote.Strand,
                        lessonNote.SubStrand,
                        lessonNote.ContentStandard,
                        lessonNote.Indicator,
                        lessonNote.TeachingLearningResources,
                        lessonNote.TeachingLearningResourcePreparationNotes,
                        lessonNote.SourcesLearningResources,
                        lessonNote.LearningGroup,
                        lessonNote.LearnerExpectation,
                        lessonNote.ImportantGradeExpectation,
                        lessonNote.LearningOutcomes,
                        lessonNote.FormofAssessment,
                        lessonNote.LearnerEntryBehavior,
                        lessonNote.SequenceofLesson,
                        lessonNote.TermID,
                        lessonNote.LessonNoteDate,
                        lessonNote.Time_Period,
                        Status = 1, // Assuming you want to set a default status
                        UpdatedBy = lessonNote.UserId, // Assuming UpdatedBy is the same as UserId
                        UpdatedOn = DateTime.UtcNow
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in AddLessonNoteAsync: {ex.Message}");
                throw;
            }
        }


        public async Task<IEnumerable<LessonNote>> GetLessonNotesByUserIdAsync(int userId)
        {
            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    var query = "SELECT * FROM SchoolManagement.lessonnotes WHERE UserId = @UserId";

                    return await connection.QueryAsync<LessonNote>(query, new { UserId = userId });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetLessonNotesByUserIdAsync: {ex.Message}");
                throw;
            }
        }

        public async Task<LessonNote> GetLessonNoteByIdAsync(int lessonNoteId)
        {
            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    var query = "SELECT * FROM SchoolManagement.lessonnotes WHERE LessonnotesID = @LessonNoteId";

                    return await connection.QueryFirstOrDefaultAsync<LessonNote>(query, new { LessonNoteId = lessonNoteId });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetLessonNoteByIdAsync: {ex.Message}");
                throw;
            }
        }

        public async Task<List<SchoolCourse>> GetLessonNotSchoolCoursesAsync()
        {
            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    var query = "SELECT SCID, SchoolCourse AS SchoolCourseName FROM SchoolManagement.SchoolCourse";
                    return (await connection.QueryAsync<SchoolCourse>(query)).ToList();
                }
            }
            catch (SqlException ex)
            {
                // Log SQL exceptions
                Console.WriteLine($"SQL Exception: {ex.Message}");
            }
            catch (Exception ex)
            {
                // Log other exceptions
                Console.WriteLine($"Exception: {ex.Message}");
            }

            return new List<SchoolCourse>();
        }

        //filter the db to pull out the date range
        public async Task<List<TEACHERSLESSONNOTES>> GetLessonNotesByDateRangeAsync(DateTime fromDate, DateTime toDate)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                string sql = "SELECT * FROM SchoolManagement.TEACHERSLESSONNOTES WHERE RecDateCreated BETWEEN @FromDate AND @ToDate";
                return (await connection.QueryAsync<TEACHERSLESSONNOTES>(sql, new { FromDate = fromDate, ToDate = toDate })).ToList();
            }
        }


        //------------------Teachers-Task------------------------------//

        public async Task<IEnumerable<TeachersTask>> GetAllTasksAsync()
        {
            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    return await connection.QueryAsync<TeachersTask>("SELECT * FROM SchoolManagement.TeachersTask");
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error fetching tasks from database.", ex);
            }
        }

        public async Task<TeachersTask> GetTaskByIdAsync(int taskId)
        {
            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    return await connection.QuerySingleOrDefaultAsync<TeachersTask>(
                        "SELECT * FROM SchoolManagement.TeachersTask WHERE TeacherTaskID = @TaskId", new { TaskId = taskId });
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error fetching task with ID {taskId} from database.", ex);
            }
        }

        public async Task AddTaskAsync(TeachersTask task)
        {
            try
            {
                var userExists = await DoesUserExistAsync(task.UserID);
                if (!userExists)
                {
                    throw new Exception($"User with ID {task.UserID} does not exist.");
                }

                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    var query = @"
                INSERT INTO SchoolManagement.TeachersTask (TeachersName, TeacherTask, SwitchBar, StartDate, EndDate, Status, UserID)
                VALUES (@TeachersName, @TeacherTask, @SwitchBar, @StartDate, @EndDate, @Status, @UserID);
            ";

                    await connection.ExecuteAsync(query, task);
                }
            }
            catch (SqlException ex)
            {
                Console.WriteLine($"SQL Exception: {ex.Message}");
                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception: {ex.Message}");
                throw;
            }
        }

        private async Task<bool> DoesUserExistAsync(int userId)
        {
            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    var query = "SELECT COUNT(*) FROM SchoolManagement.Users WHERE UserID = @UserId";
                    var count = await connection.ExecuteScalarAsync<int>(query, new { UserId = userId });

                    return count > 0;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception: {ex.Message}");
                throw;
            }
        }

        public async Task UpdateTaskAsync(TeachersTask task)
        {
            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.ExecuteAsync(
                        "UPDATE SchoolManagement.TeachersTask SET TeachersName = @TeachersName, TeacherTask = @TeacherTask, SwitchBar = @SwitchBar, StartDate = @StartDate, EndDate = @EndDate, Status = @Status, UserID = @UserID WHERE TeacherTaskID = @TeacherTaskID",
                        task);
                }
            }
            catch (SqlException ex)
            {
                throw new Exception("Error updating task in database.", ex);
            }
        }

        public async Task DeleteTaskAsync(int taskId)
        {
            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.ExecuteAsync("DELETE FROM SchoolManagement.TeachersTask WHERE TeacherTaskID = @TaskId", new { TaskId = taskId });
                }
            }
            catch (SqlException ex)
            {
                throw new Exception("Error deleting task from database.", ex);
            }
        }

        //----For viewing Lesson Note submitted in the View lesson note dialog----//
        public async Task<IEnumerable<TEACHERSLESSONNOTES>> GetSubmittedLessonNotesAsync()
        {
            using var connection = new SqlConnection(connectionString);
            var sql = @"
        SELECT ln.LessonnotesID, ln.UserId, u.UserName, ln.SchoolCourse, ln.Strand, ln.SubStrand, 
               ln.ContentStandard, ln.Indicator, ln.TeachingLearningResources, 
               ln.TeachingLearningResourcePreparationNotes, ln.SourcesLearningResources, 
               ln.LearningGroup, ln.LearnerExpectation, ln.ImportantGradeExpectation, 
               ln.LearningOutcomes, ln.FormofAssessment, ln.LearnerEntryBehavior, 
               ln.SequenceofLesson, ln.Status, ln.UpdatedOn
        FROM SchoolManagement.TEACHERSLESSONNOTES ln
        JOIN SchoolManagement.Users u ON ln.UserId = u.UserID
        WHERE ln.Status = @Status
        ORDER BY ln.UpdatedOn DESC";

            return await connection.QueryAsync<TEACHERSLESSONNOTES>(sql, new { Status = Lesson_Note_Dialog_Status.New });
        }

        public async Task UpdateLessonNoteStatusAsync(TEACHERSLESSONNOTES note, int userId)
        {
            // Ensure the status is valid before proceeding
            if (!Enum.IsDefined(typeof(Lesson_Note_Dialog_Status), note.Status))
            {
                throw new ArgumentException($"Invalid status value: {note.Status}. Valid values are: {string.Join(", ", Enum.GetValues(typeof(Lesson_Note_Dialog_Status)).Cast<Lesson_Note_Dialog_Status>())}");
            }

            using (var connection = new SqlConnection(connectionString))
            {
                string query = @"
            UPDATE SchoolManagement.TEACHERSLESSONNOTES
            SET Status = @Status,
                UpdatedBy = @UpdatedBy,
                UpdatedOn = @UpdatedOn
            WHERE LessonnotesID = @LessonnotesID";

                await connection.ExecuteAsync(query, new
                {
                    note.Status,
                    UpdatedBy = userId,
                    UpdatedOn = DateTime.Now,
                    note.LessonnotesID
                });
            }
        }

        public async Task<IEnumerable<LessonNote>> GetViewStatusby()
        {
            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    string query = @"
                SELECT l.UpdatedBy, u.FullName AS UpdatedByName, l.UpdatedOn
                FROM SchoolManagement.TEACHERSLESSONNOTES l
                JOIN SchoolManagement.Users u ON l.UpdatedBy = u.UserID";

                    return await connection.QueryAsync<LessonNote>(query);
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error fetching status updates from database.", ex);
            }
        }

        public async Task<List<Class>> GetClassforlessonnotepageAsync()
        {
            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    var query = "SELECT ClassID, ClassName FROM SchoolManagement.Class"; // Ensure you have ClassName in your DB
                    return (await connection.QueryAsync<Class>(query)).ToList(); // Update to return a list of Class objects
                }
            }
            catch (SqlException ex)
            {
                Console.WriteLine($"SQL Exception: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception: {ex.Message}");
            }

            return new List<Class>();
        }



        //----For Teacher Assesment--//

        public async Task<IEnumerable<Class>> GetClasssesAsync()
        {
            using (var connection = new SqlConnection(connectionString))
            {
                var sql = "SELECT * FROM SchoolManagement.Class";
                return await connection.QueryAsync<Class>(sql);
            }
        }

        public async Task<IEnumerable<Student>> GetStudentsByClasssAsync(string classID)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                var sql = "SELECT * FROM SchoolManagement.Students WHERE ClassID = @ClassID";
                return await connection.QueryAsync<Student>(sql, new { ClassID = classID });
            }
        }

        public async Task<int> SaveAssessmentAsync(TeachersAssessment assessment)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                var sql = @"
            INSERT INTO SchoolManagement.TeachersAssesment (StudentName, ClassID, TEST1, TEST2, GROUPWORK, HOMEWORK, CLASSTEST, TOTAL_X, EXAMS_SCORE, Y, X_Y, POSITION, UserID, Course, TermID)
            VALUES (@StudentName, @ClassID, @TEST1, @TEST2, @GROUPWORK, @HOMEWORK, @CLASSTEST, @TOTAL_X, @EXAMS_SCORE, @Y, @X_Y, @POSITION, @UserID, @Course, @TermID)";

                return await connection.ExecuteAsync(sql, assessment);
            }
        }


        /*For viewing teachers TimeTable*/

        public async Task<IEnumerable<Schedule>> GetAllSchedulesAsync()
        {
            using (var connection = new SqlConnection(connectionString))
            {
                string query = "SELECT * FROM SchoolManagement.StudentTimetable_Schedule";
                return await connection.QueryAsync<Schedule>(query);
            }
        }

        public async Task<Schedule> GetScheduleByIdAsync(int scheduleId)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                string query = "SELECT * FROM SchoolManagement.StudentTimetable_Schedule WHERE ScheduleID = @ScheduleID";
                return await connection.QueryFirstOrDefaultAsync<Schedule>(query, new { ScheduleID = scheduleId });
            }
        }

        public async Task<IEnumerable<Day>> GetDaysAsync()
        {
            using (var connection = new SqlConnection(connectionString))
            {
                var query = "SELECT DayID, DayName FROM SchoolManagement.StudentTimetable_Days";
                return await connection.QueryAsync<Day>(query);
            }
        }

        public async Task AddScheduleAsync(Schedule schedule)
        {
            // Validate user input for time
            if (schedule.SubjectStartTime >= schedule.SubjectEndTime)
            {
                throw new Exception("Start time must be before end time.");
            }

            using (var connection = new SqlConnection(connectionString))
            {
                // Check if DayID exists
                var dayExists = await connection.ExecuteScalarAsync<int>(
                    "SELECT COUNT(*) FROM SchoolManagement.StudentTimetable_Days WHERE DayID = @DayID",
                    new { schedule.DayID });

                if (dayExists == 0)
                {
                    throw new Exception("The specified DayID does not exist.");
                }

                // Optionally, check for conflicts with existing schedules
                var hasConflict = await CheckScheduleConflictAsync(schedule, connection);
                if (hasConflict)
                {
                    throw new Exception("The specified time conflicts with an existing schedule.");
                }

                var query = @"
                  INSERT INTO SchoolManagement.StudentTimetable_Schedule 
                 (SCID, ClassID, SubjectStartTime, SubjectEndTime, DayID, BeforeFirstBreak, AfterFirstBreak, AfterSecondBreak) 
                   VALUES 
                  (@SCID, @ClassID, @SubjectStartTime, @SubjectEndTime, @DayID, @BeforeFirstBreak, @AfterFirstBreak, @AfterSecondBreak)";

                await connection.ExecuteAsync(query, new
                {
                    schedule.SCID,
                    schedule.ClassID,
                    schedule.SubjectStartTime,
                    schedule.SubjectEndTime,
                    schedule.DayID,
                    schedule.BeforeFirstBreak,
                    schedule.AfterFirstBreak,
                    schedule.AfterSecondBreak
                });

            }
        }
        // Example method to check for schedule conflicts
        private async Task<bool> CheckScheduleConflictAsync(Schedule schedule, SqlConnection connection)
        {
            var query = @"
 SELECT COUNT(*) 
 FROM SchoolManagement.StudentTimetable_Schedule 
 WHERE DayID = @DayID 
 AND ((CAST(SubjectStartTime AS time) < CAST(@SubjectEndTime AS time)) 
       AND (CAST(SubjectEndTime AS time) > CAST(@SubjectStartTime AS time)))";

            var count = await connection.ExecuteScalarAsync<int>(query, new
            {
                schedule.DayID,
                SubjectStartTime = schedule.SubjectStartTime.TimeOfDay,
                SubjectEndTime = schedule.SubjectEndTime.TimeOfDay
            });

            return count > 0;
        }



        public async Task UpdateScheduleAsync(Schedule schedule)
        {
            var query = @"
             UPDATE SchoolManagement.StudentTimetable_Schedule
             SET SCID = @SCID, 
             ClassID = @ClassID,
             SubjectStartTime = @SubjectStartTime,
               SubjectEndTime = @SubjectEndTime,
              DayID = @DayID,
              BeforeFirstBreak = @BeforeFirstBreak,
             AfterFirstBreak = @AfterFirstBreak,
             AfterSecondBreak = @AfterSecondBreak
             WHERE ScheduleID = @ScheduleID;";

            using (var connection = new SqlConnection(connectionString))
            {
                await connection.ExecuteAsync(query, schedule);
            }
        }


        public async Task<int> DeleteScheduleAsync(int scheduleId)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                string query = "DELETE FROM SchoolManagement.StudentTimetable_Schedule WHERE ScheduleID = @ScheduleID";
                return await connection.ExecuteAsync(query, new { ScheduleID = scheduleId });
            }
        }

        public async Task<IEnumerable<string>> GetClassIDsAsync()
        {
            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();
                var result = await connection.QueryAsync<string>("SELECT ClassID FROM SchoolManagement.Class");
                return result.ToList();
            }
        }


        //Assigning Teachers To their Subject
        public async Task<bool> AssignTeacherToSubject(TeacherSubjectAssignment assignment)
        {
            const string sql = @"
                INSERT INTO SchoolManagement.TeacherSubjectAssignment (
                    TeacherID, SCID, ClassID, DayID, SubjectStartTime, 
                    SubjectEndTime, RecDateCreated)
                VALUES (
                    @TeacherID, @SCID, @ClassID, @DayID, @SubjectStartTime, 
                    @SubjectEndTime, GETDATE())";

            using (var connection = new SqlConnection(connectionString))
            {
                var result = await connection.ExecuteAsync(sql, assignment);
                return result > 0;
            }
        }

        public async Task<List<TeachersRegistration>> GetTeachers()
        {
            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    var query = "SELECT TeacherID, TeacherFirstName, TeacherLastName FROM SchoolManagement.Teacher";

                    using (var command = new SqlCommand(query, connection))
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        var teachers = new List<TeachersRegistration>();

                        while (await reader.ReadAsync())
                        {
                            var teacher = new TeachersRegistration
                            {
                                TeacherID = reader.GetInt32(0), // Ensure this matches the correct index
                                TeacherFirstName = reader.GetString(1),
                                TeacherLastName = reader.GetString(2)
                            };

                            teachers.Add(teacher);
                        }

                        return teachers;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetTeachers: {ex.Message}");
                throw;
            }
        }


        public async Task<IEnumerable<Class>> GetTClassesAsync()
        {
            const string sql = "SELECT ClassID FROM SchoolManagement.Class";
            using (var connection = new SqlConnection(connectionString))
            {
                return await connection.QueryAsync<Class>(sql);
            }
        }

        public async Task<List<SchoolCourse>> GetsSchoolCoursesAsync()
        {
            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    var query = "SELECT SCID, SchoolCourse FROM SchoolManagement.SchoolCourse";

                    using (var command = new SqlCommand(query, connection))
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        var schoolCourses = new List<SchoolCourse>();

                        while (await reader.ReadAsync())
                        {
                            var schoolCourse = new SchoolCourse
                            {
                                SCID = reader.IsDBNull(0) ? null : reader.GetString(0),
                                SchoolCourseName = reader.IsDBNull(1) ? null : reader.GetString(1)
                            };

                            schoolCourses.Add(schoolCourse);
                        }

                        return schoolCourses;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetSchoolCoursesAsync: {ex.Message}");
                throw;
            }
        }

        //TO VIEW TEACHERS SUBJECT ASSIGN//
        public async Task<TeachersRegistration> GetTeacherById(int teacherId)
        {
            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    var query = "SELECT TeacherID, TeacherFirstName, TeacherLastName FROM SchoolManagement.Teacher WHERE TeacherID = @TeacherID";
                    return await connection.QuerySingleOrDefaultAsync<TeachersRegistration>(query, new { TeacherID = teacherId });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetTeacherById: {ex.Message}");
                throw;
            }
        }


        public async Task<List<AssignmentDetails>> GetTeacherAssignments(int teacherId)
        {
            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    var query = @"
                SELECT 
                    d.DayName,
                    a.ClassID,
                    s.SchoolCourse AS SchoolCourseName,
                    a.SubjectStartTime,
                    a.SubjectEndTime
                FROM SchoolManagement.TeacherSubjectAssignment a
                JOIN SchoolManagement.StudentTimetable_Days d ON a.DayID = d.DayID
                JOIN SchoolManagement.SchoolCourse s ON a.SCID = s.SCID
                WHERE a.TeacherID = @TeacherID";

                    var assignments = await connection.QueryAsync<AssignmentDetails>(query, new { TeacherID = teacherId });
                    return assignments.ToList();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetTeacherAssignments: {ex.Message}");
                throw;
            }
        }

        //View Errors From The Mobile App//
        public async Task<IEnumerable<User_Log>> GetErrorsFromMobileAppAsync()
        {
            using (var connection = new SqlConnection(connectionString))
            {
                return await connection.QueryAsync<User_Log>("SELECT * FROM SchoolManagementSecurity.ErrorLog");
            }
        }


        //-------------------------------------------------------------------------------------------------//
        // Method to add a new notice//
        public async Task AddNoticeAsync(string title, string content, string author, DateTime? expiryDate = null)
        {
            const string sql = @"
                INSERT INTO SchoolManagement.NoticeBoard (Title, Content, Author, DatePosted, ExpiryDate, IsActive)
                VALUES (@Title, @Content, @Author, GETDATE(), @ExpiryDate, 1)";

            using (var connection = new SqlConnection(connectionString))
            {
                await connection.ExecuteAsync(sql, new { Title = title, Content = content, Author = author, ExpiryDate = expiryDate });
            }
        }

        // Method to deactivate a notice by its ID
        public async Task DeactivateNoticeAsync(int noticeId)
        {
            const string sql = @"
                UPDATE SchoolManagement.NoticeBoard
                SET IsActive = 0
                WHERE NoticeID = @NoticeID";

            using (var connection = new SqlConnection(connectionString))
            {
                await connection.ExecuteAsync(sql, new { NoticeID = noticeId });
            }
        }

        // Method to get all active notices
        public async Task<IEnumerable<Notice>> GetActiveNoticesAsync()
        {
            const string sql = @"
                SELECT NoticeID, Title, Content, Author, DatePosted, ExpiryDate
                FROM SchoolManagement.NoticeBoard
                WHERE IsActive = 1";

            using (var connection = new SqlConnection(connectionString))
            {
                return await connection.QueryAsync<Notice>(sql);
            }
        }

        /*specificaly for Message to teacher page to use this*/
        public async Task<IEnumerable<Role>> GetNotesRolesAsync()
        {
            using (var connection = new SqlConnection(connectionString))
            {
                return await connection.QueryAsync<Role>("SELECT RoleID, RoleName FROM SchoolManagement.Roles");
            }
        }


        // Method to get users by selected RoleID
        public async Task<IEnumerable<User>> GetUsersByRoleAsync(int roleId)
        {
            const string sql = @"
            SELECT u.UserID, u.UserName, r.RoleName
            FROM SchoolManagement.Users u
            INNER JOIN SchoolManagement.MobileAppRoles mr ON u.UserID = mr.UserID
            INNER JOIN SchoolManagement.Roles r ON mr.RoleName = r.RoleName
            WHERE r.RoleID = @RoleID AND mr.Enable = 1";

            using (var connection = new SqlConnection(connectionString))
            {
                return await connection.QueryAsync<User>(sql, new { RoleID = roleId });
            }
        }

        // Method to send a message to all users in the selected role
        public async Task<bool> SendMessageAsync(string title, string content, int roleId, IEnumerable<int> userIds)
        {
            const string sql = @"
    INSERT INTO SchoolManagement.MessagesToRoleUsers (UserID, RoleID, Title, Content, DateSent)
    VALUES (@UserID, @RoleID, @Title, @Content, GETDATE())";

            using (var connection = new SqlConnection(connectionString))
            {
                try
                {
                    foreach (var userId in userIds)
                    {
                        await connection.ExecuteAsync(sql, new { UserID = userId, RoleID = roleId, Title = title, Content = content });
                    }
                    return true;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error sending message: {ex.Message}");
                    return false;
                }
            }
        }


        // Method to retrieve messages for a specific user
        public async Task<IEnumerable<MESSAGE>> GetMessagesForUserAsync(int userId)
        {
            const string sql = @"
            SELECT MessageID, UserID, Title, Content, DateSent
            FROM SchoolManagement.MessagesToRoleUsers
            WHERE UserID = @UserID";

            using (var connection = new SqlConnection(connectionString))
            {
                return await connection.QueryAsync<MESSAGE>(sql, new { UserID = userId });
            }
        }
        //-------------------------------------------------------------------------//
        //SERVICE TO MARK LESSE=ON NOTES//

        /*PHOTORECORDS*/
        public async Task SavePhotoRecordAsync(PhotoRecord photoRecord)
        {
            const string sql = @"
        INSERT INTO SchoolManagement.PhotoRecords 
        (Class, StudentName, StudentID, PhotoTitle, PhotoDescription, PhotoData) 
        VALUES 
        (@Class, @StudentName, @StudentID, @PhotoTitle, @PhotoDescription, @PhotoData)";

            using (var connection = new SqlConnection(connectionString))
            {
                await connection.ExecuteAsync(sql, photoRecord);
            }
        }

        public async Task<Student> GetStudentByFullNameAsync(string fullName, string classId)
        {
            var query = @"
        SELECT TOP 1 *
        FROM SchoolManagement.Students
        WHERE CONCAT(StudentFirstName, ' ', StudentLastName) = @FullName
        AND ClassID = @ClassID";

            using (var connection = new SqlConnection(connectionString))
            {
                return await connection.QueryFirstOrDefaultAsync<Student>(query, new { FullName = fullName, ClassID = classId });
            }
        }

        /*School Fees Implimentation*/
        // Get all fee types
        public async Task<List<FeeType>> GetFeeTypesAsync()
        {
            return await _cacheService.GetOrSetAsync("AllFeeTypes", async () =>
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    const string query = @"SELECT FeeTypeID, FeeTypeName, Description, Amount, ClassID, RecDateCreated 
                                   FROM SchoolManagement.FeeTypes";

                    var feeTypes = await connection.QueryAsync<FeeType>(query);
                    return feeTypes.AsList();
                }
            }, minutes: 20); // Cache for 20 minutes
        }

        // Get all student fees
        public async Task<IEnumerable<StudentFee>> GetStudentFeesByNameAsync(string studentName)
        {
            const string query = @"
        SELECT FeeID, StudentID, FeeTypeID, StudentName, FeeTypeName, ClassID, 
               AmountPaid, AmountLeft, PaymentDate, DueDate, Note, UserID, RecDateCreated
        FROM SchoolManagement.StudentFees
        WHERE StudentName = @StudentName";

            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    return await connection.QueryAsync<StudentFee>(query, new { StudentName = studentName });
                }
            }
            catch (Exception ex)
            {
                // Log or handle exception
                Console.WriteLine($"Error fetching fees for StudentName {studentName}: {ex.Message}");
                return Enumerable.Empty<StudentFee>(); // Return an empty collection on failure
            }
        }

        //public async Task<IEnumerable<StudentFee>> GetStudentFeesByStudentIdAsync(int studentId)
        //{
        //    const string query = "EXEC GetStudentFees @StudentID";

        //    try
        //    {
        //        using (var connection = new SqlConnection(connectionString))
        //        {
        //            await connection.OpenAsync();
        //            return await connection.QueryAsync<StudentFee>(query, new { StudentID = studentId });
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        // Log or handle exception
        //        Console.WriteLine($"Error fetching fees for StudentID {studentId}: {ex.Message}");
        //        return Enumerable.Empty<StudentFee>(); // Return an empty collection on failure
        //    }
        //}
        public async Task<IEnumerable<StudentFee>> GetStudentFeesByStudentIdAsync(int studentId)
        {
            using var connection = new SqlConnection(connectionString);
            var result = await connection.QueryAsync<StudentFee>(
                "GetStudentFees",
                new { StudentID = studentId },
                commandType: CommandType.StoredProcedure);
            return result;
        }

        public async Task<IEnumerable<StudentFee>> GetStudentFeesByStudentIdandTermAsync(int studentId, int termId)
        {
            using var connection = new SqlConnection(connectionString);
            var result = await connection.QueryAsync<StudentFee>(
                "GetStudentFeesByStudentAndTerm",
                new { StudentID = studentId, TermID = termId },
                commandType: CommandType.StoredProcedure);
            return result;
        }

        public async Task<(string CompanyName, string SchoolName, string CompanyImageBase64)> GetPdfLoginScreenDetailsAsync()
        {
            using var connection = new SqlConnection(connectionString);

            var result = await connection.QueryFirstOrDefaultAsync<LoginScreenDetail>(
                "SELECT TOP 1 CompanyRegisteredName, SchoolName, CompanyImage FROM SchoolManagement.LoginScreenDetails"
            );

            string base64Image = result?.CompanyImage != null ? Convert.ToBase64String(result.CompanyImage) : null;

            return (
                result?.CompanyRegisteredName ?? "Company Name",
                result?.SchoolName ?? "School Name",
                base64Image
            );
        }

        // Get student fees by term and payment date
        public async Task<IEnumerable<StudentFee>> GetStudentFeesByTermAndPaymentDateAsync(int termId, DateTime paymentDate)
        {
            using var connection = new SqlConnection(connectionString);
            var result = await connection.QueryAsync<StudentFee>(
                "GetStudentFeesByTermAndPaymentDate",
                new { TermID = termId, PaymentDate = paymentDate.Date },
                commandType: CommandType.StoredProcedure);
            return result;
        }


        public async Task<LoginScreenDetail> GetsLoginScreenDetailsAsync()
        {
            using var connection = new SqlConnection(connectionString);
            string sql = "SELECT TOP 1 * FROM SchoolManagement.LoginScreenDetails";
            return await connection.QueryFirstOrDefaultAsync<LoginScreenDetail>(sql);
        }

        public async Task InsertOrUpdateLoginScreenDetailsAsync(LoginScreenDetail data)
        {
            using var connection = new SqlConnection(connectionString);

            string checkSql = "SELECT COUNT(*) FROM SchoolManagement.LoginScreenDetails";
            int count = await connection.ExecuteScalarAsync<int>(checkSql);

            if (count == 0)
            {
                string insertSql = @"
            INSERT INTO SchoolManagement.LoginScreenDetails 
            (Title, CompanyImage, SchoolName, CompanyRegisteredName, SoftWareVerssion)
            VALUES (@Title, @CompanyImage, @SchoolName, @CompanyRegisteredName, @SoftWareVerssion)";
                await connection.ExecuteAsync(insertSql, data);
            }
            else
            {
                string updateSql = @"
            UPDATE SchoolManagement.LoginScreenDetails 
            SET Title = @Title, CompanyImage = @CompanyImage, 
                SchoolName = @SchoolName, CompanyRegisteredName = @CompanyRegisteredName,
                SoftWareVerssion = @SoftWareVerssion";
                await connection.ExecuteAsync(updateSql, data);
            }
        }


        public async Task<List<FeeType>> GetFeeTypesByClassAsync(string classId)
        {
            return await _cacheService.GetOrSetAsync($"FeeTypes_ByClass_{classId}", async () =>
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    const string query = @"
                    SELECT FeeTypeID, FeeTypeName, Description, Amount, ClassID, RecDateCreated
                    FROM SchoolManagement.FeeTypes
                    WHERE ClassID = @ClassID 
                    AND DeletedBy IS NULL"; // Only non-deleted fee types

                    var feeTypes = await connection.QueryAsync<FeeType>(query, new { ClassID = classId });
                    return feeTypes.AsList();
                }
            }, minutes: 15); // Cache each class-specific result for 15 minutes
        }



        /*logic to save fees*/
        //public async Task<bool> SaveStudentFeeAsync(StudentFee studentFee)
        //{
        //    try
        //    {
        //        using var connection = new SqlConnection(connectionString);
        //        await connection.OpenAsync();

        //        using var transaction = connection.BeginTransaction();

        //        // Step 1: Insert the student fee payment
        //        const string insertQuery = @"
        //INSERT INTO SchoolManagement.StudentFees 
        //(StudentID, FeeTypeID, StudentName, FeeTypeName, ClassID, AmountPaid, AmountLeft, PaymentDate, Note, UserID, PaymentMethod, TermID)
        //VALUES (@StudentID, @FeeTypeID, @StudentName, @FeeTypeName, @ClassID, @AmountPaid, @AmountLeft, @PaymentDate, @Note, @UserID, @PaymentMethod, @TermID)";

        //        var result = await connection.ExecuteAsync(insertQuery, studentFee, transaction: transaction);

        //        if (result > 0)
        //        {
        //            // Step 2: Run the stored procedure to recalculate AmountLeft
        //            await connection.ExecuteAsync("EXEC SchoolManagement.FixStudentFeeAmounts", transaction: transaction);

        //            // Step 3: Commit
        //            transaction.Commit();
        //            return true;
        //        }
        //        else
        //        {
        //            transaction.Rollback();
        //            return false;
        //        }
        //    }
        //    catch (SqlException ex)
        //    {
        //        Console.WriteLine($"SQL Error: {ex.Message}");
        //        throw new ApplicationException("An error occurred while saving the student fee.", ex);
        //    }
        //}

        public async Task<bool> SaveStudentFeeAsync(StudentFee studentFee)
        {
            const int maxRetries = 3;
            int retryCount = 0;

            while (true)
            {
                try
                {
                    return await SaveStudentFeeInternalAsync(studentFee);
                }
                catch (SqlException ex) when (ex.Number == 1205) // 1205 = Deadlock
                {
                    retryCount++;
                    Console.WriteLine($"Deadlock encountered. Retrying attempt {retryCount}...");

                    if (retryCount >= maxRetries)
                    {
                        Console.WriteLine("Max retries reached. Aborting operation.");
                        throw new ApplicationException("Deadlock could not be resolved after multiple attempts.", ex);
                    }

                    await Task.Delay(500); // Optional delay to give SQL Server time to resolve locks
                }
                catch (SqlException ex)
                {
                    Console.WriteLine($"SQL Error: {ex.Message}");
                    throw new ApplicationException("An error occurred while saving the student fee.", ex);
                }
            }
        }

        private async Task<bool> SaveStudentFeeInternalAsync(StudentFee studentFee)
        {
            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            using var transaction = connection.BeginTransaction();
            try
            {
                const string insertQuery = @"
            INSERT INTO SchoolManagement.StudentFees 
            (StudentID, FeeTypeID, StudentName, FeeTypeName, ClassID, AmountPaid, AmountLeft, PaymentDate, Note, UserID, PaymentMethod, TermID)
            VALUES (@StudentID, @FeeTypeID, @StudentName, @FeeTypeName, @ClassID, @AmountPaid, @AmountLeft, @PaymentDate, @Note, @UserID, @PaymentMethod, @TermID);
            SELECT SCOPE_IDENTITY();";

                var newId = await connection.ExecuteScalarAsync<int>(insertQuery, studentFee, transaction: transaction);

                if (newId > 0)
                {
                    // Log to SyncLog for offline/remote sync
                    string logQuery = @"INSERT INTO SchoolManagement.SyncLog 
                                (TableName, RecordID, ActionType, SyncStatus, RetryCount, MaxRetry, CreatedAt) 
                                VALUES ('StudentFees', @FeeID, 'INSERT', 'Pending', 0, 5, GETDATE())";

                    await connection.ExecuteAsync(logQuery, new { FeeID = newId }, transaction);

                    transaction.Commit();

                    // Call procedure AFTER commit
                    await connection.ExecuteAsync("EXEC SchoolManagement.FixStudentFeeAmounts");

                    return true;
                }
                else
                {
                    transaction.Rollback();
                    return false;
                }
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public async Task<List<Student>> GetStudentsWithSiblingsToDiscountAsync()
        {
            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();

                var sql = @"
        SELECT s.StudentID, 
               s.StudentFirstName,
               s.StudentLastName,
               s.ClassID,
               s.GuardianFullName
        FROM SchoolManagement.Students s
        WHERE s.GuardianFullName IN (
            SELECT GuardianFullName
            FROM SchoolManagement.Students
            GROUP BY GuardianFullName
            HAVING COUNT(StudentID) > 1
        )
        AND s.GuardianFullName NOT IN (
            SELECT GuardianFullName
            FROM SchoolManagement.StudentDiscounts
            WHERE IsActive = 1
        )
        ORDER BY s.GuardianFullName, s.StudentFirstName, s.StudentLastName";

                var students = await connection.QueryAsync<Student>(sql);
                return students.ToList();
            }
        }



        public async Task<int> AddStudentDiscountAsync(StudentDiscount discount)
        {
            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            using var transaction = connection.BeginTransaction();
            try
            {
                var sql = @"
            INSERT INTO SchoolManagement.StudentDiscounts 
                (StudentID, GuardianFullName, DiscountType, DiscountValue, IsActive, CreatedDate, UserID)
            VALUES (@StudentID, @GuardianFullName, @DiscountType, @DiscountValue, 1, GETDATE(), @UserID);
            SELECT SCOPE_IDENTITY();";

                // Insert discount and get new ID
                var newId = await connection.ExecuteScalarAsync<int>(sql, discount, transaction);

                if (newId > 0)
                {
                    // Insert into SyncLog for remote sync
                    string logQuery = @"
                INSERT INTO SchoolManagement.SyncLog
                    (TableName, RecordID, ActionType, SyncStatus, RetryCount, MaxRetry, CreatedAt)
                VALUES ('StudentDiscounts', @DiscountID, 'INSERT', 'Pending', 0, 5, GETDATE())";

                    await connection.ExecuteAsync(logQuery, new { DiscountID = newId }, transaction);

                    transaction.Commit();
                    return newId;
                }
                else
                {
                    transaction.Rollback();
                    return 0;
                }
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public async Task<List<StudentOwingRecord>> GetAllStudentsWhoOweFeesAsync()
        {
            using (var connection = new SqlConnection(connectionString))
            {
                var query = @"
            SELECT 
                s.StudentID,
                (s.StudentFirstName + ' ' + s.StudentLastName) AS FullName,
                t.Term,
                ft.FeeTypeName,
                ft.Amount AS TotalFeeAmount,
                ISNULL(SUM(f.AmountPaid), 0) AS TotalPaid,
                ft.Amount - ISNULL(SUM(f.AmountPaid), 0) AS AmountOwing
            FROM SchoolManagement.Students s
            JOIN SchoolManagement.FeeTypes ft ON s.ClassID = ft.ClassID
            LEFT JOIN SchoolManagement.StudentFees f 
                ON s.StudentID = f.StudentID AND ft.FeeTypeID = f.FeeTypeID
            LEFT JOIN SchoolManagement.SchoolTerm t ON f.TermID = t.TermID
            GROUP BY 
                s.StudentID, s.StudentFirstName, s.StudentLastName,
                ft.FeeTypeName, ft.Amount, t.Term
            HAVING ft.Amount - ISNULL(SUM(f.AmountPaid), 0) > 0
            ORDER BY FullName";

                var results = await connection.QueryAsync<StudentOwingRecord>(query);
                return results.ToList();
            }
        }

        public async Task<List<FeeTypeSummary>> GetFeeTypeSummariesAsync()
        {
            using (var connection = new SqlConnection(connectionString))
            {
                var query = @"
            SELECT 
                ft.FeeTypeName,
                SUM(ft.Amount) AS TotalExpected,
                SUM(ISNULL(f.AmountPaid, 0)) AS TotalPaid,
                SUM(ft.Amount - ISNULL(f.AmountPaid, 0)) AS TotalOwing
            FROM SchoolManagement.Students s
            JOIN SchoolManagement.FeeTypes ft ON s.ClassID = ft.ClassID
            LEFT JOIN SchoolManagement.StudentFees f 
                ON s.StudentID = f.StudentID AND ft.FeeTypeID = f.FeeTypeID
            GROUP BY ft.FeeTypeName";

                var results = await connection.QueryAsync<FeeTypeSummary>(query);
                return results.ToList();
            }
        }

        public async Task<List<FeeTypeSummary>> GetFeeTypeSummaryByClassAndTerm(string classId, int termId)
        {
            var query = @"
        SELECT 
            FeeTypeName,
            SUM(AmountPaid) AS TotalPaid,
            SUM(AmountLeft) AS TotalOwing
        FROM SchoolManagement.StudentFees
        WHERE ClassID = @ClassID AND TermID = @TermID AND AmountPaid > 0
        GROUP BY FeeTypeName";

            using var connection = new SqlConnection(connectionString);
            var result = await connection.QueryAsync<FeeTypeSummary>(query, new { ClassID = classId, TermID = termId });
            return result.ToList();
        }

        public async Task<bool> CarryOverUnpaidFeesToTermAsync(int newTermId)
        {
            using var connection = new SqlConnection(connectionString);
            var parameters = new DynamicParameters();
            parameters.Add("@NewTermID", newTermId);

            await connection.ExecuteAsync("SchoolManagement.sp_CarryOverUnpaidFees", parameters, commandType: CommandType.StoredProcedure);
            return true;
        }


        /*incase in the future something goes wrong with the current GetOutstandingBalanceAsync this is the original code i am getting back to publish it and fix the issue later*/

        //public async Task<decimal> GetOutstandingBalanceAsync(int studentId, int feeTypeId, int termId)
        //{
        //    using (var connection = new SqlConnection(connectionString))
        //    {
        //        // Get the standard fee for this fee type (not student-specific)
        //        var expectedAmount = await connection.QuerySingleOrDefaultAsync<decimal>(
        //            @"SELECT ISNULL(Amount, 0)
        //      FROM SchoolManagement.FeeTypes 
        //      WHERE FeeTypeID = @FeeTypeID",
        //            new { FeeTypeID = feeTypeId });

        //        // Get how much the student has paid for this term and fee type
        //        var paidAmount = await connection.QuerySingleOrDefaultAsync<decimal>(
        //            @"SELECT ISNULL(SUM(AmountPaid), 0) 
        //      FROM SchoolManagement.StudentFees 
        //      WHERE StudentID = @StudentID AND FeeTypeID = @FeeTypeID AND TermID = @TermID",
        //            new { StudentID = studentId, FeeTypeID = feeTypeId, TermID = termId });

        //        var balance = expectedAmount - paidAmount;

        //        // Avoid negative values
        //        return balance < 0 ? 0 : balance;
        //    }
        //}


        public async Task<decimal> GetOutstandingBalanceAsync(int studentId, int feeTypeId, int termId)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                var query = @"
        SELECT 
            ISNULL(SUM(CASE WHEN PaymentMethod = 'AutoAssign' THEN ISNULL(AmountLeft, 0) ELSE 0 END), 0) AS TotalAssigned,
            ISNULL(SUM(CASE WHEN PaymentMethod != 'AutoAssign' THEN ISNULL(AmountPaid, 0) ELSE 0 END), 0) AS TotalPaid
        FROM SchoolManagement.StudentFees
        WHERE StudentID = @StudentID
          AND FeeTypeID = @FeeTypeID
          AND TermID = @TermID";

                var result = await connection.QueryFirstOrDefaultAsync<(decimal TotalAssigned, decimal TotalPaid)>(query, new
                {
                    StudentID = studentId,
                    FeeTypeID = feeTypeId,
                    TermID = termId
                });

                decimal amountExpected;

                // If there's no AutoAssign yet, fallback to FeeTypes
                if (result.TotalAssigned == 0)
                {
                    amountExpected = await connection.QuerySingleOrDefaultAsync<decimal>(
                        @"SELECT ISNULL(Amount, 0)
                  FROM SchoolManagement.FeeTypes
                  WHERE FeeTypeID = @FeeTypeID",
                        new { FeeTypeID = feeTypeId });
                }
                else
                {
                    amountExpected = result.TotalAssigned;
                }

                decimal balance = amountExpected - result.TotalPaid;

                return balance < 0 ? 0 : balance;
            }
        }


        public async Task<List<StudentFee>> GetPreviousBalancesBreakdownAsync(int studentId, int termId)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                var query = @"
            SELECT FeeTypeName, ClassID, AmountLeft, Note
            FROM SchoolManagement.StudentFees
            WHERE StudentID = @StudentID AND TermID = @TermID
            AND Note LIKE '%carried over%'";

                var result = await connection.QueryAsync<StudentFee>(query, new
                {
                    StudentID = studentId,
                    TermID = termId
                });

                return result.ToList();
            }
        }


        // 1 Balance that’s still left in THIS term only
        //public async Task<decimal> GetTermOutstandingAsync(int studentId, int termId)
        //{
        //    const string sql = @"
        //SELECT ISNULL(SUM(AmountLeft),0)
        //FROM   SchoolManagement.StudentFees
        //WHERE  StudentID = @StudentID
        //  AND  TermID    = @TermID       -- current term only
        //  AND  AmountLeft > 0";
        //    using var db = new SqlConnection(connectionString);
        //    return await db.QuerySingleAsync<decimal>(sql, new { studentId, termId });
        //}

        //// 2 List every unpaid row BEFORE this term (for the breakdown card)
        //public async Task<IEnumerable<StudentFee>> GetOutstandingBeforeTermAsync(int studentId, int termId)
        //{
        //    const string sql = @"
        //SELECT *
        //FROM   SchoolManagement.StudentFees
        //WHERE  StudentID = @StudentID
        //  AND  TermID    < @TermID       -- any earlier term
        //  AND  AmountLeft > 0
        //ORDER BY TermID DESC, FeeTypeName";
        //    using var db = new SqlConnection(connectionString);
        //    return await db.QueryAsync<StudentFee>(sql, new { studentId, termId });
        //}


        public async Task<int> GetBankIDAsync(string paymentMethod)
        {
            string cacheKey = $"BankID_{paymentMethod}";

            return await _cacheService.GetOrSetAsync(cacheKey, async () =>
            {
                using var connection = new SqlConnection(connectionString);
                string query = "SELECT BankID FROM SchoolManagement.Bank WHERE MethodName = @PaymentMethod";
                var bankId = await connection.ExecuteScalarAsync<int?>(query, new { PaymentMethod = paymentMethod });

                return bankId ?? 0; // Return 0 if not found
            }, minutes: 1); // Cache for 1 MINT. (can be more, unless banks change often)
        }

        public async Task<int> LogsBankTransactionAsync(
       int? bankId,
       string transactionType,
       decimal amount,
       string status,
       string errorMessage)
        {
            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            using var transaction = connection.BeginTransaction();
            try
            {
                const string insertQuery = @"
            INSERT INTO SchoolManagement.BankTransactionLog
                (BankID, TransactionType, Amount, Status, ErrorMessage, CreatedAt)
            VALUES
                (@BankID, @TransactionType, @Amount, @Status, @ErrorMessage, GETDATE());
            SELECT CAST(SCOPE_IDENTITY() AS INT);";

                // Insert and get ID
                var newId = await connection.ExecuteScalarAsync<int>(insertQuery, new
                {
                    BankID = bankId,
                    TransactionType = transactionType,
                    Amount = amount,
                    Status = status,
                    ErrorMessage = errorMessage
                }, transaction);

                if (newId > 0)
                {
                    // Add to SyncLog
                    const string syncQuery = @"
                INSERT INTO SchoolManagement.SyncLog
                    (TableName, RecordID, ActionType, SyncStatus, RetryCount, MaxRetry, CreatedAt)
                VALUES
                    ('BankTransactionLog', @RecordID, 'INSERT', 'Pending', 0, 5, GETDATE());";

                    await connection.ExecuteAsync(syncQuery, new { RecordID = newId }, transaction);

                    transaction.Commit();
                    return newId;
                }
                else
                {
                    transaction.Rollback();
                    return 0;
                }
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        /*logic to display student outstanding balance*/
        public async Task<StudentFee> GetStudentFeeAsync(int studentId, int feeTypeId)
        {
            var cacheKey = $"StudentFee_{studentId}_FeeType_{feeTypeId}";

            return await _cacheService.GetOrSetAsync(cacheKey, async () =>
            {
                using var connection = new SqlConnection(connectionString);
                const string query = @"
        SELECT TOP 1 AmountLeft
        FROM SchoolManagement.StudentFees
        WHERE StudentID = @StudentID AND FeeTypeID = @FeeTypeID
        ORDER BY PaymentDate DESC";

                return await connection.QueryFirstOrDefaultAsync<StudentFee>(
                    query, new { StudentID = studentId, FeeTypeID = feeTypeId });

            }, minutes: 10); // Cache for 10 minutes
        }

        public async Task<List<string>> GetPaymentMethodsAsync()
        {
            const string cacheKey = "AllPaymentMethods";

            return await _cacheService.GetOrSetAsync(cacheKey, async () =>
            {
                using var connection = new SqlConnection(connectionString);
                string query = "SELECT MethodName FROM SchoolManagement.PaymentMethods";

                var paymentMethods = await connection.QueryAsync<string>(query);
                return paymentMethods.AsList();
            }, minutes: 60); // Cache for 1 hour or more
        }

        // Method to fetch all classes
        public async Task<List<Class>> GetsAllClassesAsync()
        {
            const string cacheKey = "AllClassIDs";

            return await _cacheService.GetOrSetAsync(cacheKey, async () =>
            {
                using var connection = new SqlConnection(connectionString);
                string query = "SELECT DISTINCT ClassID FROM SchoolManagement.Class";
                var classes = await connection.QueryAsync<Class>(query);
                return classes.AsList();
            }, minutes: 60); // Cache for 1 hour or more
        }

        public async Task<List<DetailedStudentFeeSummary>> GetDetailedStudentFeeSummaryAsync(bool allTerms, int termId)
        {
            using var connection = new SqlConnection(connectionString);
            var parameters = new DynamicParameters();
            parameters.Add("@AllTerms", allTerms ? 1 : 0);
            parameters.Add("@TermID", termId);

            var results = await connection.QueryAsync<DetailedStudentFeeSummary>(
                "SchoolManagement.GetStudentFeeSummaryReport",
                parameters,
                commandType: CommandType.StoredProcedure);

            return results.ToList();
        }


        public async Task<List<SchoolTerm>> GetAllSchoolTermsAsync()
        {
            const string cacheKey = "AllSchoolTerms";

            return await _cacheService.GetOrSetAsync(cacheKey, async () =>
            {
                using var connection = new SqlConnection(connectionString);
                string query = "SELECT TermID, Term FROM SchoolManagement.SchoolTerm";
                var terms = await connection.QueryAsync<SchoolTerm>(query);
                return terms.AsList();
            }, minutes: 60); // Cache for 1 hour or more
        }

        public async Task<SchoolTerm> GetCurrentSchoolTermAsync()
        {
            using var connection = new SqlConnection(connectionString);
            string query = @"
        SELECT TOP 1 TermID, Term 
        FROM SchoolManagement.SchoolTerm 
        WHERE IsCurrentTerm = 1";

            var term = await connection.QueryFirstOrDefaultAsync<SchoolTerm>(query);
            return term;
        }


        /**/
        public async Task<IEnumerable<Student>> SearchStudentsAsync(string searchText)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                var query = @"
            SELECT TOP 10 StudentID, 
                         StudentFirstName, 
                         StudentLastName, 
                         ClassID 
            FROM SchoolManagement.Students
            WHERE CONCAT(StudentFirstName, ' ', StudentLastName) LIKE @SearchText";

                return await connection.QueryAsync<Student>(
                    query,
                    new { SearchText = $"%{searchText}%" });
            }
        }

        // Fetch all classes (ensure the correct class model is used)
        public async Task<List<Class>> GetAllClassesforsetfeesamountAsync()
        {
            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    var query = "SELECT * FROM SchoolManagement.Class";  // Ensure the correct query here for fetching classes
                    var result = await connection.QueryAsync<Class>(query);
                    return result.AsList();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetAllClassesAsync: {ex.Message}");
                throw;
            }
        }

        // Fetch all fee types for the dropdown
        public async Task<List<FeeType>> GetAllFeeTypesAsync()
        {
            const string cacheKey = "AllActiveFeeTypes";

            return await _cacheService.GetOrSetAsync(cacheKey, async () =>
            {
                using var connection = new SqlConnection(connectionString);
                var query = "SELECT * FROM SchoolManagement.FeeTypes WHERE DeletedBy IS NULL";
                var result = await connection.QueryAsync<FeeType>(query);
                return result.AsList();
            }, minutes: 60); // Cache for 1 hour or longer
        }

        // Insert the fee amount for the selected class and fee type
        public async Task<int> InsertFeeAmountAsync(string classId, int feeTypeId, decimal amount, int userId, string feeTypeName)
        {
            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            using var transaction = connection.BeginTransaction();
            try
            {
                // Insert into FeeTypes
                var query = @"
            INSERT INTO SchoolManagement.FeeTypes 
                (ClassID, FeeTypeName, Amount, UserID, RecDateCreated)
            VALUES (@ClassID, @FeeTypeName, @Amount, @UserID, GETDATE());
            SELECT CAST(SCOPE_IDENTITY() AS INT);";

                var newId = await connection.ExecuteScalarAsync<int>(query, new
                {
                    ClassID = classId,
                    FeeTypeName = feeTypeName,
                    Amount = amount,
                    UserID = userId
                }, transaction);

                if (newId > 0)
                {
                    // Log for sync
                    string logQuery = @"
                INSERT INTO SchoolManagement.SyncLog
                    (TableName, RecordID, ActionType, SyncStatus, RetryCount, MaxRetry, CreatedAt)
                VALUES ('FeeTypes', @FeeTypeID, 'INSERT', 'Pending', 0, 5, GETDATE())";

                    await connection.ExecuteAsync(logQuery, new { FeeTypeID = newId }, transaction);

                    transaction.Commit();

                    // Invalidate cache AFTER commit
                    _cacheService.Invalidate("AllActiveFeeTypes");

                    return newId;
                }
                else
                {
                    transaction.Rollback();
                    return 0;
                }
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                Console.WriteLine($"Error in InsertFeeAmountAsync: {ex.Message}");
                throw;
            }
        }

        public async Task InsertNewFeeTypeAsync(FeeType feeType)
        {
            try
            {
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                using var transaction = connection.BeginTransaction();

                var sql = @"
        INSERT INTO SchoolManagement.FeeTypes 
        (FeeTypeName, Description, Amount, ClassID, RecDateCreated, UserID)
        VALUES (@FeeTypeName, '', @Amount, @ClassID, GETDATE(), @UserID);
        SELECT CAST(SCOPE_IDENTITY() AS INT);";

                // Get new FeeTypeID
                var newId = await connection.ExecuteScalarAsync<int>(sql, feeType, transaction);

                if (newId > 0)
                {
                    // Add to SyncLog for offline sync
                    string logQuery = @"
            INSERT INTO SchoolManagement.SyncLog
            (TableName, RecordID, ActionType, SyncStatus, RetryCount, MaxRetry, CreatedAt)
            VALUES ('FeeTypes', @FeeTypeID, 'INSERT', 'Pending', 0, 5, GETDATE())";

                    await connection.ExecuteAsync(logQuery, new { FeeTypeID = newId }, transaction);

                    transaction.Commit();

                    // Invalidate cache after successful insert
                    _cacheService.Invalidate("AllActiveFeeTypes");
                }
                else
                {
                    transaction.Rollback();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error inserting new fee type: {ex.Message}");
                throw;
            }
        }



        public async Task<List<Class>> GetAllforsetfeesFeeTypesAsync()
        {
            const string cacheKey = "AllClasses";

            return await _cacheService.GetOrSetAsync(cacheKey, async () =>
            {
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();
                var query = "SELECT * FROM SchoolManagement.Class";
                var result = await connection.QueryAsync<Class>(query);
                return result.AsList();
            }, minutes: 60); // Cache for 1 hour or more
        }


        public async Task InsertforsetfeesFeeAmountAsync(string classId, int feeTypeId, decimal amount, int userId)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();
                var query = @"
            INSERT INTO SchoolManagement.FeeTypes (ClassID, FeeTypeID, Amount, UserID)
            VALUES (@ClassID, @FeeTypeID, @Amount, @UserID)";
                await connection.ExecuteAsync(query, new { ClassID = classId, FeeTypeID = feeTypeId, Amount = amount, UserID = userId });
            }
        }

        public async Task UpdateFeeAmountAsync(int feeTypeId, string classId, decimal amount, int userId)
        {
            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            using var transaction = connection.BeginTransaction();
            try
            {
                var query = @"
        UPDATE SchoolManagement.FeeTypes
        SET Amount = @Amount, EditedOnRecDateCreated = GETDATE(), EditBy = @UserID
        WHERE FeeTypeID = @FeeTypeID AND ClassID = @ClassID";

                await connection.ExecuteAsync(query, new { FeeTypeID = feeTypeId, ClassID = classId, Amount = amount, UserID = userId }, transaction);

                // Add to SyncLog for remote sync
                string logQuery = @"
        INSERT INTO SchoolManagement.SyncLog
            (TableName, RecordID, ActionType, SyncStatus, RetryCount, MaxRetry, CreatedAt)
        VALUES ('FeeTypes', @FeeTypeID, 'UPDATE', 'Pending', 0, 5, GETDATE())";

                await connection.ExecuteAsync(logQuery, new { FeeTypeID = feeTypeId }, transaction);

                transaction.Commit();

                _cacheService.Invalidate("AllActiveFeeTypes"); // Invalidate cache
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public async Task DeleteFeeAmountAsync(int feeTypeId, int userId)
        {
            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            using var transaction = connection.BeginTransaction();
            try
            {
                var query = @"
        UPDATE SchoolManagement.FeeTypes
        SET DeletedBy = @UserID, DeletedOnRecDateCreated = GETDATE()
        WHERE FeeTypeID = @FeeTypeID";

                await connection.ExecuteAsync(query, new { FeeTypeID = feeTypeId, UserID = userId }, transaction);

                // Add to SyncLog for remote sync
                string logQuery = @"
        INSERT INTO SchoolManagement.SyncLog
            (TableName, RecordID, ActionType, SyncStatus, RetryCount, MaxRetry, CreatedAt)
        VALUES ('FeeTypes', @FeeTypeID, 'DELETE', 'Pending', 0, 5, GETDATE())";

                await connection.ExecuteAsync(logQuery, new { FeeTypeID = feeTypeId }, transaction);

                transaction.Commit();

                _cacheService.Invalidate("AllActiveFeeTypes"); // Invalidate cache
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        /*AccountReconciliationService*/
        // Search for students by name
        public async Task<IEnumerable<Student>> SearchStudentAsync(string searchTerm)
        {
            string cacheKey = $"Search_Students_{searchTerm?.ToLower()}";

            return await _cacheService.GetOrSetAsync(cacheKey, async () =>
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    string query = @"
                SELECT StudentID, StudentFirstName, StudentLastName, ClassID 
                FROM SchoolManagement.Students
                WHERE CONCAT(StudentFirstName, ' ', StudentLastName) LIKE @SearchTerm";

                    return await connection.QueryAsync<Student>(query, new { SearchTerm = $"%{searchTerm}%" });
                }
            }, minutes: 5); // Cache each search term for 5 minutes
        }


        // Get student fee details
        public async Task<IEnumerable<StudentFee>> GetStudentFeesAsync(int studentId)
        {
            string cacheKey = $"StudentFees_{studentId}";

            return await _cacheService.GetOrSetAsync(cacheKey, async () =>
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    string query = @"
                SELECT FeeID, StudentName, FeeTypeName, ClassID, AmountPaid, AmountLeft, TermID, PaymentDate, DueDate, Note 
                FROM SchoolManagement.StudentFees
                WHERE StudentID = @StudentID";

                    var fees = await connection.QueryAsync<StudentFee>(query, new { StudentID = studentId });
                    return fees;
                }
            }, minutes: 10); // Cache for 10 minutes
        }

        public async Task<IEnumerable<Staff>> GetAllStaffAsync()
        {
            using (var connection = new SqlConnection(connectionString))
            {
                string query = "SELECT * FROM SchoolManagement.Staff ORDER BY StaffLastName";
                return await connection.QueryAsync<Staff>(query);
            }
        }
      
        public async Task<List<PaymentRecord>> GetPaymentHistoryAsync(int staffId)
        {
            try
            {
                using var connection = new SqlConnection(connectionString);
                string query = @"
            SELECT PayedOn, SalaryFor, PaymentYear, Salary AS Amount 
            FROM SchoolManagement.SalaryPayments 
            WHERE StaffID = @StaffID";

                return (await connection.QueryAsync<PaymentRecord>(query, new { StaffID = staffId })).ToList();
            }
            catch (Exception ex)
            {
                // Log the error (consider using a logging framework like Serilog)
                Console.WriteLine($"Error fetching payment history: {ex.Message}");
                return new List<PaymentRecord>();
            }
        }

        public async Task<bool> PaySalaryAsync(int staffId, decimal amount, DateTime payDate, int month, int year, string bankName, string accountName, string accountNumber, decimal overTime, decimal taxDeduction, string paymentMethod, int categoryID)
        {
            try
            {
                using var connection = new SqlConnection(connectionString);
                string query = @"
            INSERT INTO SchoolManagement.SalaryPayments 
            (StaffID, PayedOn, SalaryFor, PaymentYear, Amount, BankName, AccountName, AccountNumber, OverTime, TaxDeduction, PaymentMethod, CategoryID) 
            VALUES (@StaffID, @PayedOn, @SalaryFor, @PaymentYear, @Amount, @BankName, @AccountName, @AccountNumber, @OverTime, @TaxDeduction, @PaymentMethod, @CategoryID)";

                int rows = await connection.ExecuteAsync(query, new
                {
                    StaffID = staffId,
                    PayedOn = payDate,
                    SalaryFor = month,
                    PaymentYear = year,
                    Amount = amount,
                    BankName = bankName,
                    AccountName = accountName,
                    AccountNumber = accountNumber,
                    OverTime = overTime,
                    TaxDeduction = taxDeduction,
                    PaymentMethod = paymentMethod,
                    CategoryID = categoryID

                });

                return rows > 0;
            }
            catch (Exception ex)
            {
                // Log the error (consider using a logging framework like Serilog)
                Console.WriteLine($"Error processing salary payment: {ex.Message}");
                return false;
            }
        }

        public async Task<List<PaymentCategory>> GetPaymentCategoriesAsync()
        {
            using var connection = new SqlConnection(connectionString);
            string query = "SELECT * FROM SchoolManagement.PaymentCategory";
            return (await connection.QueryAsync<PaymentCategory>(query)).ToList();
        }

        public async Task<List<Staff>> GetStaffByCategoryAsync(int categoryId)
        {
            try
            {

                using var connection = new SqlConnection(connectionString);
                string query = "SELECT * FROM SchoolManagement.Staff WHERE CategoryID = @CategoryID";
                return (await connection.QueryAsync<Staff>(query, new { CategoryID = categoryId })).ToList();
            }
            catch(Exception ex)
            {
                // Log the error (consider using a logging framework like Serilog)
                Console.WriteLine($"Error fetching staff by category: {ex.Message}");
                return new List<Staff>();
            }
        }

        public async Task<bool> ProcessAutomaticPaymentAsync(int categoryId, string paymentMethod)
        {
            int? paymentMethodId = null;

            try
            {
                using var connection = new SqlConnection(connectionString);

                // Get both staff and teachers under the selected category
                string staffQuery = "SELECT StaffID FROM SchoolManagement.Staff WHERE CategoryID = @CategoryID";
                var staffList = await connection.QueryAsync<Staff>(staffQuery, new { CategoryID = categoryId });

                string teacherQuery = "SELECT TeacherID FROM SchoolManagement.Teacher WHERE CategoryID = @CategoryID";
                var teacherList = await connection.QueryAsync<TeachersRegistration>(teacherQuery, new { CategoryID = categoryId });

                // Get payment amount for this category
                string salaryQuery = "SELECT Amount FROM SchoolManagement.PaymentCategory WHERE CategoryID = @CategoryID";
                decimal salaryAmount = await connection.ExecuteScalarAsync<decimal>(salaryQuery, new { CategoryID = categoryId });

                // Calculate total amount to deduct
                decimal totalAmountToDeduct = salaryAmount * (staffList.Count() + teacherList.Count());

                // Get PaymentMethodID
                string paymentMethodIdQuery = "SELECT PaymentMethodID FROM SchoolManagement.PaymentMethods WHERE MethodName = @MethodName";
                paymentMethodId = await connection.ExecuteScalarAsync<int>(paymentMethodIdQuery, new { MethodName = paymentMethod });

                // Get BankID associated with the PaymentMethod
                string bankIdQuery = "SELECT BankID FROM SchoolManagement.Bank WHERE PaymentMethodID = @PaymentMethodId";
                int bankId = await connection.ExecuteScalarAsync<int>(bankIdQuery, new { PaymentMethodId = paymentMethodId });

                // Check available balance in the bank
                string availableBalanceQuery = "SELECT SUM(AmountTransferred) FROM SchoolManagement.Bank WHERE PaymentMethodID = @PaymentMethodId";
                decimal availableBalance = await connection.ExecuteScalarAsync<decimal>(availableBalanceQuery, new { PaymentMethodId = paymentMethodId });

                if (availableBalance < totalAmountToDeduct)
                {
                    await LogPaymentAsync(null, null, "Automatic", totalAmountToDeduct, "Failed", "Insufficient funds in the selected bank account.", paymentMethodId.Value);
                    return false; // Not enough funds
                }

                // Insert payments for both staff and teachers
                string insertQuery = @"
INSERT INTO SchoolManagement.SalaryPayments (StaffID, TeacherID, CategoryID, PayedOn, SalaryFor, PaymentYear, Amount, PaymentMethod, PaymentMethodID)
OUTPUT INSERTED.PaymentID
VALUES (@StaffID, @TeacherID, @CategoryID, GETDATE(), @SalaryFor, @PaymentYear, @Amount, @PaymentMethod, @PaymentMethodId)";

                // Process payments for Staff
                foreach (var staff in staffList)
                {
                    var paymentId = await connection.ExecuteScalarAsync<int>(insertQuery, new
                    {
                        StaffID = staff.StaffID,
                        TeacherID = (int?)null,
                        CategoryID = categoryId,
                        SalaryFor = DateTime.Now.Month,
                        PaymentYear = DateTime.Now.Year,
                        Amount = salaryAmount,
                        PaymentMethodId = paymentMethodId.Value,
                        PaymentMethod = paymentMethod
                    });

                    await LogPaymentAsync(paymentId, bankId, "Automatic", salaryAmount, "Success", null, paymentMethodId.Value);
                }

                // Process payments for Teachers
                foreach (var teacher in teacherList)
                {
                    var paymentId = await connection.ExecuteScalarAsync<int>(insertQuery, new
                    {
                        StaffID = (int?)null,
                        TeacherID = teacher.TeacherID,
                        CategoryID = categoryId,
                        SalaryFor = DateTime.Now.Month,
                        PaymentYear = DateTime.Now.Year,
                        Amount = salaryAmount,
                        PaymentMethodId = paymentMethodId.Value,
                        PaymentMethod = paymentMethod
                    });

                    await LogPaymentAsync(paymentId, bankId, "Automatic", salaryAmount, "Success", null, paymentMethodId.Value);
                }

                // Deduct total amount from the bank
                string deductQuery = @"
UPDATE SchoolManagement.Bank
SET AmountTransferred = AmountTransferred - @TotalAmount
WHERE PaymentMethodID = @PaymentMethodId";

                await connection.ExecuteAsync(deductQuery, new { TotalAmount = totalAmountToDeduct, PaymentMethodId = paymentMethodId.Value });

                // **Log withdrawal in BankTransactionLog**
                string logWithdrawalQuery = @"
INSERT INTO SchoolManagement.BankTransactionLog (BankID, TransactionType, Amount, TransactionDate, Status, ErrorMessage)
VALUES (@BankID, 'Withdrawal', @TotalAmount, GETDATE(), 'Success', NULL)";

                await connection.ExecuteAsync(logWithdrawalQuery, new { BankID = bankId, TotalAmount = totalAmountToDeduct });


                return true;
            }
            catch (Exception ex)
            {
                await LogPaymentAsync(null, null, "Automatic", 0, "Failed", ex.Message, paymentMethodId ?? 0);
                return false;
            }
        }

        public async Task<List<SalaryPaymentHistory>> GetSalaryPaymentHistoryAsync(int? staffID = null, string staffName = null)
        {
            try
            {
                using var connection = new SqlConnection(connectionString);
                string query = @"
            SELECT PaymentID, StaffID, Amount, SalaryFor, PaymentYear, PayedOn, PaymentMethod, BankName, 
                   AccountName, AccountNumber, OverTime, TaxDeduction
            FROM SchoolManagement.SalaryPayments";

                // Add filters based on the provided parameters
                if (staffID.HasValue && !string.IsNullOrEmpty(staffName))
                {
                    query += " WHERE StaffID = @StaffID AND StaffName LIKE @StaffName";
                }
                else if (staffID.HasValue)
                {
                    query += " WHERE StaffID = @StaffID";
                }
                else if (!string.IsNullOrEmpty(staffName))
                {
                    query += " WHERE StaffName LIKE @StaffName";
                }

                var result = await connection.QueryAsync<SalaryPaymentHistory>(query, new { StaffID = staffID, StaffName = $"%{staffName}%" });
                return result.ToList();
            }
            catch (Exception ex)
            {
                // Log the error
                Console.WriteLine($"Error fetching salary payment history: {ex.Message}");
                return new List<SalaryPaymentHistory>();
            }
        }


        private async Task LogPaymentAsync(int? paymentId, int? bankId, string paymentType, decimal amount, string status, string errorMessage, int paymentMethodId)
        {
            using var connection = new SqlConnection(connectionString);

            // Check if the PaymentMethodID exists
            string checkQuery = "SELECT COUNT(1) FROM SchoolManagement.PaymentMethods WHERE PaymentMethodID = @PaymentMethodId";
            int count = await connection.ExecuteScalarAsync<int>(checkQuery, new { PaymentMethodId = paymentMethodId });

            if (count == 0)
            {
                // Handle the case where the PaymentMethodID does not exist
                Console.WriteLine($"PaymentMethodID {paymentMethodId} does not exist. Cannot log payment.");
                return; // Or throw an exception, or handle it as needed
            }

            string logQuery = @"
    INSERT INTO SchoolManagement.PaymentLog (PaymentID, BankID, PaymentType, Amount, Status, ErrorMessage, PaymentMethodID)
    VALUES (@PaymentID, @BankID, @PaymentType, @Amount, @Status, @ErrorMessage, @PaymentMethodID)";

            await connection.ExecuteAsync(logQuery, new
            {
                PaymentID = paymentId,
                BankID = bankId,
                PaymentType = paymentType,
                Amount = amount,
                Status = status,
                ErrorMessage = errorMessage,
                PaymentMethodID = paymentMethodId // Pass the correct PaymentMethodID here
            });
        }
        public async Task<bool> ProcessManualPaymentAsync(int staffId, decimal amount, DateTime payDate, int month, int year, int paymentMethodId)
        {
            try
            {
                using var connection = new SqlConnection(connectionString);
                string query = @"
INSERT INTO SchoolManagement.SalaryPayments (StaffID, PayedOn, SalaryFor, PaymentYear, Amount, PaymentMethodID)
OUTPUT INSERTED.PaymentID
VALUES (@StaffID, @PayedOn, @SalaryFor, @PaymentYear, @Amount, @PaymentMethodId)";

                // Capture the PaymentID
                int paymentId = await connection.ExecuteScalarAsync<int>(query, new
                {
                    StaffID = staffId,
                    PayedOn = payDate,
                    SalaryFor = month,
                    PaymentYear = year,
                    Amount = amount,
                    PaymentMethodId = paymentMethodId // Ensure this is an int
                });

                // Retrieve the BankID
                string bankQuery = "SELECT BankID FROM SchoolManagement.Bank WHERE PaymentMethodID = @PaymentMethodId";
                int bankId = await connection.ExecuteScalarAsync<int>(bankQuery, new { PaymentMethodId = paymentMethodId });

                // Log the payment
                await LogPaymentAsync(paymentId, bankId, "Manual", amount, "Success", null, paymentMethodId);

                return true;
            }
            catch (Exception ex)
            {
                // Log detailed error message
                await LogPaymentAsync(null, null, "Manual", amount, "Failed", $"Error processing manual payment: {ex.Message}", 0);
                return false;
            }
        }

        public async Task<(bool Success, string ErrorMessage)> InsertSalaryPaymentAsync(object paymentData)
        {
            try
            {
                var query = @"
INSERT INTO SchoolManagement.SalaryPayments 
(StaffID, CategoryID, SalaryFor, PaymentYear, Amount, OverTime, TaxDeduction, BankName, AccountName, AccountNumber, PaymentMethodId) 
OUTPUT INSERTED.PaymentID
VALUES 
(@StaffID, @CategoryID, @SalaryFor, @PaymentYear, @Amount, @OverTime, @TaxDeduction, @BankName, @AccountName, @AccountNumber, @PaymentMethodId)";

                using (var connection = new SqlConnection(connectionString))
                {
                    // Capture the PaymentID of the newly inserted salary payment
                    int paymentId = await connection.ExecuteScalarAsync<int>(query, paymentData);

                    // Retrieve the BankID based on the PaymentMethodId
                    string bankQuery = "SELECT BankID FROM SchoolManagement.Bank WHERE PaymentMethodID = @PaymentMethodId";
                    int paymentMethodId = (int)paymentData.GetType().GetProperty("PaymentMethodId").GetValue(paymentData);
                    int bankId = await connection.ExecuteScalarAsync<int>(bankQuery, new { PaymentMethodId = paymentMethodId });

                    // Log the successful insertion
                    if (paymentId > 0)
                    {
                        await LogPaymentAsync(paymentId, bankId, "Manual",
                            (decimal)paymentData.GetType().GetProperty("Amount").GetValue(paymentData), "Success", null, paymentMethodId);
                        return (true, null);
                    }
                    else
                    {
                        string errorMessage = "No rows affected while inserting salary payment.";
                        await LogPaymentAsync(null, null, "Manual",
                            (decimal)paymentData.GetType().GetProperty("Amount").GetValue(paymentData), "Failed", errorMessage, paymentMethodId);
                        return (false, errorMessage);
                    }
                }
            }
            catch (Exception ex)
            {
                string errorMessage = $"Error inserting salary payment: {ex.Message}";
                await LogPaymentAsync(null, null, "Manual",
                    (decimal)paymentData.GetType().GetProperty("Amount").GetValue(paymentData), "Failed", errorMessage, 0);
                return (false, errorMessage);
            }
        }

        public async Task<int> GetPaymentMethodIdAsync(string paymentMethodName)
        {
            using var connection = new SqlConnection(connectionString);
            string query = "SELECT PaymentMethodID FROM SchoolManagement.PaymentMethods WHERE MethodName = @MethodName";

            // Ensure you are passing the parameter correctly
            return await connection.ExecuteScalarAsync<int>(query, new { MethodName = paymentMethodName });
        }
        public async Task<decimal> GetAvailableBalanceAsync(string paymentMethod)
        {
            try
            {
                using var connection = new SqlConnection(connectionString);
                string query = "SELECT SUM(AmountTransferred) FROM SchoolManagement.Bank WHERE MethodName = @PaymentMethod";
                return await connection.ExecuteScalarAsync<decimal>(query, new { PaymentMethod = paymentMethod });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching available balance: {ex.Message}");
                return 0;
            }
        }

        public async Task<bool> DeductFromBankAsync(string paymentMethod, decimal amount)
        {
            try
            {
                using var connection = new SqlConnection(connectionString);

                // Retrieve the BankID based on the payment method
                string bankQuery = "SELECT BankID FROM SchoolManagement.Bank WHERE MethodName = @PaymentMethod";
                int bankId = await connection.ExecuteScalarAsync<int>(bankQuery, new { PaymentMethod = paymentMethod });

                // Check if BankID is valid
                if (bankId == 0)
                {
                    throw new Exception("Invalid payment method selected. No corresponding BankID found.");
                }

                string query = @"
        UPDATE SchoolManagement.Bank
        SET AmountTransferred = AmountTransferred - @Amount
        WHERE MethodName = @PaymentMethod";

                int rowsAffected = await connection.ExecuteAsync(query, new { Amount = amount, PaymentMethod = paymentMethod });

                if (rowsAffected > 0)
                {
                    // Log the successful deduction
                    await LogBankTransactionAsync(bankId, "Withdrawal", amount, "Success", null);
                    return true;
                }
                else
                {
                    // Log the failure
                    await LogBankTransactionAsync(bankId, "Withdrawal", amount, "Failed", "No rows affected.");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deducting amount from bank: {ex.Message}");
                await LogBankTransactionAsync(null, "Withdrawal", amount, "Failed", ex.Message);
                return false;
            }
        }
        private async Task LogBankTransactionAsync(int? bankId, string transactionType, decimal amount, string status, string errorMessage)
        {
            using var connection = new SqlConnection(connectionString);
            string logQuery = @"
    INSERT INTO SchoolManagement.BankTransactionLog (BankID, TransactionType, Amount, Status, ErrorMessage)
    VALUES (@BankID, @TransactionType, @Amount, @Status, @ErrorMessage)";

            await connection.ExecuteAsync(logQuery, new
            {
                BankID = bankId,
                TransactionType = transactionType,
                Amount = amount,
                Status = status,
                ErrorMessage = errorMessage
            });
        }

        /*EXPENSES*/
        // Get Expense Categories for the logged-in user
        public async Task<List<ExpenseCategory>> GetExpenseCategoriesAsync(int userId)
        {
            var categories = new List<ExpenseCategory>();

            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();
                var command = new SqlCommand("SELECT * FROM SchoolManagement.ExpenseCategories WHERE UserID = @UserID", connection);
                command.Parameters.AddWithValue("@UserID", userId);

                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        categories.Add(new ExpenseCategory
                        {
                            CategoryID = reader.GetInt32(0),
                            CategoryName = reader.GetString(1),
                            Description = reader.IsDBNull(2) ? null : reader.GetString(2),
                            UserID = reader.GetInt32(3)
                        });
                    }
                }
            }

            return categories;
        }

        // Add a new Expense Category for the logged-in user
        public async Task AddExpenseCategoryAsync(ExpenseCategory category)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();
                var command = new SqlCommand("INSERT INTO SchoolManagement.ExpenseCategories (CategoryName, Description, UserID) VALUES (@CategoryName, @Description, @UserID)", connection);
                command.Parameters.AddWithValue("@CategoryName", category.CategoryName);
                command.Parameters.AddWithValue("@Description", (object)category.Description ?? DBNull.Value);
                command.Parameters.AddWithValue("@UserID", category.UserID);

                await command.ExecuteNonQueryAsync();
            }
        }

        // Get Expenses for the logged-in user
        public async Task<List<Expense>> GetExpensesAsync(int userId)
        {
            var expenses = new List<Expense>();

            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();
                var command = new SqlCommand("SELECT * FROM SchoolManagement.Expenses WHERE UserID = @UserID", connection);
                command.Parameters.AddWithValue("@UserID", userId);

                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        expenses.Add(new Expense
                        {
                            ExpenseID = reader.GetInt32(0),
                            UserID = reader.GetInt32(1),
                            CategoryID = reader.GetInt32(2),
                            Amount = reader.GetDecimal(3),
                            ExpenseDate = reader.GetDateTime(4),
                            Description = reader.IsDBNull(5) ? null : reader.GetString(5)
                        });
                    }
                }
            }

            return expenses;
        }

        // Add a new Expense for the logged-in user
        public async Task AddExpenseAsync(Expense expense)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();
                var command = new SqlCommand("INSERT INTO SchoolManagement.Expenses (UserID, CategoryID, Amount, ExpenseDate, Description) VALUES (@UserID, @CategoryID, @Amount, @ExpenseDate, @Description)", connection);
                command.Parameters.AddWithValue("@UserID", expense.UserID);
                command.Parameters.AddWithValue("@CategoryID", expense.CategoryID);
                command.Parameters.AddWithValue("@Amount", expense.Amount);
                command.Parameters.AddWithValue("@ExpenseDate", expense.ExpenseDate);
                command.Parameters.AddWithValue("@Description", (object)expense.Description ?? DBNull.Value);

                await command.ExecuteNonQueryAsync();
            }
        }

        // Get Payment Methods for the logged-in user
        public async Task<List<PaymentMethods>> GetPaymentMethodsAsync(int userId)
        {
            var methods = new List<PaymentMethods>();

            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();
                var command = new SqlCommand("SELECT * FROM SchoolManagement.PaymentMethods WHERE UserID = @UserID", connection);
                command.Parameters.AddWithValue("@UserID", userId);

                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        methods.Add(new PaymentMethods
                        {
                            PaymentMethodID = reader.GetInt32(0),
                            MethodName = reader.GetString(1),
                            Description = reader.IsDBNull(2) ? null : reader.GetString(2),
                            UserID = reader.GetInt32(3)
                        });
                    }
                }
            }

            return methods;
        }

        // Add a new Payment Method for the logged-in user
        public async Task AddPaymentMethodAsync(PaymentMethods method)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();
                var command = new SqlCommand("INSERT INTO SchoolManagement.PaymentMethods (MethodName, Description, UserID) VALUES (@MethodName, @Description, @UserID)", connection);
                command.Parameters.AddWithValue("@MethodName", method.MethodName);
                command.Parameters.AddWithValue("@Description", (object)method.Description ?? DBNull.Value);
                command.Parameters.AddWithValue("@UserID", method.UserID);

                await command.ExecuteNonQueryAsync();
            }
        }

        // Add a new Expense Payment for the logged-in user
        public async Task AddExpensePaymentAsync(ExpensePayment payment)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();
                var command = new SqlCommand("INSERT INTO SchoolManagement.ExpensePayments (ExpenseID, PaymentMethodID, PaymentDate, AmountPaid, UserID) VALUES (@ExpenseID, @PaymentMethodID, @PaymentDate, @AmountPaid, @UserID)", connection);
                command.Parameters.AddWithValue("@ExpenseID", payment.ExpenseID);
                command.Parameters.AddWithValue("@PaymentMethodID", payment.PaymentMethodID);
                command.Parameters.AddWithValue("@PaymentDate", payment.PaymentDate);
                command.Parameters.AddWithValue("@AmountPaid", payment.AmountPaid);
                command.Parameters.AddWithValue("@UserID", payment.UserID);

                Console.WriteLine($"Executing SQL: {command.CommandText}"); // Debugging
                Console.WriteLine($"Parameters: ExpenseID={payment.ExpenseID}, PaymentMethodID={payment.PaymentMethodID}, PaymentDate={payment.PaymentDate}, AmountPaid={payment.AmountPaid}, UserID={payment.UserID}"); // Debugging

                await command.ExecuteNonQueryAsync();
            }
        }

        // Get all categories for the logged-in user
        public async Task<List<ExpenseCategory>> GetExpensesCategoriesAsync(int userId)
        {
            var categories = new List<ExpenseCategory>();

            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();
                var command = new SqlCommand("SELECT * FROM SchoolManagement.ExpenseCategories WHERE UserID = @UserID", connection);
                command.Parameters.AddWithValue("@UserID", userId);

                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        categories.Add(new ExpenseCategory
                        {
                            CategoryID = reader.GetInt32(0),
                            CategoryName = reader.GetString(1),
                            Description = reader.IsDBNull(2) ? null : reader.GetString(2),
                            UserID = reader.GetInt32(3)
                        });
                    }
                }
            }

            return categories;
        }

        // Add a new category
        public async Task AddExpensesCategoryAsync(ExpenseCategory category)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();
                var command = new SqlCommand("INSERT INTO SchoolManagement.ExpenseCategories (CategoryName, Description, UserID) VALUES (@CategoryName, @Description, @UserID)", connection);
                command.Parameters.AddWithValue("@CategoryName", category.CategoryName);
                command.Parameters.AddWithValue("@Description", (object)category.Description ?? DBNull.Value);
                command.Parameters.AddWithValue("@UserID", category.UserID);

                await command.ExecuteNonQueryAsync();
            }
        }

        // Update an existing category
        public async Task UpdateExpenseCategoryAsync(ExpenseCategory category)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();
                var command = new SqlCommand("UPDATE SchoolManagement.ExpenseCategories SET CategoryName = @CategoryName, Description = @Description WHERE CategoryID = @CategoryID", connection);
                command.Parameters.AddWithValue("@CategoryName", category.CategoryName);
                command.Parameters.AddWithValue("@Description", (object)category.Description ?? DBNull.Value);
                command.Parameters.AddWithValue("@CategoryID", category.CategoryID);

                await command.ExecuteNonQueryAsync();
            }
        }

        // Delete a category
        public async Task DeleteExpenseCategoryAsync(int categoryId)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();
                var command = new SqlCommand("DELETE FROM SchoolManagement.ExpenseCategories WHERE CategoryID = @CategoryID", connection);
                command.Parameters.AddWithValue("@CategoryID", categoryId);

                await command.ExecuteNonQueryAsync();
            }
        }

        public async Task<List<PaymentMethods>> GetsPaymentMethodsAsync(int userId)
        {
            var methods = new List<PaymentMethods>();

            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();
                var command = new SqlCommand("SELECT * FROM SchoolManagement.PaymentMethods WHERE UserID = @UserID", connection);
                command.Parameters.AddWithValue("@UserID", userId);

                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        methods.Add(new PaymentMethods
                        {
                            PaymentMethodID = reader.GetInt32(0),
                            MethodName = reader.GetString(1),
                            Description = reader.IsDBNull(2) ? null : reader.GetString(2),
                            UserID = reader.GetInt32(3),
                            BankNumber = reader.GetString(4)
                        });
                    }
                }
            }

            return methods;
        }

        // Add a new payment method
        public async Task AddsPaymentMethodAsync(PaymentMethods method)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();
                var command = new SqlCommand("INSERT INTO SchoolManagement.PaymentMethods (MethodName, Description, UserID, BankNumber) VALUES (@MethodName, @Description, @UserID, @BankNumber)", connection);
                command.Parameters.AddWithValue("@MethodName", method.MethodName);
                command.Parameters.AddWithValue("@Description", (object)method.Description ?? DBNull.Value);
                command.Parameters.AddWithValue("@UserID", method.UserID);
                command.Parameters.AddWithValue("@BankNumber", (object)method.BankNumber ?? DBNull.Value);

                await command.ExecuteNonQueryAsync();
            }
        }

        // Update an existing payment method
        public async Task UpdatePaymentMethodAsync(PaymentMethods method)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();
                var command = new SqlCommand("UPDATE SchoolManagement.PaymentMethods SET MethodName = @MethodName, Description = @Description, BankNumber = @BankNumber WHERE PaymentMethodID = @PaymentMethodID", connection);
                command.Parameters.AddWithValue("@MethodName", method.MethodName);
                command.Parameters.AddWithValue("@Description", (object)method.Description ?? DBNull.Value);
                command.Parameters.AddWithValue("@PaymentMethodID", method.PaymentMethodID);
                command.Parameters.AddWithValue("@BankNumber", method.BankNumber);

                await command.ExecuteNonQueryAsync();
            }
        }

        // Delete a payment method
        public async Task DeletePaymentMethodAsync(int paymentMethodId)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();
                var command = new SqlCommand("DELETE FROM SchoolManagement.PaymentMethods WHERE PaymentMethodID = @PaymentMethodID", connection);
                command.Parameters.AddWithValue("@PaymentMethodID", paymentMethodId);

                await command.ExecuteNonQueryAsync();
            }
        }

        //Bank
        // Method to transfer student fees to bank
        public async Task<bool> TransferStudentFeesToBank()
        {
            using (var connection = new SqlConnection(connectionString))
            {
                try
                {
                    await connection.OpenAsync();

                    // Execute the stored procedure and capture the amount transferred
                    var result = await connection.QuerySingleAsync<decimal>("EXEC SchoolManagement.TransferStudentFeesToBank", commandType: CommandType.StoredProcedure);

                    // Log the successful transfer
                    await LogBankTransactionAsync(null, "Deposit", result, "Success", null);
                    return true; // Success
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error in TransferStudentFeesToBank: " + ex.Message);
                    await LogBankTransactionAsync(null, "Deposit", 0, "Failed", ex.Message); // Log with 0 amount on failure
                    return false; // Failure
                }
            }
        }

        public async Task<IEnumerable<BankTransaction>> GetBankTransactions()
        {
            using (var connection = new SqlConnection(connectionString))
            {
                string query = @"
        SELECT * FROM SchoolManagement.BankTransactionLog
        ORDER BY TransactionDate DESC";

                return await connection.QueryAsync<BankTransaction>(query);
            }
        }

        // Method to fetch all bank transfers
        public async Task<IEnumerable<BankTransfer>> GetBankTransfers()
        {
            using (var connection = new SqlConnection(connectionString))
            {
                string query = @"
            SELECT 
                MIN(BankID) AS BankID,  -- Take the first BankID
                PaymentMethodID, 
                BankNumber, 
                MethodName, 
                SUM(AmountTransferred) AS AmountTransferred,  -- Sum up amounts
                MAX(TransferDate) AS TransferDate,  -- Keep the latest transfer date
                MIN(SystemTransferID) AS SystemTransferID  -- Use the first SystemTransferID
            FROM SchoolManagement.Bank
            GROUP BY PaymentMethodID, BankNumber, MethodName";

                return await connection.QueryAsync<BankTransfer>(query);
            }
        }

        private void StartAutoTransfer()
        {
            _timer = new Timer(async (state) =>
            {
                await TransferPaymentsToBankAsync();
            }, null, TimeSpan.Zero, TimeSpan.FromSeconds(2));
        }

        private async Task TransferPaymentsToBankAsync()
        {
            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.ExecuteAsync("EXEC SchoolManagement.TransferStudentFeesToBank");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during transfer: {ex.Message}");
            }
        }

        /*for generating pdf exams export per student*/
        public async Task<IEnumerable<Exam>> GetStudentExamsByStudentNameAsync(string studentName)
        {
            using var connection = new SqlConnection(connectionString);
            var result = await connection.QueryAsync<Exam>(
                "GetStudentExams",
                new { StudentName = studentName },
                commandType: CommandType.StoredProcedure);

            return result.ToList();
        }

        /*exams report per class*/
        public async Task<List<Exam>> GetStudentExamDataByClassAsync(string className)
        {
            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    var result = await connection.QueryAsync<Exam>(
                        "GetStudentExamDataByClass",
                        new { ClassName = className },
                        commandType: CommandType.StoredProcedure
                    );

                    return result.ToList();
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error retrieving student exam data by class.", ex);
            }
        }

        public async Task<IEnumerable<string>> GetExamsAllClassesAsync()
        {
            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();
                string query = "SELECT ClassID FROM SchoolManagement.Class ORDER BY ClassID";
                return await connection.QueryAsync<string>(query);
            }
        }

        /*Register Staff*/
        // Method to get departments for dropdown
        public async Task<List<Department>> GetDepartmentsAsync()
        {
            using (var connection = new SqlConnection(connectionString))
            {
                string query = "SELECT DepartmentID, DepartmentName FROM SchoolManagement.SchoolDepartMent";
                return (await connection.QueryAsync<Department>(query)).ToList();
            }
        }

        // Method to register a new staff
        public async Task<bool> RegisterStaff(Staff staff)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                string query = @"
            INSERT INTO SchoolManagement.Staff 
            (StaffFirstName, StaffLastName, StaffDateOfBirth, StaffGender, StaffAddress, StaffPhoneNumber, 
             StaffEmail, Position, Department, EmploymentStatus, BankName, AccountNumber, AccountName, 
             EmergencyContactName, EmergencyContactRelationship, EmergencyContactPhone, 
             DateHired, SSNIT, BasicSalary, PAYE, SSNITTIER2, CategoryName, ImageData, VotersID, HealthInsurance, GhanaCard, Remarks)
            VALUES 
            (@StaffFirstName, @StaffLastName, @StaffDateOfBirth, @StaffGender, @StaffAddress, @StaffPhoneNumber, 
             @StaffEmail, @Position, @Department, @EmploymentStatus, @BankName, @AccountNumber, @AccountName, 
             @EmergencyContactName, @EmergencyContactRelationship, @EmergencyContactPhone, 
             @DateHired, @SSNIT, @BasicSalary, @PAYE, @SSNITTIER2, @CategoryName, @ImageData, @VotersID, @HealthInsurance, @GhanaCard, @Remarks)";

                int rowsAffected = await connection.ExecuteAsync(query, staff);
                return rowsAffected > 0;
            }
        }

        //StudentID Card 
        public async Task<IEnumerable<Student>> GetStudentsAsync()
        {
            using (var connection = new SqlConnection(connectionString))
            {
                string query = @"
            SELECT 
                StudentID, 
                StudentFirstName, 
                StudentLastName, 
                ClassID, 
                ImageData 
            FROM SchoolManagement.Students";

                var students = await connection.QueryAsync<Student>(query);

                foreach (var student in students)
                {
                    if (student.ImageData != null)
                    {
                        student.ImageBase64 = $"data:image/jpeg;base64,{Convert.ToBase64String(student.ImageData)}";
                    }
                }

                return students;
            }
        }


        public async Task<Student> GetStudentByIdAsync(int studentId)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                string procedure = "GetStudentIDCardDetails";
                var parameters = new { StudentID = studentId };

                Console.WriteLine($"Executing Stored Procedure: {procedure} with StudentID: {studentId}");

                var student = await connection.QueryFirstOrDefaultAsync<Student>(
                    procedure,
                    parameters,
                    commandType: CommandType.StoredProcedure
                );

                if (student == null)
                {
                    Console.WriteLine($"Error: No student found for ID {studentId}");
                    return null;
                }

                Console.WriteLine("Fetched Student Details:");
                Console.WriteLine($"StudentID: {student.StudentID}");
                Console.WriteLine($"Name: {student.StudentFirstName} {student.StudentLastName}");
                Console.WriteLine($"ClassID: {student.ClassID}");
                Console.WriteLine($"ImageData Status: {(student.ImageData != null ? "FOUND IMAGE" : "EMPTY")}");
                Console.WriteLine($"CompanyImage Status: {(student.CompanyImage != null ? "FOUND SCHOOL IMAGE" : "EMPTY")}");

                // Convert image bytes to base64
                if (student.ImageData != null)
                {
                    student.ImageBase64 = $"data:image/jpeg;base64,{Convert.ToBase64String(student.ImageData)}";
                }

                if (student.CompanyImage != null)
                {
                    student.SchoolImageBase64 = $"data:image/jpeg;base64,{Convert.ToBase64String(student.CompanyImage)}";
                }

                return student;
            }
        }

        /*To view Movent Of Money*/
        // Get all bank transactions
        public async Task<IEnumerable<TransactionLog>> GetTransactionLogsAsync()
        {
            using (var connection = new SqlConnection(connectionString))
            {
                string query = "SELECT * FROM SchoolManagement.TransactionLogs";
                return await connection.QueryAsync<TransactionLog>(query);
            }
        }

        public async Task<IEnumerable<BankTransactionLog>> GetBankTransactionLogsAsync()
        {
            using (var connection = new SqlConnection(connectionString))
            {
                string query = "SELECT * FROM SchoolManagement.BankTransactionLog";
                return await connection.QueryAsync<BankTransactionLog>(query);
            }
        }

        public async Task<IEnumerable<PaymentLog>> GetPaymentLogsAsync()
        {
            using (var connection = new SqlConnection(connectionString))
            {
                string query = "SELECT * FROM SchoolManagement.PaymentLog";
                return await connection.QueryAsync<PaymentLog>(query);
            }
        }

        /*Set Exams*/
        public async Task<List<SetMainExamsModel>> GetAllSetMainExams()
        {
            using var connection = new SqlConnection(connectionString);
            string query = "SELECT * FROM SchoolManagement.SetMAinExams";
            var result = await connection.QueryAsync<SetMainExamsModel>(query);
            return result.ToList();
        }

        public async Task DeleteSetMainExam(int setExamId)
        {
            using var connection = new SqlConnection(connectionString);
            string query = "DELETE FROM SchoolManagement.SetMAinExams WHERE SetExamsID = @SetExamsID";
            await connection.ExecuteAsync(query, new { SetExamsID = setExamId });
        }

        public async Task AddSetMainExam(SetMainExamsModel model)
        {
            using var connection = new SqlConnection(connectionString);
            string query = @"
        INSERT INTO SchoolManagement.SetMAinExams
        (Class, SchoolCourse, ExamsTitle, QuestionType, ExamsMainDescription, ExamsSubDescription, ExamsTimeLimit)
        VALUES (@Class, @SchoolCourse, @ExamsTitle, @QuestionType, @ExamsMainDescription, @ExamsSubDescription, @ExamsTimeLimit)";

            await connection.ExecuteAsync(query, model);
        }

        public async Task InsertExamQuestion(ExamsExamsContentModel model)
        {
            using var connection = new SqlConnection(connectionString);
            string query = @"
        INSERT INTO SchoolManagement.ExamsContent
        (SetExamsID, Question, SubQuestion, ChoiceA, ChoiceB, ChoiceC, ChoiceD,
         QuestionImageData, CorrectAnswer, Remarks, QuestionSwitchButton)
        VALUES
        (@SetExamsID, @Question, @SubQuestion, @ChoiceA, @ChoiceB, @ChoiceC, @ChoiceD,
         @QuestionImageData, @CorrectAnswer, @Remarks, @QuestionSwitchButton)";

            await connection.ExecuteAsync(query, model);
        }

        /*Leave of absence*/
        public async Task<bool> InsertLeaveAsync(LeaveOfAbsence leave)
        {
            try
            {
                using var conn = new SqlConnection(connectionString);
                var sql = @"
            INSERT INTO SchoolManagement.LeaveOfAbsence
            (UserID, TeacherID, StaffID, LeaveType, StartDate, EndDate, Reason, Status, ApprovedBy, UpdatedBY)
            VALUES (@UserID, @TeacherID, @StaffID, @LeaveType, @StartDate, @EndDate, @Reason, @Status, @ApprovedBy, @UpdatedBY)";
                var rowsAffected = await conn.ExecuteAsync(sql, leave);
                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return false;
            }
        }


        public async Task<IEnumerable<TeachersRegistration>> GetTeacherAsync()
        {
            using var conn = new SqlConnection(connectionString);
            return await conn.QueryAsync<TeachersRegistration>("SELECT TeacherID, TeacherFirstName, TeacherLastName FROM SchoolManagement.Teacher");
        }

        public async Task<IEnumerable<Staff>> GetStaffAsync()
        {
            using var conn = new SqlConnection(connectionString);
            return await conn.QueryAsync<Staff>("SELECT * FROM SchoolManagement.Staff WHERE IsEnabled = 1");
        }

        public async Task<TeachersRegistration> GetTeacherByIdAsync(int id)
        {
            using var conn = new SqlConnection(connectionString);
            return await conn.QueryFirstOrDefaultAsync<TeachersRegistration>(
                "SELECT * FROM SchoolManagement.Teacher WHERE TeacherID = @id", new { id });
        }

        public async Task<Staff> GetStaffByIdAsync(int id)
        {
            using var conn = new SqlConnection(connectionString);
            return await conn.QueryFirstOrDefaultAsync<Staff>(
                "SELECT * FROM SchoolManagement.Staff WHERE StaffID = @id", new { id });
        }

        public async Task<string> GetActiveLeaveTypeForTeacherAsync(int teacherId)
        {
            using var conn = new SqlConnection(connectionString);
            return await conn.QueryFirstOrDefaultAsync<string>(
                @"SELECT TOP 1 LeaveType 
          FROM SchoolManagement.LeaveOfAbsence 
          WHERE TeacherID = @teacherId AND Status = 'Approved' AND EndDate >= GETDATE()
          ORDER BY StartDate DESC", new { teacherId }) ?? "";
        }

        public async Task<string> GetActiveLeaveTypeForStaffAsync(int staffId)
        {
            using var conn = new SqlConnection(connectionString);
            return await conn.QueryFirstOrDefaultAsync<string>(
                @"SELECT TOP 1 LeaveType 
          FROM SchoolManagement.LeaveOfAbsence 
          WHERE StaffID = @staffId AND Status = 'Approved' AND EndDate >= GETDATE()
          ORDER BY StartDate DESC", new { staffId }) ?? "";
        }

        public async Task<IEnumerable<LeaveOfAbsence>> GetLeaveHistoryForTeacherAsync(int teacherId)
        {
            using var conn = new SqlConnection(connectionString);
            return await conn.QueryAsync<LeaveOfAbsence>(
                @"SELECT * FROM SchoolManagement.LeaveOfAbsence 
          WHERE TeacherID = @teacherId 
          ORDER BY StartDate DESC", new { teacherId });
        }

        public async Task<IEnumerable<LeaveOfAbsence>> GetLeaveHistoryForStaffAsync(int staffId)
        {
            using var conn = new SqlConnection(connectionString);
            return await conn.QueryAsync<LeaveOfAbsence>(
                @"SELECT * FROM SchoolManagement.LeaveOfAbsence 
          WHERE StaffID = @staffId 
          ORDER BY StartDate DESC", new { staffId });
        }

        /*Approval of leave*/
        public async Task<IEnumerable<LeaveOfAbsenceViewModel>> GetPendingLeaveRequestsAsync()
        {
            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();

                // Only get pending leave requests (Status = 0)
                return await connection.QueryAsync<LeaveOfAbsenceViewModel>(
                   @"SELECT l.LeaveID, l.UserID, u.UserName, l.LeaveType, l.StartDate, l.EndDate, l.Reason, l.Status
      FROM SchoolManagement.LeaveOfAbsence l
      INNER JOIN SchoolManagement.Users u ON l.UserID = u.UserID
      WHERE l.Status = 'Pending'"
                );
            }
        }


        public async Task UpdateLeaveRequestStatusAsync(LeaveOfAbsenceViewModel leaveRequest, int updatedBy)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();
                var query = @"
            UPDATE SchoolManagement.LeaveOfAbsence 
            SET 
                Status = @Status, 
                ApprovedBy = @ApprovedBy, 
                DateApproved = @DateApproved, 
                UpdatedBY = @UpdatedBY 
            WHERE LeaveID = @LeaveID";

                await connection.ExecuteAsync(query, new
                {
                    Status = leaveRequest.Status.ToString(), // ?? convert enum to string
                    ApprovedBy = updatedBy,
                    DateApproved = DateTime.Now,
                    UpdatedBY = updatedBy,
                    leaveRequest.LeaveID
                });
            }
        }


        public async Task<IEnumerable<LeaveOfAbsenceViewModel>> GetLeaveRequestsByDateRangeAsync(DateTime fromDate, DateTime toDate)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();
                return await connection.QueryAsync<LeaveOfAbsenceViewModel>(
                    "SELECT l.LeaveID, l.UserID, u.UserName, l.LeaveType, l.StartDate, l.EndDate, l.Reason, l.Status FROM SchoolManagement.LeaveOfAbsence l INNER JOIN SchoolManagement.Users u ON l.UserID = u.UserID WHERE l.StartDate BETWEEN @FromDate AND @ToDate",
                    new { FromDate = fromDate, ToDate = toDate }
                );
            }
        }

        /*Leave Dialog*/
        public async Task<IEnumerable<LeaveOfAbsenceViewModel>> GetLeaveRequestsByStatusAsync(LeaveStatus status)
        {
            using var connection = new SqlConnection(connectionString);
            string query = @"
        SELECT 
            loa.LeaveID,
            loa.UserID,
            u.UserName,
            loa.LeaveType,
            loa.StartDate,
            loa.EndDate,
            loa.Reason,
            loa.Status,
            loa.ApprovedBy,
            loa.DateApproved,
            loa.UpdatedBY
        FROM SchoolManagement.LeaveOfAbsence loa
        INNER JOIN SchoolManagement.Users u ON loa.UserID = u.UserID
        WHERE loa.Status = @Status";

            var results = await connection.QueryAsync<LeaveOfAbsenceViewModel>(query, new { Status = status.ToString() });
            return results;
        }


        /*Print time table*/
        public async Task<IEnumerable<ClassTimetableModel>> GetClassTimetableByClassIdAsync(string classId)
        {
            using var connection = new SqlConnection(connectionString);

            var query = @"
        SELECT 
            s.ScheduleID,
            s.ClassID,
            s.SCID,
            c.SchoolCourse AS Subject,  -- Changed alias here
            d.DayName AS Day,
            s.SubjectStartTime AS StartTime,
            s.SubjectEndTime AS EndTime,
            CASE 
                WHEN s.BeforeFirstBreak = 1 THEN 'BeforeFirstBreak'
                WHEN s.AfterFirstBreak = 1 THEN 'AfterFirstBreak'
                WHEN s.AfterSecondBreak = 1 THEN 'AfterSecondBreak'
                ELSE 'Unknown'
            END AS Period
        FROM SchoolManagement.StudentTimetable_Schedule s
        INNER JOIN SchoolManagement.SchoolCourse c ON s.SCID = c.SCID
        INNER JOIN SchoolManagement.StudentTimetable_Days d ON s.DayID = d.DayID
        WHERE s.ClassID = @ClassID
        ORDER BY d.DayID, s.SubjectStartTime";

            var result = await connection.QueryAsync<ClassTimetableModel>(query, new { ClassID = classId });
            return result;
        }

        // For Expense Category List
        public async Task<List<ExpenseCategory>> GetExpenseCategoriesAsync()
        {
            using var connection = new SqlConnection(connectionString);
            var query = "SELECT * FROM SchoolManagement.ExpenseCategories";
            var result = await connection.QueryAsync<ExpenseCategory>(query);
            return result.ToList();
        }

        // For Payment Method List
        public async Task<List<PaymentMethods>> GetPaymentMethodsExpenesAsync()
        {
            using var connection = new SqlConnection(connectionString);
            var query = "SELECT * FROM SchoolManagement.PaymentMethods";
            var result = await connection.QueryAsync<PaymentMethods>(query);
            return result.ToList();
        }

        // Save Expense
        public async Task SaveExpenseAsync(Expense expense)
        {
            using var connection = new SqlConnection(connectionString);
            var query = @"
        INSERT INTO SchoolManagement.Expenses (UserID, CategoryID, Amount, ExpenseDate, Description)
        VALUES (@UserID, @CategoryID, @Amount, @ExpenseDate, @Description)";
            await connection.ExecuteAsync(query, expense);
        }

        public async Task<bool> ProcessExpensePaymentAsync(Expense expense, int paymentMethodId)
        {
            try
            {
                using var connection = new SqlConnection(connectionString);

                // Step 1: Get BankID from payment method
                string getBankQuery = @"
                 SELECT b.BankID, b.AmountTransferred 
                 FROM SchoolManagement.Bank b 
                 JOIN SchoolManagement.PaymentMethods pm ON b.MethodName = pm.MethodName
                 WHERE pm.PaymentMethodID = @PaymentMethodId";

                var bankInfo = await connection.QueryFirstOrDefaultAsync<(int BankID, decimal AmountTransferred)>(
                    getBankQuery, new { PaymentMethodId = paymentMethodId });

                if (bankInfo.BankID == 0)
                {
                    await LogsBankTransactionAsync(null, "Withdrawal", expense.Amount, "Failed", "Invalid Payment Method or BankID not found.");
                    return false;
                }

                // Step 2: Check if bank has enough balance
                if (bankInfo.AmountTransferred < expense.Amount)
                {
                    await LogsBankTransactionAsync(bankInfo.BankID, "Withdrawal", expense.Amount, "Failed", "Insufficient funds.");
                    return false;
                }

                // Step 3: Deduct the amount from the bank
                string deductQuery = "UPDATE SchoolManagement.Bank SET AmountTransferred = AmountTransferred - @Amount WHERE BankID = @BankID";
                int rowsAffected = await connection.ExecuteAsync(deductQuery, new { Amount = expense.Amount, BankID = bankInfo.BankID });

                if (rowsAffected == 0)
                {
                    await LogsBankTransactionAsync(bankInfo.BankID, "Withdrawal", expense.Amount, "Failed", "No rows affected.");
                    return false;
                }

                // Step 4: Log the successful bank transaction
                await LogsBankTransactionAsync(bankInfo.BankID, "Withdrawal", expense.Amount, "Success", null);

                // Step 5: Log the payment
                await LogPaymentAsync(null, bankInfo.BankID, "Expense", expense.Amount, "Success", null, paymentMethodId);

                // Step 6: Save the actual expense
                string insertExpenseQuery = @"
                   INSERT INTO SchoolManagement.Expenses (UserID, CategoryID, Amount, ExpenseDate, Description)
                   VALUES (@UserID, @CategoryID, @Amount, @ExpenseDate, @Description)";
                await connection.ExecuteAsync(insertExpenseQuery, expense);

                return true;
            }
            catch (Exception ex)
            {
                await LogsBankTransactionAsync(null, "Withdrawal", expense.Amount, "Failed", ex.Message);
                return false;
            }
        
        }
        //added a method to fetch the list of processed expenses
        public async Task<IEnumerable<ExpenseHistoryView>> GetExpenseHistoryAsync()
        {
            using var connection = new SqlConnection(connectionString);
            string query = @"
        SELECT e.ExpenseID, u.UserName, c.CategoryName, e.Amount, e.Description, e.ExpenseDate
        FROM SchoolManagement.Expenses e
        JOIN SchoolManagement.Users u ON e.UserID = u.UserID
        JOIN SchoolManagement.ExpenseCategories c ON e.CategoryID = c.CategoryID
        ORDER BY e.ExpenseDate DESC";

            return await connection.QueryAsync<ExpenseHistoryView>(query);
        }


        /*Bank Deposit*/
        public async Task<bool> InsertBankDepositAsync(BankDeposit deposit, int userId)
        {
            using var connection = new SqlConnection(connectionString);

            var systemTransferId = new Random().Next(100000, 999999);
            var query = @"
INSERT INTO SchoolManagement.Bank (
    PaymentMethodID,
    BankNumber,
    MethodName,
    AmountTransferred,
    SystemTransferID,
    AmountInHand,
    Remarks,
    UserID
)
VALUES (
    @PaymentMethodID,
    @BankNumber,
    @MethodName,
    @AmountTransferred,
    @SystemTransferID,
    @AmountTransferred,
    @Remarks,
    @UserID
)";

            var result = await connection.ExecuteAsync(query, new
            {
                deposit.PaymentMethodID,
                deposit.BankNumber,
                deposit.MethodName,
                deposit.AmountTransferred,
                SystemTransferID = systemTransferId,
                deposit.Remarks,
                UserID = userId
            });

            return result > 0;
        }

        public async Task<List<PaymentMethods>> GetAllPaymentMethodsAsync()
        {
            using var connection = new SqlConnection(connectionString);
            var sql = @"
        SELECT 
            PaymentMethodID, 
            MethodName, 
            BankNumber -- ensure this column is retrieved
        FROM 
            SchoolManagement.PaymentMethods";

            var result = await connection.QueryAsync<PaymentMethods>(sql);
            return result.ToList();
        }

        public async Task<bool> LogManualDepositAsync(BankDeposit deposit, int userId)
        {
            try
            {
                using var connection = new SqlConnection(connectionString);

                // Step 1: Generate a unique SystemTransferID
                var systemTransferId = new Random().Next(100000, 999999);

                // Step 2: Insert into Bank table
                string insertBankQuery = @"
            INSERT INTO SchoolManagement.Bank (
                PaymentMethodID,
                BankNumber,
                MethodName,
                AmountTransferred,
                SystemTransferID,
                AmountInHand,
                Remarks,
                UserID
            )
            VALUES (
                @PaymentMethodID,
                @BankNumber,
                @MethodName,
                @AmountTransferred,
                @SystemTransferID,
                @AmountTransferred,
                @Remarks,
                @UserID
            );
            SELECT CAST(SCOPE_IDENTITY() AS INT);";

                int bankId = await connection.ExecuteScalarAsync<int>(insertBankQuery, new
                {
                    deposit.PaymentMethodID,
                    deposit.BankNumber,
                    deposit.MethodName,
                    deposit.AmountTransferred,
                    SystemTransferID = systemTransferId,
                    deposit.Remarks,
                    UserID = userId
                });

                // Step 3: Log the transaction in BankTransactionLogs
                await LogsBankTransactionAsync(bankId, "Deposit", deposit.AmountTransferred, "Success Manual Deposit", "");

                return true;
            }
            catch (Exception ex)
            {
                await LogsBankTransactionAsync(null, "Deposit", deposit.AmountTransferred, "Failed", ex.Message);
                return false;
            }
        }

        public async Task<bool> TransferFundsAsync(FundTransfer transfer, int userId)
        {
            using var connection = new SqlConnection(connectionString);

            try
            {
                Console.WriteLine("Opening SQL connection...");
                await connection.OpenAsync();
                Console.WriteLine("SQL connection opened.");

                using var transaction = connection.BeginTransaction();
                Console.WriteLine("Transaction started.");

                var systemTransferId = new Random().Next(100000, 999999);

                var fromBank = await connection.QueryFirstOrDefaultAsync<dynamic>(
                    "SELECT TOP 1 * FROM SchoolManagement.Bank WHERE MethodName = @MethodName ORDER BY BankID DESC",
                    new { MethodName = transfer.FromMethod }, transaction: transaction);

                var toBank = await connection.QueryFirstOrDefaultAsync<dynamic>(
                    "SELECT TOP 1 * FROM SchoolManagement.Bank WHERE MethodName = @MethodName ORDER BY BankID DESC",
                    new { MethodName = transfer.ToMethod }, transaction: transaction);

                if (fromBank == null || toBank == null)
                    throw new Exception("One of the selected payment methods does not exist.");

                if ((decimal)fromBank.AmountInHand < transfer.Amount)
                    throw new Exception("Insufficient funds in the source method.");

                string insertQuery = @"INSERT INTO SchoolManagement.Bank 
            (PaymentMethodID, BankNumber, MethodName, AmountTransferred, SystemTransferID, AmountInHand, Remarks, UserID)
            VALUES 
            (@PaymentMethodID, @BankNumber, @MethodName, @AmountTransferred, @SystemTransferID, @AmountInHand, @Remarks, @UserID)";

                // Deduct
                await connection.ExecuteAsync(insertQuery, new
                {
                    PaymentMethodID = fromBank.PaymentMethodID,
                    BankNumber = fromBank.BankNumber,
                    MethodName = transfer.FromMethod,
                    AmountTransferred = -transfer.Amount,
                    SystemTransferID = systemTransferId,
                    AmountInHand = (decimal)fromBank.AmountInHand - transfer.Amount,
                    Remarks = "Fund transfer to " + transfer.ToMethod + " - " + transfer.Remarks,
                    UserID = userId
                }, transaction);

                // Credit
                await connection.ExecuteAsync(insertQuery, new
                {
                    PaymentMethodID = toBank.PaymentMethodID,
                    BankNumber = toBank.BankNumber,
                    MethodName = transfer.ToMethod,
                    AmountTransferred = transfer.Amount,
                    SystemTransferID = systemTransferId,
                    AmountInHand = (decimal)toBank.AmountInHand + transfer.Amount,
                    Remarks = "Fund received from " + transfer.FromMethod + " - " + transfer.Remarks,
                    UserID = userId
                }, transaction);

                transaction.Commit();
                Console.WriteLine("Transaction committed.");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception occurred: " + ex.Message);
                Console.WriteLine(ex.StackTrace);
                throw new Exception($"Transfer failed: {ex.Message}", ex);
            }
        }


        public async Task<decimal> GetTotalCashInHandAsync()
        {
            using (var connection = new SqlConnection(connectionString))
            {
                string sql = @"
            SELECT ISNULL(SUM(AmountTransferred), 0)
            FROM SchoolManagement.Bank
            WHERE MethodName = 'Cash In Hand'";

                return await connection.ExecuteScalarAsync<decimal>(sql);
            }
        }


        /*Atendance Print per class and student*/
        public async Task<IEnumerable<SchoolTerm>> GetAllTermsAsync()
        {
            using var conn = new SqlConnection(connectionString);
            await conn.OpenAsync();
            const string sql = "SELECT TermID, Term FROM SchoolManagement.SchoolTerm ORDER BY TermID";
            return await conn.QueryAsync<SchoolTerm>(sql);
        }

        public async Task<IEnumerable<StudentsAttendance>> GetAttendanceByClassTermDateAsync(
            string classId, int termId, DateTime date)
        {
            using var conn = new SqlConnection(connectionString);
            await conn.OpenAsync();
            const string sql = @"
                    SELECT 
                      a.StudentFirstName,
                      a.StudentLastName,
                      a.ClassID,
                      a.EnableSwitch,
                      a.RecDateCreated,
                      t.Term
                    FROM SchoolManagement.StudentsAttendance a
                    JOIN SchoolManagement.SchoolTerm t
                      ON a.TermID = t.TermID
                    WHERE a.ClassID = @ClassID
                      AND a.TermID = @TermID
                      AND CAST(a.RecDateCreated AS DATE) = @Date;";

            return await conn.QueryAsync<StudentsAttendance>(sql,
                new { ClassID = classId, TermID = termId, Date = date.Date });
        }

        // 1) Search students by name fragment
        public async Task<IEnumerable<StudentLookupDto>> SearchAllStudentsAsync(string term)
        {
            using var conn = new SqlConnection(connectionString);
            await conn.OpenAsync();
            const string sql = @"
      SELECT StudentID, StudentFirstName, StudentLastName, ClassID
      FROM SchoolManagement.Students
      WHERE StudentFirstName LIKE @p OR StudentLastName LIKE @p
      ORDER BY StudentLastName";
            return await conn.QueryAsync<StudentLookupDto>(
              sql, new { p = $"%{term}%" });
        }

        // 2) Fetch attendance by student
        public async Task<IEnumerable<StudentsAttendance>> GetAttendanceByStudentAsync(
      string firstName, string lastName, string classId)
        {
            using var conn = new SqlConnection(connectionString);
            await conn.OpenAsync();

            const string sql = @"
      SELECT 
        a.AttendanceDate,
        a.EnableSwitch,
        a.TermID,
        t.Term
      FROM SchoolManagement.StudentsAttendance AS a
      INNER JOIN SchoolManagement.SchoolTerm AS t
        ON a.TermID = t.TermID
      WHERE a.StudentFirstName = @FirstName
        AND a.StudentLastName  = @LastName
        AND a.ClassID          = @ClassID
      ORDER BY a.AttendanceDate;";

            return await conn.QueryAsync<StudentsAttendance>(
                sql,
                new
                {
                    FirstName = firstName,
                    LastName = lastName,
                    ClassID = classId
                });
        }


        /*Print Lesson Note*/
        public async Task<IEnumerable<User>> GetsAllTeachersForLessonNotePrintAsync()
        {
            using var con = new SqlConnection(connectionString);
            await con.OpenAsync();
            const string sql = @"
        SELECT UserID, FullName 
        FROM SchoolManagement.Users 
        ORDER BY FullName
    ";
            return await con.QueryAsync<User>(sql);
        }

        public async Task<IEnumerable<SchoolTerm>> GetAllTermsForLessonNotePrintAsync()
        {
            using var con = new SqlConnection(connectionString);
            await con.OpenAsync();
            const string sql = "SELECT TermID, Term FROM SchoolManagement.SchoolTerm ORDER BY TermID";
            return await con.QueryAsync<SchoolTerm>(sql);
        }

        public async Task<IEnumerable<LessonNote>> GetLessonNotesByTeacherTermDateRangeAsync(
     int userId, int termId, DateTime fromDate, DateTime toDate)
        {
            using var con = new SqlConnection(connectionString);
            await con.OpenAsync();
            const string sql = @"
        SELECT * FROM SchoolManagement.TEACHERSLESSONNOTES
        WHERE UserID = @UserId
        AND TermID = @TermId
        AND LessonNoteDate BETWEEN @FromDate AND @ToDate
    ";
            return await con.QueryAsync<LessonNote>(
                sql,
                new { UserId = userId, TermId = termId, FromDate = fromDate, ToDate = toDate }
            );
        }

        /*for updating staffs employee status*/
        // Get all staff records with minimal info for grid
        public async Task<IEnumerable<Staff>> GetDesplayAllStaffAsync()
        {
            using var connection = new SqlConnection(connectionString);
            var sql = @"SELECT * FROM SchoolManagement.Staff";
            return await connection.QueryAsync<Staff>(sql);
        }

        public async Task UpdateStaffEmploymentStatusAsync(int staffId, string newStatus, int changedByUserId)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();
                var oldStatus = await connection.QueryFirstOrDefaultAsync<string>(
                    "SELECT EmploymentStatus FROM SchoolManagement.Staff WHERE StaffID = @StaffID",
                    new { StaffID = staffId });

                await connection.ExecuteAsync(
                    "UPDATE SchoolManagement.Staff SET EmploymentStatus = @NewStatus WHERE StaffID = @StaffID",
                    new { NewStatus = newStatus, StaffID = staffId });

                await connection.ExecuteAsync(@"
                    INSERT INTO SchoolManagement.StaffEmploymentStatusHistory 
                    (StaffID, OldStatus, NewStatus, ChangedByUserID, Remarks, ChangeDate)
                    VALUES (@StaffID, @OldStatus, @NewStatus, @ChangedByUserID, @Remarks, GETDATE())",
                    new
                    {
                        StaffID = staffId,
                        OldStatus = oldStatus,
                        NewStatus = newStatus,
                        ChangedByUserID = changedByUserId,
                        Remarks = "Updated Employment Status"
                    });
            }

        }

        //TEACHERS ATTENDANCE PDF EXPORT
        public async Task<IEnumerable<string>> GetTeacherNamesAsync()
        {
            using var connection = new SqlConnection(connectionString);
            var query = @"SELECT DISTINCT CONCAT(TeacherFirstName, ' ', TeacherLastName) AS FullName
                      FROM SchoolManagement.TeachersAttendanceOut";
            return await connection.QueryAsync<string>(query);
        }

        public async Task<IEnumerable<dynamic>> GetAttendanceByTeacherAndDate(string teacherName, DateTime date)
        {
            using var connection = new SqlConnection(connectionString);

            var query = @"
    SELECT 
        A.TeacherFirstName,
        A.TeacherLastName,
        A.ClockIN,
        B.ClockOUT
    FROM SchoolManagement.TeachersAttendance A
    JOIN SchoolManagement.TeachersAttendanceOut B
        ON A.TeacherFirstName = B.TeacherFirstName
        AND A.TeacherLastName = B.TeacherLastName
        AND CAST(A.RecDateCreated AS DATE) = CAST(B.RecDateCreated AS DATE)
    WHERE CONCAT(A.TeacherFirstName, ' ', A.TeacherLastName) = @Name
      AND CAST(A.ClockIN AS DATE) = @Date";

            return await connection.QueryAsync<dynamic>(query, new { Name = teacherName, Date = date.Date });
        }


        public async Task<IEnumerable<dynamic>> GetAttendanceByDateRange(DateTime from, DateTime to)
        {
            using var connection = new SqlConnection(connectionString);

            var query = @"
    SELECT 
        A.TeacherFirstName,
        A.TeacherLastName,
        A.ClockIN,
        B.ClockOUT
    FROM SchoolManagement.TeachersAttendance A
    JOIN SchoolManagement.TeachersAttendanceOut B
        ON A.TeacherFirstName = B.TeacherFirstName
        AND A.TeacherLastName = B.TeacherLastName
        AND CAST(A.RecDateCreated AS DATE) = CAST(B.RecDateCreated AS DATE)
    WHERE CAST(A.ClockIN AS DATE) BETWEEN @From AND @To";

            return await connection.QueryAsync<dynamic>(query, new { From = from.Date, To = to.Date });
        }

        //transactio fees view
        public async Task<List<FeeTransactionSummary>> GetFeeTransactionSummariesAsync()
        {
            string cacheKey = "FeeTransactionSummaries";

            return await _cacheService.GetOrSetAsync(cacheKey, async () =>
            {
                using var connection = new SqlConnection(connectionString);
                var query = @"
            SELECT 
                sf.FeeTypeName,
                b.MethodName,
                sf.AmountPaid,
                sf.PaymentDate
            FROM 
                SchoolManagement.StudentFees sf
            LEFT JOIN 
                SchoolManagement.Bank b ON sf.SystemTransferID = b.SystemTransferID
            WHERE 
                sf.AmountPaid > 0
            ORDER BY 
                sf.PaymentDate DESC;
        ";

                var result = await connection.QueryAsync<FeeTransactionSummary>(query);
                return result.ToList();

            }, minutes: 10); // Cached for 10 minutes
        }

        public async Task<List<FeeTransactionSummary>> GetFilteredFeeTransactionSummariesAsync(DateTime? fromDate, DateTime? toDate, string feeTypeName)
        {
            // Generate a unique cache key based on the parameters
            string cacheKey = $"FilteredFeeTransactions_{fromDate?.ToString("yyyyMMdd")}_{toDate?.ToString("yyyyMMdd")}_{feeTypeName ?? "All"}";

            return await _cacheService.GetOrSetAsync(cacheKey, async () =>
            {
                using var connection = new SqlConnection(connectionString);

                var query = new StringBuilder(@"
            SELECT 
                sf.FeeTypeName,
                b.MethodName,
                sf.AmountPaid,
                sf.PaymentDate
            FROM 
                SchoolManagement.StudentFees sf
            LEFT JOIN 
                SchoolManagement.Bank b ON sf.SystemTransferID = b.SystemTransferID
            WHERE 
                sf.AmountPaid > 0
        ");

                var parameters = new DynamicParameters();

                if (fromDate.HasValue)
                {
                    query.Append(" AND sf.PaymentDate >= @FromDate");
                    parameters.Add("FromDate", fromDate.Value.Date);
                }

                if (toDate.HasValue)
                {
                    query.Append(" AND sf.PaymentDate <= @ToDate");
                    parameters.Add("ToDate", toDate.Value.Date.AddDays(1).AddSeconds(-1));
                }

                if (!string.IsNullOrEmpty(feeTypeName))
                {
                    query.Append(" AND sf.FeeTypeName = @FeeTypeName");
                    parameters.Add("FeeTypeName", feeTypeName);
                }

                query.Append(" ORDER BY sf.PaymentDate DESC");

                var result = await connection.QueryAsync<FeeTransactionSummary>(query.ToString(), parameters);
                return result.ToList();

            }, minutes: 10); // Cache for 10 minutes
        }

        //view all students page
        public async Task<List<Student>> GetAllStudentsAsync()
        {
            string cacheKey = "AllStudents";

            return await _cacheService.GetOrSetAsync(cacheKey, async () =>
            {
                using var connection = new SqlConnection(connectionString);
                string query = @"
            SELECT StudentID, StudentFirstName, StudentLastName, ClassID
            FROM SchoolManagement.Students
            ORDER BY ClassID, StudentFirstName ASC, StudentLastName ASC";

                var result = await connection.QueryAsync<Student>(query);
                return result.ToList();

            }, minutes: 10); // Cache for 10 minutes
        }

        public async Task DeleteStudentAsync(int studentId)
        {
            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            using var transaction = connection.BeginTransaction();

            try
            {
                // Move student to DeletedStudents
                string moveQuery = @"
            INSERT INTO SchoolManagement.DeletedStudents
            SELECT *, GETDATE()
            FROM SchoolManagement.Students
            WHERE StudentID = @StudentID;

            DELETE FROM SchoolManagement.Students
            WHERE StudentID = @StudentID;";

                await connection.ExecuteAsync(moveQuery, new { StudentID = studentId }, transaction);

                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }


        public async Task<IEnumerable<StudentFeeHistory>> GetStudentFeeHistoryAsync(int studentId, int termId)
        {
            using var connection = new SqlConnection(connectionString);
            string query = @"SELECT FeeTypeName, 
                                    AmountLeft, 
                                    AmountPaid, 
                                    (AmountLeft + AmountPaid) AS Amount
                             FROM SchoolManagement.StudentFees
                             WHERE StudentID = @StudentID AND TermID = @TermID";
            return await connection.QueryAsync<StudentFeeHistory>(query, new { StudentID = studentId, TermID = termId });
        }

        public async Task<IEnumerable<Student>> GetStudentsWithClassAsync()
        {
            using var connection = new SqlConnection(connectionString);
            string query = @"SELECT s.StudentID, 
                            s.StudentFirstName, 
                            s.StudentLastName, 
                            s.ClassID 
                     FROM SchoolManagement.Students s";
            return await connection.QueryAsync<Student>(query);
        }

        public async Task<ClassFeeInfo?> GetClassFeeInfoAsync(string classId)
        {
            using var connection = new SqlConnection(connectionString);
            string query = @"
                SELECT FeeTypeName, Amount
                FROM SchoolManagement.FeeTypes
                WHERE ClassID = @ClassID AND DeletedBy IS NULL
                ORDER BY RecDateCreated DESC";

            return await connection.QueryFirstOrDefaultAsync<ClassFeeInfo>(query, new { ClassID = classId });
        }

        /*bank balance*/

        public async Task<IEnumerable<BankBalance>> GetBankBalancesAsync()
        {
            using var connection = new SqlConnection(connectionString);
            const string sql = "SELECT MethodName, TotalAmount FROM SchoolManagement.BankBalances";
            return await connection.QueryAsync<BankBalance>(sql);
        }

        public async Task<int> SyncBankBalancesAsync(int userId)
        {
            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            // Start a database transaction to ensure atomicity
            using var transaction = connection.BeginTransaction();

            try
            {
                // Clear the BankBalances table before inserting fresh totals
                const string deleteSql = "DELETE FROM SchoolManagement.BankBalances";
                await connection.ExecuteAsync(deleteSql, transaction: transaction);

                // Insert the sum of AmountTransferred grouped by MethodName
                const string insertSql = @"
            INSERT INTO SchoolManagement.BankBalances (MethodName, TotalAmount)
            SELECT MethodName, SUM(AmountTransferred)
            FROM SchoolManagement.Bank
            GROUP BY MethodName";

                await connection.ExecuteAsync(insertSql, transaction: transaction);

                // Log the successful sync operation
                const string logSql = @"
            INSERT INTO SchoolManagement.BankBalanceSyncLog (SyncByUserID, Status)
            VALUES (@UserID, 'SUCCESS')";

                await connection.ExecuteAsync(logSql, new { UserID = userId }, transaction: transaction);

                // Commit the transaction
                await transaction.CommitAsync();

                return 1;  // success
            }
            catch (Exception ex)
            {
                // Attempt to log the failure, ignoring any logging errors
                try
                {
                    const string logFailSql = @"
                INSERT INTO SchoolManagement.BankBalanceSyncLog (SyncByUserID, Status, ErrorMessage)
                VALUES (@UserID, 'FAILED', @ErrorMessage)";

                    await connection.ExecuteAsync(logFailSql, new
                    {
                        UserID = userId,
                        ErrorMessage = ex.Message
                    }, transaction: transaction);
                }
                catch
                {
                    // Ignored: logging failure should not throw
                }

                // Roll back the transaction since an error occurred
                await transaction.RollbackAsync();

                return 0;  // failure
            }
        }

        public async Task<(int StudentOwingByClass, decimal TotalOwingAmount)> GetStudentsOwingSummaryAsync()
        {
            string cacheKey = "OwingStudentsSummary";

            return await _cacheService.GetOrSetAsync(cacheKey, async () =>
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    var sql = @"
                WITH ExpectedFeesPerStudent AS (
                    SELECT 
                        s.StudentID,
                        s.StudentFirstName + ' ' + s.StudentLastName AS StudentFullName,
                        s.ClassID,
                        ft.FeeTypeID,
                        ft.FeeTypeName,
                        ft.Amount AS ExpectedAmount
                    FROM SchoolManagement.Students s
                    INNER JOIN SchoolManagement.FeeTypes ft ON s.ClassID = ft.ClassID
                ),
                ActualPayments AS (
                    SELECT 
                        sf.StudentID,
                        sf.FeeTypeID,
                        sf.AmountLeft
                    FROM SchoolManagement.StudentFees sf
                ),
                FeesOwed AS (
                    SELECT 
                        efps.StudentID,
                        efps.StudentFullName,
                        efps.FeeTypeName,
                        efps.ExpectedAmount,
                        ap.AmountLeft,
                        CASE 
                            WHEN ap.StudentID IS NULL THEN efps.ExpectedAmount
                            WHEN ap.AmountLeft IS NULL THEN 0
                            ELSE ap.AmountLeft
                        END AS OwedAmount
                    FROM ExpectedFeesPerStudent efps
                    LEFT JOIN ActualPayments ap 
                        ON efps.StudentID = ap.StudentID AND efps.FeeTypeID = ap.FeeTypeID
                ),
                OwingStudents AS (
                    SELECT 
                        StudentID,
                        SUM(OwedAmount) AS TotalOwed
                    FROM FeesOwed
                    GROUP BY StudentID
                    HAVING SUM(OwedAmount) > 0
                )

                SELECT 
                    COUNT(*) AS StudentOwingByClass,
                    SUM(TotalOwed) AS TotalOwingAmount
                FROM OwingStudents;";

                    return await connection.QueryFirstOrDefaultAsync<(int, decimal)>(sql);
                }
            }, minutes: 15);
        }

        //
        public async Task<List<TermFeeSummary>> GetTermFeeSummariesAsync()
        {
            var summaries = new List<TermFeeSummary>();

            using var connection = new SqlConnection(connectionString);

            // Step 1: Get all distinct TermIDs from StudentFees
            var termIds = (await connection.QueryAsync<int>(
                "SELECT DISTINCT TermID FROM SchoolManagement.StudentFees")).ToList();

            foreach (var termId in termIds)
            {
                decimal expectedTotal = 0;
                decimal actualTotal = 0;

                // Step 2: Get all students and their ClassID
                var students = await connection.QueryAsync<(int StudentID, string ClassID)>(
                    "SELECT StudentID, ClassID FROM SchoolManagement.Students WHERE EnableSwitch = 1");

                foreach (var student in students)
                {
                    // Step 3: Get all fee types for the student's class
                    var classFees = await connection.QueryAsync<decimal>(
                        "SELECT Amount FROM SchoolManagement.FeeTypes WHERE ClassID = @ClassID",
                        new { ClassID = student.ClassID });

                    expectedTotal += classFees.Sum(); // Add up all fees for this student
                }

                // Step 4: Get actual amount paid for this term
                actualTotal = await connection.ExecuteScalarAsync<decimal>(
                    "SELECT ISNULL(SUM(AmountPaid), 0) FROM SchoolManagement.StudentFees WHERE TermID = @TermID",
                    new { TermID = termId });

                summaries.Add(new TermFeeSummary
                {
                    TermID = termId,
                    ExpectedTotal = expectedTotal,
                    ActualTotal = actualTotal
                });
            }

            return summaries;
        }

        public async Task<List<ClassTermFeeSummary>> GetClassTermFeeSummariesAsync(int termId)
        {
            using var connection = new SqlConnection(connectionString);

            var sql = @"
    SELECT 
        c.ClassID,
        @TermID AS TermID,
        -- Total number of students in the class
        (SELECT COUNT(*) 
         FROM SchoolManagement.Students s 
         WHERE s.ClassID = c.ClassID AND s.EnableSwitch = 1) AS TotalStudents,

        -- Total number of fee types for the class
        (SELECT COUNT(*) 
         FROM SchoolManagement.FeeTypes f 
         WHERE f.ClassID = c.ClassID) AS FeeTypesCount,

        -- ExpectedTotal = Total fee per student * Number of students
        (SELECT ISNULL(SUM(f.Amount), 0) 
         FROM SchoolManagement.FeeTypes f 
         WHERE f.ClassID = c.ClassID) 
         * 
        (SELECT COUNT(*) 
         FROM SchoolManagement.Students s 
         WHERE s.ClassID = c.ClassID AND s.EnableSwitch = 1) AS ExpectedTotal,

        -- ActualTotal paid by students for the selected term
        (SELECT ISNULL(SUM(sf.AmountPaid), 0) 
         FROM SchoolManagement.StudentFees sf 
         WHERE sf.ClassID = c.ClassID AND sf.TermID = @TermID) AS ActualTotal

    FROM 
        (SELECT DISTINCT ClassID FROM SchoolManagement.Students) c;
    ";

            var result = await connection.QueryAsync<ClassTermFeeSummary>(sql, new { TermID = termId });

            // Calculate Outstanding after query (safe in C#)
            foreach (var summary in result)
            {
                summary.Outstanding = summary.ExpectedTotal - summary.ActualTotal;
            }

            return result.ToList();
        }

        public async Task<List<StudentFee>> GetStudentFeesByClassAndTerm(string classId, int termId)
        {
            var query = @"
        SELECT 
            FeeID, StudentID, StudentName, ClassID, FeeTypeName,
            AmountPaid, AmountLeft, PaymentDate, TermID
        FROM SchoolManagement.StudentFees
        WHERE ClassID = @ClassID AND TermID = @TermID AND AmountPaid > 0
        ORDER BY StudentName";

            using (var connection = new SqlConnection(connectionString))
            {
                var result = await connection.QueryAsync<StudentFee>(query, new { ClassID = classId, TermID = termId });
                return result.ToList();
            }
        }

        /*fees paid per date*/
        public async Task<IEnumerable<StudentFee>> GetsStudentFeesByDateAsync(DateTime dateCreated)
        {
            using var connection = new SqlConnection(connectionString);
            var result = await connection.QueryAsync<StudentFee>(
                "GetAllStudentsFeesPerDate",
                new { DateCreated = dateCreated },
                commandType: CommandType.StoredProcedure);
            return result;
        }

        /*FEEDING FEE*/
        public async Task<List<SchoolTerm>> GetSchoolTermsAsync()
        {
            using var connection = new SqlConnection(connectionString);
            string query = "SELECT TermID, Term, IsCurrentTerm FROM SchoolManagement.SchoolTerm ORDER BY TermID DESC";
            var result = await connection.QueryAsync<SchoolTerm>(query);
            return result.ToList();
        }

        public async Task<List<OtherFee>> GetOtherFeesAsync()
        {
            using var connection = new SqlConnection(connectionString);
            string query = "SELECT FeeTypeID, FeeTypeName, Amount, ClassID FROM SchoolManagement.OtherFees";
            var result = await connection.QueryAsync<OtherFee>(query);
            return result.ToList();
        }

        public async Task SavePaymentAsync(PaymentsOtherFee payment)
        {
            using var connection = new SqlConnection(connectionString);
            string query = @"
        INSERT INTO SchoolManagement.PaymentsOtherFees 
        (StudentID, FeeTypeID, StudentName, FeeTypeName, ClassID, AmountPaid, AmountLeft, PaymentDate, UserID, PaymentMethod, TermID, PaymentStatus) 
        VALUES (@StudentID, @FeeTypeID, @StudentName, @FeeTypeName, @ClassID, @AmountPaid, @AmountLeft, @PaymentDate, @UserID, @PaymentMethod, @TermID, @PaymentStatus)";

            await connection.ExecuteAsync(query, payment);
        }

        public async Task<List<Student>> GetAllStudentsNameAsync()
        {
            using var connection = new SqlConnection(connectionString);

            string query = @"
        SELECT StudentID, StudentFirstName, StudentLastName, ClassID
        FROM SchoolManagement.Students
        WHERE EnableSwitch = 1";

    var result = await connection.QueryAsync<Student>(query);
            return result.ToList();
        }

        //public async Task<SchoolTerm?> GetCurrentTermAsync()
        //{
        //    using var connection = new SqlConnection(connectionString);
        //    string query = "SELECT TOP 1 TermID, Term, IsCurrentTerm FROM SchoolManagement.SchoolTerm WHERE IsCurrentTerm = 1";
        //    return await connection.QueryFirstOrDefaultAsync<SchoolTerm>(query);
        //}

        public async Task<int> InsertOtherFeeAsync(OtherFee fee)
        {
            using var connection = new SqlConnection(connectionString);
            string query = @"
        INSERT INTO SchoolManagement.OtherFees
        (FeeTypeName, Description, Amount, ClassID, UserID, RecDateCreated)
        VALUES (@FeeTypeName, @Description, @Amount, @ClassID, @UserID, GETDATE())";

            return await connection.ExecuteAsync(query, fee);
        }


        //  Insert new fee
        public async Task<int> AddOtherFeeAsync(OtherFee fee)
        {
            using var connection = new SqlConnection(connectionString);
            string query = @"
                INSERT INTO SchoolManagement.OtherFees 
                (FeeTypeName, Description, Amount, ClassID, UserID) 
                VALUES (@FeeTypeName, @Description, @Amount, @ClassID, @UserID)";

            return await connection.ExecuteAsync(query, fee);
        }

        //  Update existing fee
        public async Task<int> UpdateOtherFeeAsync(OtherFee fee, int userId)
        {
            using var connection = new SqlConnection(connectionString);
            string query = @"
        UPDATE SchoolManagement.OtherFees 
        SET FeeTypeName = @FeeTypeName,
            Description = @Description,
            Amount = @Amount,
            ClassID = @ClassID,
            EditBy = @EditBy,
            EditedOnRecDateCreated = GETDATE()
        WHERE FeeTypeID = @FeeTypeID";

            return await connection.ExecuteAsync(query, new
            {
                fee.FeeTypeID,
                fee.FeeTypeName,
                fee.Description,
                fee.Amount,
                fee.ClassID,
                EditBy = userId 
            });
        }


        //  Delete (soft delete) fee
        public async Task<int> DeleteOtherFeeAsync(int feeTypeId, int userId)
        {
            using var connection = new SqlConnection(connectionString);
            string query = @"
                UPDATE SchoolManagement.OtherFees 
                SET DeletedBy = @UserId,
                    DeletedOnRecDateCreated = GETDATE()
                WHERE FeeTypeID = @FeeTypeID";

            return await connection.ExecuteAsync(query, new { FeeTypeID = feeTypeId, UserId = userId });
        }


        /*other_fee_summery*/
        public async Task<List<FeeSummaryDto>> GetFeeSummaryAsync(DateTime? selectedDate, int? selectedTermId, int? selectedFeeTypeId)
        {
            using var connection = new SqlConnection(connectionString);
            string query = @"
        SELECT 
            s.ClassID,
            ofee.FeeTypeName,
            COUNT(DISTINCT s.StudentID) AS TotalStudents,
            ofee.Amount AS FeePerStudent,
            COUNT(DISTINCT s.StudentID) * ofee.Amount AS ExpectedAmount,
            ISNULL(SUM(p.AmountPaid), 0) AS CollectedAmount,
            (COUNT(DISTINCT s.StudentID) * ofee.Amount) - ISNULL(SUM(p.AmountPaid), 0) AS BalanceLeft
        FROM SchoolManagement.Students s
        JOIN SchoolManagement.OtherFees ofee
            ON s.ClassID = ofee.ClassID
        LEFT JOIN SchoolManagement.PaymentsOtherFees p
            ON p.StudentID = s.StudentID
           AND p.FeeTypeID = ofee.FeeTypeID
           AND (@SelectedDate IS NULL OR CAST(p.PaymentDate AS DATE) = @SelectedDate)
           AND (@SelectedTerm IS NULL OR p.TermID = @SelectedTerm)
        WHERE ofee.FeeTypeID = @SelectedFeeTypeID
        GROUP BY s.ClassID, ofee.FeeTypeName, ofee.Amount";

            return (await connection.QueryAsync<FeeSummaryDto>(query,
                new { SelectedDate = selectedDate, SelectedTerm = selectedTermId, SelectedFeeTypeID = selectedFeeTypeId })).ToList();
        }

        public async Task<List<OtherFee>> GetAllOtherFeesAsync()
        {
            using var conn = new SqlConnection(connectionString);
            await conn.OpenAsync();
            const string sql = "SELECT FeeTypeID, FeeTypeName, Amount, ClassID FROM SchoolManagement.OtherFees ORDER BY FeeTypeName";
            var result = await conn.QueryAsync<OtherFee>(sql);
            return result.ToList();
        }


    }
}
