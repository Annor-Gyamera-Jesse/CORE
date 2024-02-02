namespace CORE.MODEL
{
    public class TeachersRegistration
    {
        public int TeacherID { get; set; }
        public string TeacherFirstName { get; set; }
        public string TeacherLastName { get; set; }
        public DateTime TeacherDateOfBirth { get; set; }
        public string TeacherGender { get; set; }
        public string TeacherAddress { get; set; }
        public string TeacherPhoneNumber { get; set; }
        public string TeacherEmail { get; set; }
        public byte[] ImageData { get; set; }
        public bool EnableSwitch { get; set; }
        public string FullName => $"{TeacherFirstName} {TeacherLastName}";
    }
}
