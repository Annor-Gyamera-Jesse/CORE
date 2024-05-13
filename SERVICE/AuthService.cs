using Dapper;
using CORE.MODEL;
using Microsoft.Extensions.Configuration;
using System;
using System.Data;
using System.Security.Claims;
using System.Data.SqlClient;

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

                string query = "INSERT INTO SchoolManagement.Users (UserName, Password) VALUES (@UserName, @Password)";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@UserName", user.UserName);
                    command.Parameters.AddWithValue("@Password", user.Password);

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

        //----Assign Roles--------//

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

        public async Task<List<Role>> GetRolesAsync()
        {
            List<Role> roles = new List<Role>();

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();

                using (SqlCommand command = new SqlCommand("SELECT RoleID, RoleName FROM SchoolManagement.Roles", connection))
                using (SqlDataReader reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        roles.Add(new Role
                        {
                            RoleID = reader.GetInt32(0),
                            RoleName = reader.GetString(1)
                        });
                    }
                }
            }

            return roles;
        }

        public async Task SaveUserRolesAsync(int userID, string roleName, bool enable)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();

                using (SqlCommand command = new SqlCommand("INSERT INTO SchoolManagement.UserRoles (UserID, UserName, RoleName, Enable) VALUES (@UserID, @UserName, @RoleName, @Enable)", connection))
                {
                    command.Parameters.AddWithValue("@UserID", userID);
                    command.Parameters.AddWithValue("@UserName", "");
                    command.Parameters.AddWithValue("@RoleName", roleName);
                    command.Parameters.AddWithValue("@Enable", enable);

                    await command.ExecuteNonQueryAsync();
                }
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
                        "GuardianFullName, GuardianGender, GuardianHouseAddress, GuardianWorkAddress, GuardianEmail, GuardianFirstContact, GuardianSecondContact, EnableSwitch) " +
                                "VALUES (@FirstName, @LastName, @DateOfBirth, @Gender, @Address, @PhoneNumber, @Email, @ImageData, @ClassID," +
                                "@GuardianFullName, @GuardianGender, @GuardianHouseAddress, @GuardianWorkAddress, @GuardianEmail, @GuardianFirstContact, @GuardianSecondContact, @EnableSwitch); " +
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

                    var query = "SELECT TOP (1) StudentID, StudentFirstName, StudentLastName, StudentDateOfBirth, StudentGender, " +
             "StudentAddress, StudentPhoneNumber, StudentEmail, ImageData, ClassID, GuardianFullName, GuardianGender," +
             " GuardianHouseAddress, GuardianWorkAddress, GuardianEmail, GuardianFirstContact, GuardianSecondContact " +
             "FROM SchoolManagement.Students " +
             "WHERE CONCAT(StudentFirstName, ' ', StudentLastName) LIKE @StudentName";

                    // Remove the 'using' statement for the result variable
                    var result = await connection.QueryFirstOrDefaultAsync<Student>(query, new { StudentName = $"%{studentName}%" });

                    return result;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetStudentByNameAsync: {ex.Message}");
                // Handle the exception as needed (log, throw, etc.)
                throw; // Rethrow the exception after logging/handling if needed
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

        public async Task SaveAttendanceAsync(List<Student> students, string classID)
        {
            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    // Iterate through the list of students and save attendance for each student
                    foreach (var student in students)
                    {
                        var query = "INSERT INTO SchoolManagement.StudentsAttendance (StudentFirstName, StudentLastName, ClassID, EnableSwitch) " +
                                    "VALUES (@StudentFirstName, @StudentLastName, @ClassID, @EnableSwitch)";

                        var parameters = new
                        {
                            StudentFirstName = student.StudentFirstName,
                            StudentLastName = student.StudentLastName,
                            ClassID = classID,
                            EnableSwitch = student.EnableSwitch
                        };

                        await connection.ExecuteAsync(query, parameters);
                    }
                }
            }
            catch (SqlException ex)
            {
                Console.WriteLine($"SQL Exception: {ex.Message}");
                throw; // Re-throw the exception to propagate it up
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception: {ex.Message}");
                throw; // Re-throw the exception to propagate it up
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
                (TeacherFirstName, TeacherLastName, EnableSwitch, ClockIN, RecDateCreated)
                VALUES 
                (@TeacherFirstName, @TeacherLastName, @EnableSwitch, @ClockIN, @RecDateCreated)
            ";

                    using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@TeacherFirstName", attendanceRecord.TeacherFirstName);
                        command.Parameters.AddWithValue("@TeacherLastName", attendanceRecord.TeacherLastName);
                        command.Parameters.AddWithValue("@EnableSwitch", attendanceRecord.EnableSwitch);
                        command.Parameters.AddWithValue("@ClockIN", attendanceRecord.ClockIN);
                        //command.Parameters.AddWithValue("@ClockOUT", attendanceRecord.ClockOUT);
                        command.Parameters.AddWithValue("@RecDateCreated", DateTime.Now);

                        int rowsAffected = await command.ExecuteNonQueryAsync();
                        return rowsAffected > 0;
                    }
                }
            }
            catch (SqlException ex)
            {
                // Log SQL exceptions
                Console.WriteLine($"SQL Exception: {ex.Message}");
                return false;
            }
            catch (Exception ex)
            {
                // Log other exceptions
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
                    (TeacherFirstName, TeacherLastName, EnableSwitch, ClockIN, ClockOUT)
                    VALUES 
                    (@TeacherFirstName, @TeacherLastName, @EnableSwitch, @ClockIN, @ClockOUT)
                ";

                    using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@TeacherFirstName", attendanceRecord.TeacherFirstName);
                        command.Parameters.AddWithValue("@TeacherLastName", attendanceRecord.TeacherLastName);
                        command.Parameters.AddWithValue("@EnableSwitch", attendanceRecord.EnableSwitch);
                        command.Parameters.AddWithValue("@ClockIN", attendanceRecord.ClockIN);
                        command.Parameters.AddWithValue("@ClockOUT", attendanceRecord.ClockOUT);

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

        //----for submitting lesson note--//

        public async Task<int> SubmitLessonNoteAsync(LessonNote lessonNote)
        {
            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    var query = @"INSERT INTO SchoolManagement.lessonnotes (UserId, SchoolCourse, Topic, OBJECTIVES, TLMTLA, INTRODUCTION, COREPOINTS, EVALUATIONREMARKS) 
                          VALUES (@UserId, @SchoolCourse, @Topic, @Objectives, @TLMTLA, @Introduction, @CorePoints, @EvaluationRemarks)";

                    return await connection.ExecuteAsync(query, lessonNote);
                }
            }
            catch (SqlException ex)
            {
                // Log SQL-related exceptions
                Console.WriteLine($"SQL Exception in SubmitLessonNoteAsync: {ex}");
                throw; // Re-throw the exception
            }
            catch (Exception ex)
            {
                // Log other types of exceptions
                Console.WriteLine($"Exception in SubmitLessonNoteAsync: {ex}");
                throw; // Re-throw the exception
            }
        }

    }
}
