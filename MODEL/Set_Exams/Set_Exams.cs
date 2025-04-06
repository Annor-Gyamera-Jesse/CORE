namespace CORE.MODEL.Set_Exams
{
    public class SetMainExamsModel
    {
        public int SetExamsID { get; set; }
        public string Class { get; set; }
        public string SchoolCourse { get; set; }
        public string ExamsTitle { get; set; }
        public string QuestionType { get; set; }
        public string ExamsMainDescription { get; set; }
        public string ExamsSubDescription { get; set; }
        public DateTime? ExamsTimeLimit { get; set; }
        public int UserID { get; set; }
    }

    public class ExamsExamsContentModel
    {
        public int ExamsExamsContent { get; set; }
        public int SetExamsID { get; set; }
        public string Question { get; set; }
        public string SubQuestion { get; set; }
        public string ChoiceA { get; set; }
        public string ChoiceB { get; set; }
        public string ChoiceC { get; set; }
        public string ChoiceD { get; set; }
        public byte[] QuestionImageData { get; set; }
        public string CorrectAnswer { get; set; }
        public string Remarks { get; set; }
        public bool QuestionSwitchButton { get; set; }
        public int UserID { get; set; }
    }



}
