namespace CORE.SERVICE.MainLayout.Module
{
    public class MenuAccess
    {
        public int RoleID { get; set; }
        public int MenuID { get; set; }
        public bool CanAccess { get; set; }
        public string MenuName { get; set; } // Add this property
    }
}
