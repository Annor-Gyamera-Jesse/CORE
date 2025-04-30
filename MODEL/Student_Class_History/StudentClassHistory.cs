namespace CORE.MODEL.Student_Class_History
{
    public class StudentClassHistory
    {
        public int HistoryID { get; set; }
        public int StudentID { get; set; }
        public string Course { get; set; }
        public string ClassName { get; set; }
        public int Term { get; set; }
        public int ClassScore { get; set; }
        public int ExamsScore { get; set; }
        public int TotalScore { get; set; }
        public string Position { get; set; }
        public DateTime DateCreated { get; set; }
    }

}
