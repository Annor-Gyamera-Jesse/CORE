namespace CORE.MODEL.Expenses
{
    public class ExpenseHistoryView
    {
        public int ExpenseID { get; set; }
        public string UserName { get; set; }
        public string CategoryName { get; set; }
        public decimal Amount { get; set; }
        public string Description { get; set; }
        public DateTime ExpenseDate { get; set; }
    }

}
