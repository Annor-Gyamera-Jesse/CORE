
using Dapper;
using CORE.MODEL;
using System.Data.SqlClient;
using CORE.Pages.LESSON_NOTE;
using CORE.SERVICE.MainLayout.Module;
using static CORE.MODEL.Teachers_Time_Table;
using static CORE.Pages.COURSES.View_Teacher_Subject_Assign_ByID;
using System.Data;

namespace CORE.SERVICE
{
    // AuthService.cs
    public class AuthService
    {
        private readonly string connectionString;

        public AuthService(string connectionString)
        {
            this.connectionString = connectionString;
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
            using (var connection = new SqlConnection(connectionString))
            {
                var userRoleAndMenuAccess = new UserRoleAndMenuAccess();

                // Get user role
                userRoleAndMenuAccess.Role = await connection.QuerySingleAsync<string>(
                    "SELECT r.RoleName FROM SchoolManagementSecurity.MainSystemRoles r " +
                    "JOIN SchoolManagement.Users u ON r.RoleID = u.RoleID WHERE u.UserID = @UserID",
                    new { UserID = userId });

                // Get menu items accessible to this role
                userRoleAndMenuAccess.MenuItems = (await connection.QueryAsync<MenuItem>(
                    "SELECT m.* FROM SchoolManagementSecurity.MainMenu m " +
                    "JOIN SchoolManagementSecurity.MenuAccess a ON m.MenuID = a.MenuID " +
                    "WHERE a.RoleID = (SELECT RoleID FROM SchoolManagement.Users WHERE UserID = @UserID) AND a.CanAccess = 1",
                    new { UserID = userId })).ToList();

                return userRoleAndMenuAccess;
            }
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


        //--- For company LoginLayout display--///
        public async Task<string> GetSoftwareVersionAsync()
        {
            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "SELECT SoftWareVerssion FROM SchoolManagement.LoginScreenDetails";
                    return (string)await command.ExecuteScalarAsync();
                }
            }
        }

        public int GetCompanyRegisteredYear()
        {
            // Use current year as company registered date
            return DateTime.Now.Year;
        }

        public async Task<(string, string)> GetLoginScreenDetailsAsync()
        {
            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "SELECT SchoolName, CompanyRegisteredName FROM SchoolManagement.LoginScreenDetails";
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            string schoolName = reader.GetString(reader.GetOrdinal("SchoolName"));
                            string companyRegisteredName = reader.GetString(reader.GetOrdinal("CompanyRegisteredName"));
                            return (schoolName, companyRegisteredName);
                        }
                    }
                }
            }
            return (null, null); // Return null if no data found
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
                            Password = reader.GetString(2)
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

            return users;
        }

          // Method to get all users
        public async Task<IEnumerable<User>> GetMUsersAsync()
        {
            using (var connection = new SqlConnection(connectionString))
            {
                return await connection.QueryAsync<User>("SELECT UserID, UserName FROM SchoolManagement.Users");
            }
        }

        // Method to get all roles
        public async Task<IEnumerable<Role>> GetRolesAsync()
        {
            using (var connection = new SqlConnection(connectionString))
            {
                return await connection.QueryAsync<Role>("SELECT RoleName FROM SchoolManagement.Roles");
            }
        }

        // Method to get all menu items
        public async Task<IEnumerable<MobileMenuItem>> GetMenuItemsAsync()
        {
            using (var connection = new SqlConnection(connectionString))
            {
                return await connection.QueryAsync<MobileMenuItem>("SELECT CategoryName, MenuItem AS ItemName FROM SchoolManagement.MobileAppMenuDisplay");
            }
        }

        // Method to get distinct category names
        public async Task<IEnumerable<string>> GetCategoryNamesAsync()
        {
            using (var connection = new SqlConnection(connectionString))
            {
                return await connection.QueryAsync<string>(
                    "SELECT DISTINCT CategoryName FROM SchoolManagement.MobileAppMenuDisplay");
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
            using (var connection = new SqlConnection(connectionString))
            {
                return await connection.QueryAsync<MobileAppRole>(
                    "SELECT * FROM SchoolManagement.MobileAppRoles WHERE UserID = @UserID",
                    new { UserID = userId });
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
                    string.IsNullOrEmpty(student.StudentLastName) ||
                    student.StudentDateOfBirth == null)
                {
                    throw new ArgumentException("StudentFirstName, StudentLastName, and StudentDateOfBirth are required fields.");
                }

                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    var query = "INSERT INTO SchoolManagement.Students (StudentFirstName, StudentLastName, StudentDateOfBirth, StudentGender, StudentAddress, StudentPhoneNumber, StudentEmail, ImageData, ClassID, " +
                        "GuardianFullName, GuardianGender, GuardianHouseAddress, GuardianWorkAddress, GuardianEmail, GuardianFirstContact, GuardianSecondContact, EnableSwitch, StudentMedicalReport) " +
                                "VALUES (@FirstName, @LastName, @DateOfBirth, @Gender, @Address, @PhoneNumber, @Email, @ImageData, @ClassID," +
                                "@GuardianFullName, @GuardianGender, @GuardianHouseAddress, @GuardianWorkAddress, @GuardianEmail, @GuardianFirstContact, @GuardianSecondContact, @EnableSwitch, @StudentMedicalReport); " +
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
                        command.Parameters.Add("@ImageData", SqlDbType.VarBinary).Value = student.ImageData ?? (object)DBNull.Value;
                        // Add parameter for ClassID
                        command.Parameters.AddWithValue("@ClassID", student.ClassID);
                        command.Parameters.AddWithValue("@GuardianFullName", student.GuardianFullName);
                        command.Parameters.AddWithValue("@GuardianGender", student.GuardianGender);
                        command.Parameters.AddWithValue("@GuardianHouseAddress", student.GuardianHouseAddress);
                        command.Parameters.AddWithValue("@GuardianWorkAddress", student.GuardianWorkAddress);
                        command.Parameters.AddWithValue("@GuardianEmail", student.GuardianEmail);
                        command.Parameters.AddWithValue("@GuardianFirstContact", student.GuardianFirstContact);
                        command.Parameters.AddWithValue("@GuardianSecondContact", student.GuardianSecondContact);
                        command.Parameters.AddWithValue("@EnableSwitch", student.EnableSwitch);
                        command.Parameters.AddWithValue("@StudentMedicalReport", student.StudentMedicalReport ?? (object)DBNull.Value);

                        // ExecuteScalarAsync returns the identity of the new record (StudentID)
                        var result = await command.ExecuteScalarAsync();

                        // Check if the insertion was successful
                        return result != null ? Convert.ToInt32(result) : 0;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in AddStudentAsync: {ex.Message}");
                // Handle the exception as needed (log, throw, etc.)
                throw; // Rethrow the exception after logging/handling if needed
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

                    var query = "INSERT INTO SchoolManagement.Teacher (TeacherFirstName, TeacherLastName, TeacherDateOfBirth, TeacherGender, TeacherAddress, TeacherPhoneNumber, TeacherEmail, ImageData) " +
                                "VALUES (@FirstName, @LastName, @DateOfBirth, @Gender, @Address, @PhoneNumber, @Email, @ImageData); " +
                                "SELECT SCOPE_IDENTITY();";

                    using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@FirstName", teachersRegistration.TeacherFirstName);
                        command.Parameters.AddWithValue("@LastName", teachersRegistration.TeacherLastName);
                        command.Parameters.AddWithValue("@DateOfBirth", teachersRegistration.TeacherDateOfBirth);
                        command.Parameters.AddWithValue("@Gender", teachersRegistration.TeacherGender);
                        command.Parameters.AddWithValue("@Address", teachersRegistration.TeacherAddress);
                        command.Parameters.AddWithValue("@PhoneNumber", teachersRegistration.TeacherPhoneNumber);
                        command.Parameters.AddWithValue("@Email", teachersRegistration.TeacherEmail);

                        // Add parameter for ImageData
                        command.Parameters.AddWithValue("@ImageData", teachersRegistration.ImageData ?? (object)DBNull.Value);

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

                            //                             var schoolCourse = new SchoolCourse();

                            //                             // Check for null values before retrieving
                            //                             schoolCourse.SCID = reader.IsDBNull(0) ? null : reader.GetString(0);
                            //                             schoolCourse.SchoolCourseName = reader.IsDBNull(1) ? null : reader.GetString(1);


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
                        // Check for null and DBNull.Value for nullable properties
                        command.Parameters.AddWithValue("@SCID", (object)schoolCourse.SCID ?? DBNull.Value);
                        command.Parameters.AddWithValue("@SchoolCourse", (object)schoolCourse.SchoolCourseName ?? DBNull.Value);

                        return await command.ExecuteNonQueryAsync();
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

                        return await command.ExecuteNonQueryAsync();
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

                        return await command.ExecuteNonQueryAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                // Use a logging framework or log to a file instead of Console.WriteLine
                Console.WriteLine($"Error in DeleteSchoolCourseAsync: {ex.Message}");
                throw;
            }
        }



        /////////////////////////////////////////////////////////////////////////////////////
        ///

        //Assign Teachers 
        public async Task<List<TeachersRegistration>> GetAllTeachersAsync()
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

                    // Insert data into the Classes table with a fixed class name
                    var query = "INSERT INTO SchoolManagement.Classes (ClassID, ClassName, TeacherFirstName, TeacherLastName, TeacherPhoneNumber) VALUES (@ClassId, 'DefaultClass', @TeacherFirstName, @TeacherLastName, @TeacherPhoneNumber)";
                    var parameters = new DynamicParameters();
                    parameters.Add("@ClassId", classId);
                    parameters.Add("@TeacherFirstName", teacher.TeacherFirstName);
                    parameters.Add("@TeacherLastName", teacher.TeacherLastName);
                    parameters.Add("@TeacherPhoneNumber", teacher.TeacherPhoneNumber);

                    var result = await connection.ExecuteAsync(query, parameters);

                    return result;
                }
            }
            catch (Exception ex)
            {
                // Use a logging framework to log the error
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
                Console.WriteLine($"Error in GetAllClassesAsync: {ex.Message}");
                throw;
            }
        }

        //TeacherAssignClassService
        public async Task<List<TeachersAssignClasses>> GetAllClassesAsync()
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
                return null; // Handle the exception as needed
            }
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

                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in DeleteClassAsync: {ex.Message}");
                return false; // Handle the exception as needed
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
        }

        public async Task SaveAttendanceAsync(List<Student> students, string classID, int userID)
        {
            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    foreach (var student in students)
                    {
                        var query = "INSERT INTO SchoolManagement.StudentsAttendance (StudentFirstName, StudentLastName, ClassID, EnableSwitch, UserID) " +
                                    "VALUES (@StudentFirstName, @StudentLastName, @ClassID, @EnableSwitch, @UserID)";

                        var parameters = new
                        {
                            StudentFirstName = student.StudentFirstName,
                            StudentLastName = student.StudentLastName,
                            ClassID = classID,
                            EnableSwitch = student.EnableSwitch,
                            UserID = userID
                        };

                        await connection.ExecuteAsync(query, parameters);
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
                    (TeacherFirstName, TeacherLastName, EnableSwitch, ClockIN, ClockOUT, UserID)
                    VALUES 
                    (@TeacherFirstName, @TeacherLastName, @EnableSwitch, @ClockIN, @ClockOUT, @UserID)
                ";

                    using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@TeacherFirstName", attendanceRecord.TeacherFirstName);
                        command.Parameters.AddWithValue("@TeacherLastName", attendanceRecord.TeacherLastName);
                        command.Parameters.AddWithValue("@EnableSwitch", attendanceRecord.EnableSwitch);
                        command.Parameters.AddWithValue("@ClockIN", attendanceRecord.ClockIN);
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

        // Method to get students by class
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

        public async Task<int> InsertExamAsync(Exam exam)
        {
            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    var query = @"INSERT INTO SchoolManagement.SchoolExams 
                          (StudentName, ClassName, AcademicYear, VacationDate, PromotedTo, NumberOnRoll, 
                           Term, Position, NextTermsBegins, AttendanceOut, AttendanceIn, SchoolCourse, ClassScore, 
                           ExamsScore, TotalScore, SubjectsPositions, Grade, TeachersRemarks, Conduct, HeadmasterRemark, 
                           SchoolInformation, TeachersSignature, HeadMasterSignature) 
                          VALUES 
                          (@StudentName, @ClassName, @AcademicYear, @VacationDate, @PromotedTo, @NumberOnRoll, 
                           @Term, @Position, @NextTermsBegins, @AttendanceOut, @AttendanceIn, @SchoolCourse, @ClassScore, 
                           @ExamsScore, @TotalScore, @SubjectsPositions, @Grade, @TeachersRemarks, @Conduct, @HeadmasterRemark, 
                           @SchoolInformation, @TeachersSignature, @HeadMasterSignature);
                          SELECT SCOPE_IDENTITY();";

                    return await connection.ExecuteScalarAsync<int>(query, exam);
                }
            }
            catch (SqlException ex)
            {
                // Log SQL exceptions
                Console.WriteLine($"SQL Exception: {ex.Message}");
                throw; // Re-throw the exception for better debugging
            }
            catch (Exception ex)
            {
                // Log other exceptions
                Console.WriteLine($"Exception: {ex.Message}");
                throw; // Re-throw the exception for better debugging
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

        //-------Display All Students------//
        public async Task<List<Student>> GetAllStudents()
        {
            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    var query = "SELECT * FROM SchoolManagement.Students";
                    var result = await connection.QueryAsync<Student>(query);

                    return result.AsList();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetAllTeachersAsync: {ex.Message}");
                throw;
            }
        }

        //-----Total Number of Students displayed in the Card---//
        public async Task<int> GetTotalStudentsCount()
        {
            int totalStudents = 0;

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                string query = "SELECT COUNT(*) FROM SchoolManagement.Students";
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

                    var query = @"INSERT INTO SchoolManagement.lessonnotes 
                        (UserId, SchoolCourse, Topic, OBJECTIVES, TLMTLA, INTRODUCTION, COREPOINTS, EVALUATIONREMARKS) 
                        VALUES (@UserId, @SchoolCourse, @Topic, @OBJECTIVES, @TLMTLA, @INTRODUCTION, @COREPOINTS, @EVALUATIONREMARKS)";

                    return await connection.ExecuteAsync(query, new
                    {
                        lessonNote.UserId,
                        lessonNote.SchoolCourse,
                        lessonNote.Topic,
                        lessonNote.OBJECTIVES,
                        lessonNote.TLMTLA,
                        lessonNote.INTRODUCTION,
                        lessonNote.COREPOINTS,
                        lessonNote.EVALUATIONREMARKS
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
        public async Task<IEnumerable<LessonNote>> GetSubmittedLessonNotesAsync()
        {
            using (var connection = new SqlConnection(connectionString))
            {
                string query = @"
                SELECT ln.LessonnotesID, ln.UserId, u.UserName, ln.SchoolCourse, ln.Topic, ln.OBJECTIVES, ln.TLMTLA, ln.INTRODUCTION, ln.COREPOINTS, ln.EVALUATIONREMARKS, ln.Status
                FROM SchoolManagement.lessonnotes ln
                JOIN SchoolManagement.Users u ON ln.UserId = u.UserID";

                var lessonNotes = await connection.QueryAsync<LessonNote>(query);
                return lessonNotes;
            }
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
            INSERT INTO SchoolManagement.TeachersAssesment (StudentName, ClassID, TEST1, TEST2, GROUPWORK, HOMEWORK, CLASSTEST, TOTAL_X, EXAMS_SCORE, Y, X_Y, POSITION, UserID)
            VALUES (@StudentName, @ClassID, @TEST1, @TEST2, @GROUPWORK, @HOMEWORK, @CLASSTEST, @TOTAL_X, @EXAMS_SCORE, @Y, @X_Y, @POSITION, @UserID)";

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
         (SCID, ClassID, SubjectStartTime, SubjectEndTime, DayID) 
     VALUES 
         (@SCID, @ClassID, @SubjectStartTime, @SubjectEndTime, @DayID)";

                await connection.ExecuteAsync(query, new
                {
                    schedule.SCID,
                    schedule.ClassID,
                    schedule.SubjectStartTime,
                    schedule.SubjectEndTime,
                    schedule.DayID
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
     DayID = @DayID
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
        // Method to add a new notice
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
        //-------------------------------------------------------------------------//
    }
}