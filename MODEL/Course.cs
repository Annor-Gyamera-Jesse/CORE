namespace CORE.MODEL
{
    public class Course
    {
        public int CourseID { get; set; }
        public string CourseName { get; set; }
        public string Description { get; set; }

        // Navigation property (if needed)
        //public ICollection<StudentCourseRegistration> StudentRegistrations { get; set; }
    }
}
