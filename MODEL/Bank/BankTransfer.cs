namespace CORE.MODEL.Bank
{
    // BankTransfer Model
    public class BankTransfer
    {
        public int BankID { get; set; }
        public int PaymentMethodID { get; set; }
        public string BankNumber { get; set; }
        public string MethodName { get; set; }
        public decimal AmountTransferred { get; set; }
        public DateTime TransferDate { get; set; }
        public int SystemTransferID { get; set; }
    }

    public class BankDeposit
    {
        public int UserID   { get; set; }
        public int PaymentMethodID { get; set; }
        public string BankNumber { get; set; }
        public string MethodName { get; set; }
        public decimal AmountTransferred { get; set; }
        public string Remarks { get; set; }
    }


    public class BankBalance
    {
        public string MethodName { get; set; }
        public decimal TotalAmount { get; set; }
    }

    public class StudentOwingByClass
    {
        public string ClassID { get; set; }
        public int TotalOwingStudents { get; set; }
        public decimal TotalOwingAmount { get; set; }
    }

    // Strongly typed model instead of dynamic
    public class BankEntry
    {
        public int BankID { get; set; }
        public int PaymentMethodID { get; set; }
        public string BankNumber { get; set; }
        public string MethodName { get; set; }
        public decimal AmountInHand { get; set; }
        public decimal AmountTransferred { get; set; }
        public int UserID { get; set; }
    }

}
