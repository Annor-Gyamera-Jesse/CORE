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

}
