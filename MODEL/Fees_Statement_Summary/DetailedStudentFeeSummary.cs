namespace CORE.MODEL.Fees_Statement_Summary
{
    public class DetailedStudentFeeSummary
    {
        public string StudentName { get; set; }
        public string CurrentClass { get; set; }
        public int CurrentTerm { get; set; }
        public string CurrentFeeType { get; set; }
        public decimal CurrentAmount { get; set; }
        public decimal CurrentPaid { get; set; }
        public decimal CurrentLeft { get; set; }
        public string PreviousClass { get; set; }
        public string PreviousFeeType { get; set; }
        public decimal PreviousAmount { get; set; }
        public decimal PreviousPaid { get; set; }
        public decimal PreviousLeft { get; set; }
        public string Reason { get; set; }
    }

}
