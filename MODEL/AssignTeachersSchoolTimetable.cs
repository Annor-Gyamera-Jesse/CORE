namespace CORE.MODEL
{
    public class AssignTeachersSchoolTimetable
    {
        public int ClassSchedulingID { get; set; }
        public string ClassID { get; set; }
        public string SchoolCourseName { get; set; }
        public string Period { get; set; }
        public string Day { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public int TeacherID { get; set; }
        public int UserID { get; set; }
        public string Note { get; set; }
    }

}
