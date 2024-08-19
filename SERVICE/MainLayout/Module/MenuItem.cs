namespace CORE.SERVICE.MainLayout.Module
{
    public class MenuItem
    {
        public int MenuID { get; set; }
        public string Text { get; set; }
        public string Path { get; set; }
        public string Icon { get; set; }
    }

    //Form Mobile App Menuitem
    public class MobileAppRole
    {
        public int UserRoleID { get; set; }
        public int UserID { get; set; }
        public string UserName { get; set; }
        public string RoleName { get; set; }
        public bool Enable { get; set; }
        public string MenuItem { get; set; }
        public string CategoryName { get; set; }
    }

    public class MobileMenuItem
    {
        public int CategoryID { get; set; }
        public string CategoryName { get; set; }
        public string ItemName { get; set; }
    }

}
