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
        public DateTime DateHired { get; set; }
        // SSNIT Fields
        public string SSNIT { get; set; }  // Dropdown (Y/N)
        public decimal BasicSalary { get; set; }
        public decimal PAYE { get; set; }
        public decimal SSNITTIER2 { get; set; }

        // Account Details Fields
        public string VotersID { get; set; }  // Dropdown (Yes/No)
        public string HealthInsurance { get; set; }  // Dropdown (Yes/No)
        public string GhanaCard { get; set; }  // Dropdown (Yes/No)
        public string Bank { get; set; }  // Selected Bank Name
        public string AccountNumber { get; set; }
        public string Remarks { get; set; }
        public string SSNITNumber { get; set; }
        public string CategoryName { get; set; }

    }
}
