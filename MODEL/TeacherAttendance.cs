using Microsoft.VisualBasic;

namespace CORE.MODEL
{
    public class TeacherAttendance
    {
      public int  TeachersAttendanceID {get; set;}
        public int TeacherID { get; set;}
        public string TeacherFirstName { get; set; }
        public string TeacherLastName { get; set; }
        public bool EnableSwitch { get; set; }
        //public DateTime ClockOUT { get; set; }
        public DateTime ClockIN { get; set; }
        public int UserID { get; set; }
    }
}
