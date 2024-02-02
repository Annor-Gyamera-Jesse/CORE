namespace CORE.MODEL
{
    public class UserRole
    {
        public int UserID { get; set; }
        public string UserName { get; set; }
        public int RoleID { get; set; }
        public string RoleName { get; set; }
        public bool Assigned { get; set; }
    }

}
