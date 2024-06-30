namespace CORE.Pages.LESSON_NOTE
{
    public class LessonNote
    {
        public int LessonnotesID { get; set; }
        public string UserName { get; set; }
        public int UserId { get; set; }
        public string SchoolCourse { get; set; }
        public string Topic { get; set; }
        public string OBJECTIVES { get; set; }
        public string TLMTLA { get; set; }
        public string INTRODUCTION { get; set; }
        public string COREPOINTS { get; set; }
        public string EVALUATIONREMARKS { get; set; }
        public DateTime RecDateCreated { get; set; } = DateTime.Now; // Initialize with current date/time
    }
}
