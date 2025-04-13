namespace CORE.MODEL.LeaveStatus
{
    public class LeaveOfAbsenceViewModel
    {
        public int LeaveID { get; set; }
        public int UserID { get; set; }
        public string UserName { get; set; }
        public string LeaveType { get; set; } // E.g., Sick Leave, Vacation
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Reason { get; set; }
        public LeaveStatus Status { get; set; }  // Enum for the leave status (Pending, Approved, Rejected)
        public int? ApprovedBy { get; set; }  // User ID of the approver
        public DateTime? DateApproved { get; set; } // The date when the leave was approved
        public int UpdatedBY { get; set; }
    }

    public enum LeaveStatus
    {
        Pending = 0,
        Approved = 1,
        Rejected = 2
    }
}
