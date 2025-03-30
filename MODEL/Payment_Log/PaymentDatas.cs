namespace CORE.MODEL.Payment_Log
{
    public class PaymentDatas
    {
        public int StaffID { get; set; }
        public int CategoryID { get; set; }
        public int SalaryFor { get; set; }
        public int PaymentYear { get; set; }
        public decimal Amount { get; set; }
        public decimal OverTime { get; set; }
        public decimal TaxDeduction { get; set; }
        public string BankName { get; set; }
        public string AccountName { get; set; }
        public string AccountNumber { get; set; }
    }
}
