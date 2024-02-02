using Radzen.Blazor.Rendering;

namespace CORE.MODEL
{
    public class StudentCourseRegistration
    {
        public int StudentID { get; set; }
        public Student Student { get; set; }

        public int CourseID { get; set; }
        public Course Course { get; set; }

        public DateTime RegistrationDate { get; set; }
    }
}
