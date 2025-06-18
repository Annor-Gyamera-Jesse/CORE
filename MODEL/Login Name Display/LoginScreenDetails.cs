namespace CORE.MODEL.Login_Name_Display
{
    public class LoginScreenDetail
    {
        public string Title { get; set; }
        public byte[] CompanyImage { get; set; }
        public string SchoolName { get; set; }
        public string CompanyRegisteredName { get; set; }
        public string SoftWareVerssion { get; set; }
        public int CompanyRegisteredDate => DateTime.Now.Year;
    }
}
