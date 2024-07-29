namespace CORE.MODEL
{
    public class TeacherSubjectAssignment
    {
        public int TeacherID { get; set; }
        public string SCID { get; set; }
        public string ClassID { get; set; }
        public int DayID { get; set; }
        public DateTime? SubjectStartTime { get; set; }
        public DateTime? SubjectEndTime { get; set; }
        public DateTime? AssignmentDate { get; set; }
    }
}
