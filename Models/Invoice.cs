using System;

namespace ClaimsManagementApp.Models
{
    public class Invoice
    {
        public int ClaimId { get; set; }
        public string LecturerName { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public DateTime GeneratedDate { get; set; }
        public string InvoiceNumber { get; set; } = string.Empty;
    }
}