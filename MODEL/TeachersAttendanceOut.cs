namespace CORE.MODEL
{
    public class TeachersAttendanceOut
    {
        public int TeachersAttendanceOutID { get; set; }
        public int TeacherID { get; set; }
        public string TeacherFirstName { get; set; }
        public string TeacherLastName { get; set; }
        public bool EnableSwitch { get; set; }
        public DateTime ClockOUT { get; set; }
        public int UserID { get; set; }
    }
}
