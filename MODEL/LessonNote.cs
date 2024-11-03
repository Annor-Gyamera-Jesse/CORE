using CORE.MODEL;

namespace CORE.Pages.LESSON_NOTE
{
    public class LessonNote
    {
        public int LessonnotesID { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; }
        public string SchoolCourse { get; set; }
        public string Strand { get; set; }
        public string SubStrand { get; set; }
        public string ContentStandard { get; set; }
        public string Indicator { get; set; }
        public string TeachingLearningResources { get; set; }
        public string TeachingLearningResourcePreparationNotes { get; set; }
        public string SourcesLearningResources { get; set; }
        public string LearningGroup { get; set; }
        public string LearnerExpectation { get; set; }
        public string ImportantGradeExpectation { get; set; }
        public string LearningOutcomes { get; set; }
        public string FormofAssessment { get; set; }
        public string LearnerEntryBehavior { get; set; }
        public string SequenceofLesson { get; set; }
        public Lesson_Note_Dialog_Status Status { get; set; }
        public int UpdatedBy { get; set; }
        public DateTime UpdatedOn { get; set; } = DateTime.Now; // Initialize with current date/time
        public string ClassID { get; set; }
    }

}
