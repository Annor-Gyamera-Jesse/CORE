namespace CORE.MODEL
{
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
