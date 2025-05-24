namespace CORE.MODEL.Expenses
{
    public class PaymentMethods
    {
        public int PaymentMethodID { get; set; }
        public string MethodName { get; set; }
        public string Description { get; set; }     
        public int UserID { get; set; }
        public string BankNumber { get; set; }
    }

    public class PaymentViewModel
    {
        public int BankID { get; set; }
        public int PaymentMethodID { get; set; }
        public string MethodName { get; set; }
        public string BankNumber { get; set; }
        public decimal AmountTransferred { get; set; }
        public decimal AmountInHand { get; set; }
        public DateTime TransferDate { get; set; }
        public int SystemTransferID { get; set; }
        public string Remarks { get; set; }
        public int UserID { get; set; }
        public string UserName { get; set; } // From Users table
    }
}
