namespace CORE.MODEL
{
    public class User_Log
    { 
        public int ErrorLogId {  get; set; }
        public DateTime LogDate { get; set; }
        public string ErrorMessage { get; set; }
        public string StackTrace { get; set; }
        public string ControllerName { get; set; }
        public string ActionName { get; set; }
        public int UserID { get; set; }
    }
}
