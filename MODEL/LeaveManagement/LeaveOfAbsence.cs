namespace CORE.MODEL.LeaveManagement
{
    public class LeaveOfAbsence
    {
        public int LeaveID { get; set; }
        public int? UserID { get; set; }
        public int? TeacherID { get; set; }
        public int? StaffID { get; set; }
        public string LeaveType { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Reason { get; set; }
        public string Status { get; set; } = "Pending";
        public int? ApprovedBy { get; set; }
        public DateTime? DateRequested { get; set; }
        public DateTime? DateApproved { get; set; }
        public int? UpdatedBY { get; set; }
    }

    public class PersonOption
    {
        public int ID { get; set; }
        public string FullName { get; set; }
    }

}
