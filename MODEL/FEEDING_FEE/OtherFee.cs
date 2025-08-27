namespace CORE.MODEL.FEEDING_FEE
{
    public class OtherFee
    {
        public int FeeTypeID { get; set; }               // PK
        public string FeeTypeName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal Amount { get; set; }
        public string ClassID { get; set; } = string.Empty;
        public DateTime RecDateCreated { get; set; } = DateTime.Now;
        public int? UserID { get; set; }
        public int? DeletedBy { get; set; }
        public int? EditBy { get; set; }
        public DateTime? DeletedOnRecDateCreated { get; set; }
        public DateTime? EditedOnRecDateCreated { get; set; }
    }

    public class FeeSummaryDto
    {
        public string ClassID { get; set; }
        public string FeeTypeName { get; set; }
        public int TotalStudents { get; set; }
        public decimal FeePerStudent { get; set; }
        public decimal ExpectedAmount { get; set; }
        public decimal CollectedAmount { get; set; }
        public decimal BalanceLeft { get; set; }
    }
}
