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
}
