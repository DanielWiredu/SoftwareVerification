namespace SoftwareVerification_API.Models
{
    public class Transaction
    {
        public int Id { get; set; }
        public string AccountNumber { get; set; } = "";
        public double Amount { get; set; }
        public string Type { get; set; } = ""; // Deposit, Withdraw, etc.
        public string Description { get; set; } = ""; // Optional description
        public DateTime Timestamp { get; set; }
    }
}
