namespace CORE.MODEL.Bank.Transaction_Logs
{
    public class TransactionLog
    {
        public int TransactionID { get; set; }
        public string TransactionType { get; set; }
        public decimal AmountBeforeTransfer { get; set; }
        public decimal AmountTransferred { get; set; }
        public decimal AmountAfterTransfer { get; set; }
        public DateTime TransferDate { get; set; }
        public int UserID { get; set; }
    }

    public class BankTransactionLog
    {
        public int TransactionID { get; set; }
        public int BankID { get; set; }
        public string BankName { get; set; }
        public string TransactionType { get; set; }
        public decimal Amount { get; set; }
        public DateTime TransactionDate { get; set; }
        public string Status { get; set; }
        public string ErrorMessage { get; set; }
    }

    public class PaymentLog
    {
        public int LogID { get; set; }
        public int? PaymentID { get; set; }
        public int? BankID { get; set; }
        public string BankName { get; set; }
        public string PaymentType { get; set; }
        public decimal Amount { get; set; }
        public DateTime TransactionDate { get; set; }
        public string Status { get; set; }
        public string ErrorMessage { get; set; }
        public DateTime CreatedAt { get; set; }
        public int? PaymentMethodID { get; set; }
    }

}
