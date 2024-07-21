namespace CORE.MODEL
{
    public class SchoolTimetable
    {
        public int ClassSchedulingID { get; set; }
        public string ClassID { get; set; }
        public int CourseID { get; set; }
        public string CourseName { get; set; }  // New property
        public int PeriodID { get; set; }
        public string Day { get; set; }
        public int TeacherID { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public int UserID { get; set; }
        public string Note { get; set; }
        public DateTime RecDateCreated { get; set; }
    }

    public class Period
    {
        public int PeriodID { get; set; }
        public string PeriodName { get; set; }
    }

    public class TeacherAssignment
    {
        public int ClassSchedulingID { get; set; }
        public string ClassID { get; set; }
        public int CourseID { get; set; }
        public string CourseName { get; set; }
        public int TeacherID { get; set; }
        public string TeacherName { get; set; }
        public string Day { get; set; }
        public string Period { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string Note { get; set; }
    }
}
