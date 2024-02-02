namespace CORE.MODEL
{
    public class NonTeachingStaffsRegistration
    {
        public int NTSID { get; set; }
        public string NonTeachingStaffsFirstName { get; set; }
        public string NonTeachingStaffsLastName { get; set; }
        public DateTime NonTeachingStaffsDateOfBirth { get; set; }
        public string NonTeachingStaffsGender { get; set; }
        public string NonTeachingStaffsAddress { get; set; }
        public string NonTeachingStaffsPhoneNumber { get; set; }
        public string NonTeachingStaffsEmail { get; set; }
        public byte[] ImageData { get; set; }
    }
}
