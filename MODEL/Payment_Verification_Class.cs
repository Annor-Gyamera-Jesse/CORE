namespace CORE.MODEL
{
    public class PaymentVerificationResult
    {
        public string Status { get; set; }
        public string Message { get; set; }
        public Dictionary<string, string> Details { get; set; } = new();
    }

    public class PaymentVerificationResponse
    {
        public bool Status { get; set; }
        public string Message { get; set; }
        public PaymentData Data { get; set; }
    }

    public class PaymentData
    {
        public string Status { get; set; }
        public string Reference { get; set; }
        public string GatewayResponse { get; set; }
        public string PaidAt { get; set; }
        public string Channel { get; set; }
        public string Currency { get; set; }
        public decimal Amount { get; set; }
        public Authorization Authorization { get; set; }
        public Customer Customer { get; set; }
    }

    public class Authorization
    {
        public string CardType { get; set; }
        public string Last4 { get; set; }
        public string Bank { get; set; }
        public string ExpMonth { get; set; }
        public string ExpYear { get; set; }
    }

    public class Customer
    {
        public string Email { get; set; }
        public string CustomerCode { get; set; }
    }
}
