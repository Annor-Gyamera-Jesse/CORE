namespace CORE.MODEL.DASHBOARD_AMOUNT_SUM
{
    public class TermFeeSummary
    {
        public int TermID { get; set; }
        public decimal ExpectedTotal { get; set; }
        public decimal ActualTotal { get; set; }
        public decimal Outstanding => ExpectedTotal - ActualTotal;
    }

    public class ClassTermFeeSummary
    {
        public string ClassID { get; set; }
        public int TermID { get; set; }
        public string TermName { get; set; }
        public int TotalStudents { get; set; }
        public int FeeTypesCount { get; set; }
        public decimal ExpectedTotal { get; set; }
        public decimal ActualTotal { get; set; }
        public decimal Outstanding { get; set; }
    }


}
