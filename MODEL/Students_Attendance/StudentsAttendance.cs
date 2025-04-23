namespace CORE.MODEL.Students_Attendance
{
    public class StudentsAttendance
    {
        public int AttendanceID { get; set; }
        public string StudentFirstName { get; set; }
        public string StudentLastName { get; set; }
        public string ClassID { get; set; }
        public bool EnableSwitch { get; set; }
        public DateTime RecDateCreated { get; set; }
        public int UserID { get; set; }
        public DateTime AttendanceDate { get; set; }
        public int TermID { get; set; }
        public string Term { get; set; }  // from join
    }
}
