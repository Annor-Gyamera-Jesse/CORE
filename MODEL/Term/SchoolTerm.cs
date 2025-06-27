namespace CORE.MODEL.Term
{
    public class SchoolTerm
    {
        public int TermID { get; set; }
        public string Term { get; set; }
        public bool IsCurrentTerm { get; set; }
        public DateTime TermEndDate { get; set; }
    }
}
