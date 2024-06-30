namespace CORE.MODEL
{
    public class TeachersTask
    {
        public int TeacherTaskID { get; set; }
        public string TeachersName { get; set; }
        public string TeacherTask { get; set; }
        public bool SwitchBar { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public TaskStatus Status { get; set; }
        public int UserID { get; set; }
    }
}
