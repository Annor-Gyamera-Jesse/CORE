using CORE.MODEL;
using CORE.MODEL.Bank.Transaction_Logs;
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
                @"SELECT TOP 50 * 
                  FROM SchoolManagement.SyncLog 
                  WHERE SyncStatus = 'Pending' AND RetryCount < MaxRetry 
                  ORDER BY CreatedAt");

            foreach (var log in logs)
            {
                try
                {
                    switch (log.TableName)
                    {
                        case "Students":
                            await SyncStudent(localConn, remoteConn, log);
                            break;

                        case "StudentFees":
                            await SyncStudentFee(localConn, remoteConn, log);
                            break;

                        case "StudentDiscounts":
                            await SyncStudentDiscount(localConn, remoteConn, log);
                            break;

                        case "BankTransactionLog":
                            await SyncBankTransactionLog(localConn, remoteConn, log);
                            break;
                        case "FeeTypes":
                            await SyncFeeType(localConn, remoteConn, log);
                            break;                      


                    }


                    // Mark success
                    await localConn.ExecuteAsync(
                        "UPDATE SchoolManagement.SyncLog SET SyncStatus='Success', LastTriedAt=GETDATE() WHERE SyncID=@id",
                        new { id = log.SyncID });
                }
                catch (Exception ex)
                {
                    // Log failure + increment retry
                    await localConn.ExecuteAsync(
                        @"UPDATE SchoolManagement.SyncLog 
                          SET RetryCount=RetryCount+1, LastTriedAt=GETDATE(), ErrorMessage=@err 
                          WHERE SyncID=@id",
                        new { id = log.SyncID, err = ex.Message });
                }
            }
        }

        private static async Task SyncStudent(SqlConnection localConn, SqlConnection remoteConn, SyncLog log)
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

        private static async Task SyncStudentFee(SqlConnection localConn, SqlConnection remoteConn, SyncLog log)
        {
            if (log.ActionType == "INSERT")
            {
                var fee = await localConn.QuerySingleAsync<StudentFee>(
                    "SELECT * FROM SchoolManagement.StudentFees WHERE FeeID = @id",
                    new { id = log.RecordID });

                await remoteConn.ExecuteAsync(@"
                    INSERT INTO SchoolManagement.StudentFees
                    (StudentID, FeeTypeID, StudentName, FeeTypeName, ClassID, AmountPaid, AmountLeft, PaymentDate, Note, UserID, PaymentMethod, TermID)
                    VALUES (@StudentID, @FeeTypeID, @StudentName, @FeeTypeName, @ClassID, @AmountPaid, @AmountLeft, @PaymentDate, @Note, @UserID, @PaymentMethod, @TermID)",
                    fee);
            }
            else if (log.ActionType == "UPDATE")
            {
                var fee = await localConn.QuerySingleAsync<StudentFee>(
                    "SELECT * FROM SchoolManagement.StudentFees WHERE FeeID = @id",
                    new { id = log.RecordID });

                await remoteConn.ExecuteAsync(@"
                    UPDATE SchoolManagement.StudentFees SET
                        StudentID=@StudentID,
                        FeeTypeID=@FeeTypeID,
                        StudentName=@StudentName,
                        FeeTypeName=@FeeTypeName,
                        ClassID=@ClassID,
                        AmountPaid=@AmountPaid,
                        AmountLeft=@AmountLeft,
                        PaymentDate=@PaymentDate,
                        Note=@Note,
                        UserID=@UserID,
                        PaymentMethod=@PaymentMethod,
                        TermID=@TermID
                    WHERE FeeID=@FeeID",
                    fee);
            }
            else if (log.ActionType == "DELETE")
            {
                await remoteConn.ExecuteAsync(
                    "DELETE FROM SchoolManagement.StudentFees WHERE FeeID=@id",
                    new { id = log.RecordID });
            }
        }

        private static async Task SyncStudentDiscount(SqlConnection localConn, SqlConnection remoteConn, SyncLog log)
        {
            if (log.ActionType == "INSERT")
            {
                var discount = await localConn.QuerySingleAsync<StudentDiscount>(
                    "SELECT * FROM SchoolManagement.StudentDiscounts WHERE DiscountID = @id",
                    new { id = log.RecordID });

                await remoteConn.ExecuteAsync(@"
                    INSERT INTO SchoolManagement.StudentDiscounts
                    (StudentID, GuardianFullName, DiscountType, DiscountValue, IsActive, CreatedDate, UserID)
                    VALUES (@StudentID, @GuardianFullName, @DiscountType, @DiscountValue, @IsActive, @CreatedDate, @UserID)",
                    discount);
            }
            else if (log.ActionType == "UPDATE")
            {
                var discount = await localConn.QuerySingleAsync<StudentDiscount>(
                    "SELECT * FROM SchoolManagement.StudentDiscounts WHERE DiscountID = @id",
                    new { id = log.RecordID });

                await remoteConn.ExecuteAsync(@"
                    UPDATE SchoolManagement.StudentDiscounts SET
                        StudentID=@StudentID,
                        GuardianFullName=@GuardianFullName,
                        DiscountType=@DiscountType,
                        DiscountValue=@DiscountValue,
                        IsActive=@IsActive,
                        CreatedDate=@CreatedDate,
                        UserID=@UserID
                    WHERE DiscountID=@DiscountID",
                    discount);
            }
            else if (log.ActionType == "DELETE")
            {
                await remoteConn.ExecuteAsync(
                    "DELETE FROM SchoolManagement.StudentDiscounts WHERE DiscountID=@id",
                    new { id = log.RecordID });
            }
        }

        private static async Task SyncBankTransactionLog(SqlConnection localConn, SqlConnection remoteConn, SyncLog log)
        {
            if (log.ActionType == "INSERT")
            {
                var bankTx = await localConn.QuerySingleAsync<BankTransactionLog>(
                    "SELECT * FROM SchoolManagement.BankTransactionLog WHERE TransactionID = @id",
                    new { id = log.RecordID });

                await remoteConn.ExecuteAsync(@"
            INSERT INTO SchoolManagement.BankTransactionLog
                (BankID, TransactionType, Amount, Status, ErrorMessage, CreatedAt)
            VALUES
                (@BankID, @TransactionType, @Amount, @Status, @ErrorMessage, @CreatedAt)",
                    bankTx);
            }
            else if (log.ActionType == "UPDATE")
            {
                var bankTx = await localConn.QuerySingleAsync<BankTransactionLog>(
                    "SELECT * FROM SchoolManagement.BankTransactionLog WHERE TransactionID = @id",
                    new { id = log.RecordID });

                await remoteConn.ExecuteAsync(@"
            UPDATE SchoolManagement.BankTransactionLog SET
                BankID=@BankID,
                TransactionType=@TransactionType,
                Amount=@Amount,
                Status=@Status,
                ErrorMessage=@ErrorMessage,
                CreatedAt=@CreatedAt
            WHERE TransactionID=@TransactionID",
                    bankTx);
            }
            else if (log.ActionType == "DELETE")
            {
                await remoteConn.ExecuteAsync(
                    "DELETE FROM SchoolManagement.BankTransactionLog WHERE TransactionID=@id",
                    new { id = log.RecordID });
            }
        }

        private static async Task SyncFeeType(SqlConnection localConn, SqlConnection remoteConn, SyncLog log)
        {
            var feeType = await localConn.QuerySingleAsync<FeeType>(
                "SELECT * FROM SchoolManagement.FeeTypes WHERE FeeTypeID = @id",
                new { id = log.RecordID });

            if (log.ActionType == "INSERT")
            {
                await remoteConn.ExecuteAsync(@"
            INSERT INTO SchoolManagement.FeeTypes
            (FeeTypeName, Description, Amount, ClassID, RecDateCreated, UserID)
            VALUES (@FeeTypeName, @Description, @Amount, @ClassID, @RecDateCreated, @UserID)",
                    feeType);
            }
            else if (log.ActionType == "UPDATE")
            {
                await remoteConn.ExecuteAsync(@"
            UPDATE SchoolManagement.FeeTypes SET
                FeeTypeName=@FeeTypeName,
                Description=@Description,
                Amount=@Amount,
                ClassID=@ClassID,
                RecDateCreated=@RecDateCreated,
                UserID=@UserID
            WHERE FeeTypeID=@FeeTypeID",
                    feeType);
            }
            else if (log.ActionType == "DELETE")
            {
                await remoteConn.ExecuteAsync(
                    "DELETE FROM SchoolManagement.FeeTypes WHERE FeeTypeID=@id",
                    new { id = log.RecordID });
            }
        }

    }
}
