namespace CORE.MODEL.Expenses
{
    public class ExpensePayment
    {
        public int ExpensePaymentID { get; set; }
        public int ExpenseID { get; set; }
        public int PaymentMethodID { get; set; }
        public DateTime PaymentDate { get; set; }
        public decimal AmountPaid { get; set; }
        public int UserID { get; set; }
    }
}
