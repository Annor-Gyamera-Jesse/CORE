using System.ComponentModel.DataAnnotations;

namespace CORE.MODEL
{
    public class Student
    {
        public int StudentID { get; set; }
        public string StudentFirstName { get; set; }
        public string StudentLastName { get; set; }
        public DateTime StudentDateOfBirth { get; set; }
        public string StudentGender { get; set; }
        public string StudentAddress { get; set; }
        public string StudentPhoneNumber { get; set; }
        public string StudentEmail { get; set; }
        public byte[] ImageData { get; set; }
        public string ClassID { get; set; }
        public string GuardianFullName { get; set; }
        public string GuardianGender { get; set; }
        public string GuardianHouseAddress { get; set; }
        public string GuardianWorkAddress { get; set; }
        public string GuardianEmail { get; set; }
        public string GuardianFirstContact { get; set; }
        public string GuardianSecondContact { get; set; }
        public bool EnableSwitch { get; set; }
        public string StudentMedicalReport { get; set; }
        public int UserID { get; set; }
        public string FullName => $"{StudentFirstName} {StudentLastName}";
        public string DisplayInfo => $"{FullName} - {ClassID}";
        public string ImageBase64 { get; set; }
        public string SchoolName { get; set; }
        public byte[] CompanyImage { get; set; }
        public string SchoolImageBase64 { get; set; }

    }

    public class StudentLookupDto
    {
        public int StudentID { get; set; }
        public string DisplayText => $"{StudentFirstName} {StudentLastName} – {ClassID}";
        public string StudentFirstName { get; set; }
        public string StudentLastName { get; set; }
        public string ClassID { get; set; }
    }

}

