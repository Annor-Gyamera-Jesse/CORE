namespace CORE.MODEL
{
    public class UserLog
    {
        public int LogId { get; set; }
        public int UserId { get; set; }
        public string EventName { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
