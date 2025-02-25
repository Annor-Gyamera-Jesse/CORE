namespace CORE.MODEL
{
    public class FeeType
    {
        public int FeeTypeID { get; set; }
        public string FeeTypeName { get; set; }
        public string Description { get; set; }
        public decimal Amount { get; set; }
        public string ClassID { get; set; }
        public DateTime RecDateCreated { get; set; }
        public int? UserID { get; set; }
        public int? DeletedBy { get; set; }
        public DateTime? DeletedOnRecDateCreated { get; set; }
        public int? EditBy { get; set; }
        public DateTime? EditedOnRecDateCreated { get; set; }
    }


    public class StudentFee
    {
        public int FeeID { get; set; }
        public int StudentID { get; set; }
        public int FeeTypeID { get; set; }
        public string StudentName { get; set; }
        public string FeeTypeName { get; set; }
        public string ClassID { get; set; }
        public decimal AmountPaid { get; set; }
        public decimal? AmountLeft { get; set; }
        public DateTime PaymentDate { get; set; }
        public DateTime DueDate { get; set; }
        public string Note { get; set; }
        public int UserID { get; set; }
        public string PaymentMethod { get; set; }
        public int TermID { get; set; }
    }

    public class FeePayment
    {
        public int FeePaymentID { get; set; }
        public int FeeID { get; set; }
        public decimal AmountPaid { get; set; }
        public string PaymentMethod { get; set; }
        public DateTime TransactionDate { get; set; }
        public string ReferenceNumber { get; set; }
        public string Note { get; set; }
        public int UserID { get; set; }
    }

    public class PaymentMethod
    {
        public string Code { get; set; }
        public string Name { get; set; }
    }

}
