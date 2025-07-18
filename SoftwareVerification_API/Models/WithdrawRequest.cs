namespace SoftwareVerification_API.Models
{
    public class WithdrawRequest
    {
        public string AccountNumber { get; set; } = "";
        public double Amount { get; set; }
    }
}
