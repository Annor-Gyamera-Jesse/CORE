namespace CORE.MODEL.Salary_Payment_History
{
    public class SalaryPaymentHistory
    {
        public int PaymentID { get; set; }
        public int StaffID { get; set; }
        public decimal Amount { get; set; }
        public int SalaryFor { get; set; }
        public int PaymentYear { get; set; }
        public DateTime PayedOn { get; set; }
        public string PaymentMethod { get; set; }
        public string BankName { get; set; }
        public string AccountName { get; set; }
        public string AccountNumber { get; set; }
        public decimal OverTime { get; set; }
        public decimal TaxDeduction { get; set; }
    }

}
