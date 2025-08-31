using CORE.MODEL;
using CORE.MODEL.OFFLINE_MODEL;
using Dapper;
using System.Data.SqlClient;


namespace CORE.SERVICE.OFFLINE_SERVICE
{
    public class SyncService
    {
        private readonly string localConnectionString;
        private readonly string remoteConnectionString;

        public SyncService(IConfiguration configuration)
        {
            localConnectionString = configuration.GetConnectionString("DefaultConnection");
            remoteConnectionString = configuration.GetConnectionString("RemoteConnection");
        }

        public async Task SyncToRemote()
        {
            using var localConn = new SqlConnection(localConnectionString);
            using var remoteConn = new SqlConnection(remoteConnectionString);

            await localConn.OpenAsync();
            await remoteConn.OpenAsync();

            var logs = await localConn.QueryAsync<SyncLog>(
                "SELECT TOP 50 * FROM SchoolManagement.SyncLog WHERE SyncStatus = 'Pending' AND RetryCount < MaxRetry ORDER BY CreatedAt");

            foreach (var log in logs)
            {
                try
                {
                    if (log.TableName == "Students")
                    {
                        if (log.ActionType == "INSERT")
                        {
                            var student = await localConn.QuerySingleAsync<Student>(
                                "SELECT * FROM SchoolManagement.Students WHERE StudentID = @id",
                                new { id = log.RecordID });

                            await remoteConn.ExecuteAsync(@"
                            INSERT INTO SchoolManagement.Students
                            (StudentFirstName, StudentLastName, StudentDateOfBirth, StudentGender, StudentAddress, StudentPhoneNumber, StudentEmail, ClassID)
                            VALUES (@StudentFirstName, @StudentLastName, @StudentDateOfBirth, @StudentGender, @StudentAddress, @StudentPhoneNumber, @StudentEmail, @ClassID)",
                                student);
                        }
                        else if (log.ActionType == "UPDATE")
                        {
                            var student = await localConn.QuerySingleAsync<Student>(
                                "SELECT * FROM SchoolManagement.Students WHERE StudentID = @id",
                                new { id = log.RecordID });

                            await remoteConn.ExecuteAsync(@"
                            UPDATE SchoolManagement.Students SET
                            StudentFirstName=@StudentFirstName,
                            StudentLastName=@StudentLastName,
                            StudentDateOfBirth=@StudentDateOfBirth,
                            StudentGender=@StudentGender,
                            StudentAddress=@StudentAddress,
                            StudentPhoneNumber=@StudentPhoneNumber,
                            StudentEmail=@StudentEmail,
                            ClassID=@ClassID
                            WHERE StudentID=@StudentID",
                                student);
                        }
                        else if (log.ActionType == "DELETE")
                        {
                            await remoteConn.ExecuteAsync(
                                "DELETE FROM SchoolManagement.Students WHERE StudentID=@id",
                                new { id = log.RecordID });
                        }
                    }

                    // Mark as success
                    await localConn.ExecuteAsync(
                        "UPDATE SchoolManagement.SyncLog SET SyncStatus='Success', LastTriedAt=GETDATE() WHERE SyncID=@id",
                        new { id = log.SyncID });
                }
                catch (Exception ex)
                {
                    // X Log error + increment retry
                    await localConn.ExecuteAsync(
                        "UPDATE SchoolManagement.SyncLog SET RetryCount=RetryCount+1, LastTriedAt=GETDATE(), ErrorMessage=@err WHERE SyncID=@id",
                        new { id = log.SyncID, err = ex.Message });
                }
            }
        }
    }
}
