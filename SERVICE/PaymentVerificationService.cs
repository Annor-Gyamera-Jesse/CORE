using CORE.MODEL;
using Newtonsoft.Json;



namespace CORE.SERVICE
{
    public class PaymentVerificationService
    {
        private readonly HttpClient _httpClient;
        private const string SecretKey = "sk_live_8e306ba1b6d55884d12e4775b2c13f060f7f0a47";
        private const string BaseUrl = "https://api.paystack.co/";

        public PaymentVerificationService(HttpClient httpClient)
        {
            _httpClient = httpClient;
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {SecretKey}");
        }

        public async Task<PaymentVerificationResult> VerifyTransactionAsync(string reference)
        {
            var response = await _httpClient.GetAsync($"{BaseUrl}transaction/verify/{reference}");
            response.EnsureSuccessStatusCode();

            var responseData = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<PaymentVerificationResponse>(responseData);

            // Normalize amount by dividing by 100 if needed
            decimal normalizedAmount = result.Data.Amount / 100m;

            return new PaymentVerificationResult
            {
                Status = result.Data.Status,
                Message = result.Message,
                Details = new Dictionary<string, string>
        {
            { "Reference", result.Data.Reference },
            { "Status", result.Data.Status },
            { "Paid At", result.Data.PaidAt },
            { "Channel", result.Data.Channel },
            { "Currency", result.Data.Currency },
            { "Amount", $"{result.Data.Currency} {normalizedAmount:N2}" }, // Explicit formatting
            { "Card Type", result.Data.Authorization?.CardType ?? "N/A" },
            { "Bank", result.Data.Authorization?.Bank ?? "N/A" },
            { "Email", result.Data.Customer?.Email ?? "N/A" }
        }
            };
        }


    }
}
