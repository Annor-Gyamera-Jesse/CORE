using CORE.MODEL;
using CORE.MODEL.Bank;
using CORE.MODEL.Bank.Transaction_Logs;
using CORE.MODEL.Expenses;
using CORE.MODEL.FEEDING_FEE;
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
                        case "Bank":
                            await SyncBank(localConn, remoteConn, log);
                            break;
                        case "ExpenseCategories":
                            await SyncExpenseCategory(localConn, remoteConn, log);
                            break;
                        case "PaymentMethods":
                            await SyncPaymentMethod(localConn, remoteConn, log);
                            break;
                        //case "BankTransactionLog":
                        //    await SyncBankTransaction(localConn, remoteConn, log);
                        //    break;
                        case "Expenses":
                            await SyncExpense(localConn, remoteConn, log);
                            break;
                        case "Staff":
                            await SyncStaffStatus(localConn, remoteConn, log);
                            break;
                        case "PaymentsOtherFees":
                            await SyncPaymentsOtherFee(localConn, remoteConn, log);
                            break;
                        case "OtherFees":
                            await SyncOtherFee(localConn, remoteConn, log);
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


        private static async Task SyncStaffStatus(SqlConnection localConn, SqlConnection remoteConn, SyncLog log)
        {
            if (log.ActionType == "UPDATE")
            {
                // Fetch into an anonymous type instead of dynamic
                var staff = await localConn.QuerySingleAsync<(int StaffID, string EmploymentStatus)>(
                    "SELECT StaffID, EmploymentStatus FROM SchoolManagement.Staff WHERE StaffID = @id",
                    new { id = log.RecordID });

                // Create a concrete anonymous object for ExecuteAsync
                var parameters = new
                {
                    StaffID = staff.StaffID,
                    EmploymentStatus = staff.EmploymentStatus
                };

                await remoteConn.ExecuteAsync(@"
            UPDATE SchoolManagement.Staff SET
                EmploymentStatus = @EmploymentStatus
            WHERE StaffID = @StaffID",
                    parameters);
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
        //this is the reason why i wrote the cautious because i dont really know it will go to the cloud
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
            var fee = await localConn.QuerySingleAsync<FeeType>(
                "SELECT * FROM SchoolManagement.FeeTypes WHERE FeeTypeID = @id",
                new { id = log.RecordID });

            if (log.ActionType == "INSERT")
            {
                await remoteConn.ExecuteAsync(@"
            INSERT INTO SchoolManagement.FeeTypes
            (FeeTypeName, Description, Amount, ClassID, RecDateCreated, UserID)
            VALUES (@FeeTypeName, @Description, @Amount, @ClassID, @RecDateCreated, @UserID)",
                    fee);
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
                UserID=@UserID,
                EditedOnRecDateCreated=@EditedOnRecDateCreated,
                EditBy=@EditBy
            WHERE FeeTypeID=@FeeTypeID",
                    fee);
            }
            else if (log.ActionType == "DELETE")
            {
                await remoteConn.ExecuteAsync(@"
            UPDATE SchoolManagement.FeeTypes SET
                DeletedBy=@DeletedBy,
                DeletedOnRecDateCreated=@DeletedOnRecDateCreated
            WHERE FeeTypeID=@FeeTypeID",
                    fee);
            }
        }


        //private static async Task SyncBank(SqlConnection localConn, SqlConnection remoteConn, SyncLog log)
        //{
        //    if (log.ActionType == "UPDATE")
        //    {
        //        var bank = await localConn.QuerySingleAsync<BankTransfer>(
        //            "SELECT * FROM SchoolManagement.Bank WHERE BankID = @id",
        //            new { id = log.RecordID });

        //        await remoteConn.ExecuteAsync(@"
        //    UPDATE SchoolManagement.Bank
        //    SET AmountTransferred = @AmountTransferred
        //    WHERE BankID = @BankID",
        //            bank);
        //    }
        //}


        private static async Task SyncExpenseCategory(SqlConnection localConn, SqlConnection remoteConn, SyncLog log)
        {
            if (log.ActionType == "INSERT")
            {
                var category = await localConn.QuerySingleAsync<ExpenseCategory>(
                    "SELECT * FROM SchoolManagement.ExpenseCategories WHERE CategoryID = @id",
                    new { id = log.RecordID });

                await remoteConn.ExecuteAsync(@"
            INSERT INTO SchoolManagement.ExpenseCategories
            (CategoryName, Description, UserID)
            VALUES (@CategoryName, @Description, @UserID)",
                    category);
            }
            else if (log.ActionType == "UPDATE")
            {
                var category = await localConn.QuerySingleAsync<ExpenseCategory>(
                    "SELECT * FROM SchoolManagement.ExpenseCategories WHERE CategoryID = @id",
                    new { id = log.RecordID });

                await remoteConn.ExecuteAsync(@"
            UPDATE SchoolManagement.ExpenseCategories
            SET CategoryName=@CategoryName,
                Description=@Description,
                UserID=@UserID
            WHERE CategoryID=@CategoryID",
                    category);
            }
            else if (log.ActionType == "DELETE")
            {
                await remoteConn.ExecuteAsync(
                    "DELETE FROM SchoolManagement.ExpenseCategories WHERE CategoryID=@id",
                    new { id = log.RecordID });
            }
        }

        private static async Task SyncPaymentMethod(SqlConnection localConn, SqlConnection remoteConn, SyncLog log)
        {
            if (log.ActionType == "INSERT")
            {
                var method = await localConn.QuerySingleAsync<PaymentMethods>(
                    "SELECT * FROM SchoolManagement.PaymentMethods WHERE PaymentMethodID = @id",
                    new { id = log.RecordID });

                await remoteConn.ExecuteAsync(@"
            INSERT INTO SchoolManagement.PaymentMethods
            (MethodName, Description, UserID, BankNumber)
            VALUES (@MethodName, @Description, @UserID, @BankNumber)",
                    method);
            }
            else if (log.ActionType == "UPDATE")
            {
                var method = await localConn.QuerySingleAsync<PaymentMethods>(
                    "SELECT * FROM SchoolManagement.PaymentMethods WHERE PaymentMethodID = @id",
                    new { id = log.RecordID });

                await remoteConn.ExecuteAsync(@"
            UPDATE SchoolManagement.PaymentMethods
            SET MethodName = @MethodName,
                Description = @Description,
                BankNumber = @BankNumber
            WHERE PaymentMethodID = @PaymentMethodID",
                    method);
            }
            else if (log.ActionType == "DELETE")
            {
                await remoteConn.ExecuteAsync(
                    "DELETE FROM SchoolManagement.PaymentMethods WHERE PaymentMethodID=@id",
                    new { id = log.RecordID });
            }
        }

        private static async Task SyncBankTransaction(SqlConnection localConn, SqlConnection remoteConn, SyncLog log)
        {
            if (log.ActionType == "INSERT")
            {
                var transaction = await localConn.QuerySingleAsync<BankTransactionLog>(
                    "SELECT * FROM SchoolManagement.BankTransactionLog WHERE BankTransactionID = @id",
                    new { id = log.RecordID });

                await remoteConn.ExecuteAsync(@"
            INSERT INTO SchoolManagement.BankTransactionLog
            (BankID, TransactionType, Amount, Status, ErrorMessage, CreatedAt)
            VALUES (@BankID, @TransactionType, @Amount, @Status, @ErrorMessage, @CreatedAt)",
                    transaction);
            }
            else if (log.ActionType == "UPDATE")
            {
                var transaction = await localConn.QuerySingleAsync<BankTransactionLog>(
                    "SELECT * FROM SchoolManagement.BankTransactionLog WHERE BankTransactionID = @id",
                    new { id = log.RecordID });

                await remoteConn.ExecuteAsync(@"
            UPDATE SchoolManagement.BankTransactionLog SET
                BankID=@BankID,
                TransactionType=@TransactionType,
                Amount=@Amount,
                Status=@Status,
                ErrorMessage=@ErrorMessage,
                CreatedAt=@CreatedAt
            WHERE BankTransactionID=@BankTransactionID",
                    transaction);
            }
            else if (log.ActionType == "DELETE")
            {
                await remoteConn.ExecuteAsync(
                    "DELETE FROM SchoolManagement.BankTransactionLog WHERE BankTransactionID=@id",
                    new { id = log.RecordID });
            }
        }

        private static async Task SyncExpense(SqlConnection localConn, SqlConnection remoteConn, SyncLog log)
        {
            if (log.ActionType == "INSERT")
            {
                var expense = await localConn.QuerySingleAsync<Expense>(
                    "SELECT * FROM SchoolManagement.Expenses WHERE ExpenseID = @id",
                    new { id = log.RecordID });

                await remoteConn.ExecuteAsync(@"
            INSERT INTO SchoolManagement.Expenses
                (UserID, CategoryID, Amount, ExpenseDate, Description)
            VALUES
                (@UserID, @CategoryID, @Amount, @ExpenseDate, @Description)",
                    expense);
            }
            else if (log.ActionType == "UPDATE")
            {
                var expense = await localConn.QuerySingleAsync<Expense>(
                    "SELECT * FROM SchoolManagement.Expenses WHERE ExpenseID = @id",
                    new { id = log.RecordID });

                await remoteConn.ExecuteAsync(@"
            UPDATE SchoolManagement.Expenses SET
                UserID=@UserID,
                CategoryID=@CategoryID,
                Amount=@Amount,
                ExpenseDate=@ExpenseDate,
                Description=@Description
            WHERE ExpenseID=@ExpenseID",
                    expense);
            }
            else if (log.ActionType == "DELETE")
            {
                await remoteConn.ExecuteAsync(
                    "DELETE FROM SchoolManagement.Expenses WHERE ExpenseID=@id",
                    new { id = log.RecordID });
            }
        }

        private static async Task SyncBank(SqlConnection localConn, SqlConnection remoteConn, SyncLog log)
        {
            if (log.ActionType == "INSERT")
            {
                var bank = await localConn.QuerySingleAsync<BankTransfer>(
                    "SELECT * FROM SchoolManagement.Bank WHERE BankID = @id",
                    new { id = log.RecordID });

                await remoteConn.ExecuteAsync(@"
            INSERT INTO SchoolManagement.Bank
            (PaymentMethodID, BankNumber, MethodName, AmountTransferred, SystemTransferID, AmountInHand, Remarks, UserID)
            VALUES (@PaymentMethodID, @BankNumber, @MethodName, @AmountTransferred, @SystemTransferID, @AmountInHand, @Remarks, @UserID)",
                    bank);
            }
            else if (log.ActionType == "UPDATE")
            {
                var bank = await localConn.QuerySingleAsync<BankTransfer>(
                    "SELECT * FROM SchoolManagement.Bank WHERE BankID = @id",
                    new { id = log.RecordID });

                await remoteConn.ExecuteAsync(@"
            UPDATE SchoolManagement.Bank SET
                PaymentMethodID=@PaymentMethodID,
                BankNumber=@BankNumber,
                MethodName=@MethodName,
                AmountTransferred=@AmountTransferred,
                SystemTransferID=@SystemTransferID,
                AmountInHand=@AmountInHand,
                Remarks=@Remarks,
                UserID=@UserID
            WHERE BankID=@BankID",
                    bank);
            }
            else if (log.ActionType == "DELETE")
            {
                await remoteConn.ExecuteAsync(
                    "DELETE FROM SchoolManagement.Bank WHERE BankID=@id",
                    new { id = log.RecordID });
            }
        }

        private static async Task SyncPaymentsOtherFee(SqlConnection localConn, SqlConnection remoteConn, SyncLog log)
        {
            if (log.ActionType == "INSERT")
            {
                var payment = await localConn.QuerySingleAsync<PaymentsOtherFee>(
                    "SELECT * FROM SchoolManagement.PaymentsOtherFees WHERE PaymentID = @id",
                    new { id = log.RecordID });

                await remoteConn.ExecuteAsync(@"
            INSERT INTO SchoolManagement.PaymentsOtherFees
            (StudentID, FeeTypeID, StudentName, FeeTypeName, ClassID, AmountPaid, AmountLeft, PaymentDate, UserID, PaymentMethod, TermID, PaymentStatus)
            VALUES
            (@StudentID, @FeeTypeID, @StudentName, @FeeTypeName, @ClassID, @AmountPaid, @AmountLeft, @PaymentDate, @UserID, @PaymentMethod, @TermID, @PaymentStatus)",
                    payment);
            }
            else if (log.ActionType == "UPDATE")
            {
                var payment = await localConn.QuerySingleAsync<PaymentsOtherFee>(
                    "SELECT * FROM SchoolManagement.PaymentsOtherFees WHERE PaymentID = @id",
                    new { id = log.RecordID });

                await remoteConn.ExecuteAsync(@"
            UPDATE SchoolManagement.PaymentsOtherFees SET
                StudentID=@StudentID,
                FeeTypeID=@FeeTypeID,
                StudentName=@StudentName,
                FeeTypeName=@FeeTypeName,
                ClassID=@ClassID,
                AmountPaid=@AmountPaid,
                AmountLeft=@AmountLeft,
                PaymentDate=@PaymentDate,
                UserID=@UserID,
                PaymentMethod=@PaymentMethod,
                TermID=@TermID,
                PaymentStatus=@PaymentStatus
            WHERE PaymentID=@PaymentID",
                    payment);
            }
            else if (log.ActionType == "DELETE")
            {
                await remoteConn.ExecuteAsync(
                    "DELETE FROM SchoolManagement.PaymentsOtherFees WHERE PaymentID=@id",
                    new { id = log.RecordID });
            }
        }
        //private static async Task SyncOtherFee(SqlConnection localConn, SqlConnection remoteConn, SyncLog log)
        //{
        //    if (log.ActionType == "INSERT")
        //    {
        //        var fee = await localConn.QuerySingleAsync<OtherFee>(
        //            "SELECT * FROM SchoolManagement.OtherFees WHERE OtherFeeID = @id",
        //            new { id = log.RecordID });

        //        await remoteConn.ExecuteAsync(@"
        //    INSERT INTO SchoolManagement.OtherFees
        //    (FeeTypeName, Description, Amount, ClassID, UserID, RecDateCreated)
        //    VALUES (@FeeTypeName, @Description, @Amount, @ClassID, @UserID, @RecDateCreated)",
        //            fee);
        //    }
        //    else if (log.ActionType == "UPDATE")
        //    {
        //        var fee = await localConn.QuerySingleAsync<OtherFee>(
        //            "SELECT * FROM SchoolManagement.OtherFees WHERE OtherFeeID = @id",
        //            new { id = log.RecordID });

        //        await remoteConn.ExecuteAsync(@"
        //    UPDATE SchoolManagement.OtherFees SET
        //        FeeTypeName=@FeeTypeName,
        //        Description=@Description,
        //        Amount=@Amount,
        //        ClassID=@ClassID,
        //        UserID=@UserID,
        //        RecDateCreated=@RecDateCreated
        //    WHERE OtherFeeID=@OtherFeeID",
        //            fee);
        //    }
        //    else if (log.ActionType == "DELETE")
        //    {
        //        await remoteConn.ExecuteAsync(
        //            "DELETE FROM SchoolManagement.OtherFees WHERE OtherFeeID=@id",
        //            new { id = log.RecordID });
        //    }
        //}
        private static async Task SyncOtherFee(SqlConnection localConn, SqlConnection remoteConn, SyncLog log)
        {
            var fee = await localConn.QuerySingleAsync<OtherFee>(
                "SELECT * FROM SchoolManagement.OtherFees WHERE FeeTypeID = @id",
                new { id = log.RecordID });

            if (log.ActionType == "INSERT")
            {
                await remoteConn.ExecuteAsync(@"
            INSERT INTO SchoolManagement.OtherFees
            (FeeTypeName, Description, Amount, ClassID, UserID, RecDateCreated)
            VALUES (@FeeTypeName, @Description, @Amount, @ClassID, @UserID, @RecDateCreated)",
                    fee);
            }
            else if (log.ActionType == "UPDATE")
            {
                await remoteConn.ExecuteAsync(@"
            UPDATE SchoolManagement.OtherFees SET
                FeeTypeName=@FeeTypeName,
                Description=@Description,
                Amount=@Amount,
                ClassID=@ClassID,
                UserID=@UserID,
                RecDateCreated=@RecDateCreated
            WHERE FeeTypeID=@FeeTypeID",
                    fee);
            }
            else if (log.ActionType == "DELETE")
            {
                await remoteConn.ExecuteAsync(@"
            UPDATE SchoolManagement.OtherFees
            SET DeletedBy=@DeletedBy,
                DeletedOnRecDateCreated=GETDATE()
            WHERE FeeTypeID=@FeeTypeID",
                    new { FeeTypeID = log.RecordID, DeletedBy = fee.UserID });
            }
        }


    }
}

