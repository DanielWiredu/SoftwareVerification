namespace SoftwareVerification_API.Models
{
    public class TransferRequest
    {
        public string from_account { get; set; } = string.Empty;
        public string to_account { get; set; } = string.Empty;
        public double amount { get; set; }
    }
}
