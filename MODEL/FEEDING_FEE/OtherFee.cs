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
}
