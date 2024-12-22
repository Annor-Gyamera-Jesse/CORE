using Microsoft.VisualBasic;

namespace CORE.MODEL
{
    public class TeacherAttendance
    {
      public int TeacherID { get; set;}
        public string TeacherFirstName { get; set; }
        public string TeacherLastName { get; set; }
        public bool EnableSwitch { get; set; }
        public DateTime ClockOUT { get; set; }
        public DateTime? ClockInTime { get; set; }
        public DateTime? ClockOutTime { get; set; }
        public DateTime ClockIN { get; set; }
        public DateTime RecDateCreated { get; set; }
        public int UserID { get; set; }
        public string FullName => $"{TeacherFirstName} {TeacherLastName}";
    }
}
