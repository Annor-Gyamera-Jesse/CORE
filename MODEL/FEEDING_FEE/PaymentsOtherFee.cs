namespace CORE.MODEL.FEEDING_FEE
{
    public class PaymentsOtherFee
    {
        public int FeeID { get; set; }                   // PK
        public int StudentID { get; set; }               // FK Students
        public int FeeTypeID { get; set; }               // FK OtherFees
        public string StudentName { get; set; } = string.Empty;
        public string FeeTypeName { get; set; } = string.Empty;
        public string ClassID { get; set; } = string.Empty;

        public decimal AmountPaid { get; set; } = 0.00m;
        public decimal? AmountLeft { get; set; }

        public DateTime PaymentDate { get; set; } = DateTime.Now;
        public DateTime? DueDate { get; set; }
        public string? Note { get; set; }

        public int? UserID { get; set; }
        public DateTime RecDateCreated { get; set; } = DateTime.Now;

        public string PaymentMethod { get; set; } = string.Empty;
        public string SystemTransferStatus { get; set; } = "New";
        public int? SystemTransferID { get; set; }
        public string? TransferErrorMessage { get; set; }

        public int TermID { get; set; }
        public string PaymentStatus { get; set; } = "Not Fully Paid";
    }
}
