namespace CORE.MODEL
{
    public class TeachersAttendanceOut
    {
        public int TeachersAttendanceOutID { get; set; }
        public string TeacherFirstName { get; set; }
        public string TeacherLastName { get; set; }
        public bool EnableSwitch { get; set; }
        public DateTime ClockOUT { get; set; }
        public DateTime ClockIN { get; set; }
        public int UserID { get; set; }
    }
}
