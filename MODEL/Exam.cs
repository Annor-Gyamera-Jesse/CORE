namespace CORE.MODEL
{
    public class Exam
    {
        public int ExamID { get; set; }
        public int UserID { get; set; }
        //public string CourseID { get; set; }
        public int StudentID { get; set; }
        public string StudentName { get; set; }
        public string ClassName { get; set; }
        public string AcademicYear { get; set; }
        public DateTime? VacationDate { get; set; }
        public string PromotedTo { get; set; }
        public int NumberOnRoll { get; set; }
        public int TermID { get; set; }
        public string Position { get; set; }
        public DateTime? NextTermsBegins { get; set; }
        public int AttendanceOut { get; set; }
        public int AttendanceIn { get; set; }
        public string SchoolCourse { get; set; }
        public int ClassScore { get; set; }
        public int ExamsScore { get; set; }
        public int TotalScore { get; set; }
        public string SubjectsPositions { get; set; }
        public string Grade { get; set; }
        public string TeachersRemarks { get; set; }
        public string Conduct { get; set; }
        public string HeadmasterRemark { get; set; }
        public string SchoolInformation { get; set; }
        public string TeachersSignature { get; set; }
        public string HeadMasterSignature { get; set; }
        //public string ClassID { get; set; }
        // Add a property for related SchoolCourse data
        public SchoolCourse SchoolCourseData
        {
            get; set;
        }
    }
}
