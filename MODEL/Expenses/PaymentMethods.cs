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
}
