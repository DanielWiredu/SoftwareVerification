using Microsoft.EntityFrameworkCore;

namespace SoftwareVerification_API.Models
{
    //[Index(nameof(AccountNumber), IsUnique = true)]
    public class Account
    {
        public int Id { get; set; }
        public string AccountNumber { get; set; } = "";
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public double Balance { get; set; } = 0;
        public bool IsClosed { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

}
