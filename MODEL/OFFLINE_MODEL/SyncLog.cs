namespace CORE.MODEL.OFFLINE_MODEL
{
    public class SyncLog
    {
        public int SyncID { get; set; }
        public string TableName { get; set; }
        public int RecordID { get; set; }
        public string ActionType { get; set; }
        public string SyncStatus { get; set; }
        public int RetryCount { get; set; }
        public int MaxRetry { get; set; }
        public string ConflictStatus { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastTriedAt { get; set; }
        public DateTime LastModifiedAt { get; set; }
        public string ErrorMessage { get; set; }
    }

}
