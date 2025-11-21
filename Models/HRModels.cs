using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ClaimsManagementApp.Models
{
    public class HRDashboardViewModel
    {
        public int TotalClaims { get; set; }
        public int ApprovedClaims { get; set; }
        public int PendingSettlement { get; set; }
        public int SettledClaims { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal PendingAmount { get; set; }
        public List<Claim> RecentApprovedClaims { get; set; } = new List<Claim>();
        public List<Claim> ReadyForSettlement { get; set; } = new List<Claim>();
    }

    public class SettlementRequest
    {
        public int ClaimId { get; set; }

        [Required(ErrorMessage = "Settlement notes are required")]
        [MinLength(10, ErrorMessage = "Settlement notes must be at least 10 characters long")]
        public string SettlementNotes { get; set; } = string.Empty;

        [Required(ErrorMessage = "Payment reference is required")]
        [MinLength(3, ErrorMessage = "Payment reference must be at least 3 characters long")]
        public string PaymentReference { get; set; } = string.Empty;
    }

    public class SettlementViewModel
    {
        public Claim Claim { get; set; } = new Claim();
        public SettlementRequest SettlementRequest { get; set; } = new SettlementRequest();
    }

    public class HRAnalyticsViewModel
    {
        public int TotalClaims { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal AverageClaimAmount { get; set; }
        public Dictionary<string, int> ClaimsByStatus { get; set; } = new Dictionary<string, int>();
        public Dictionary<string, int> MonthlyTrends { get; set; } = new Dictionary<string, int>();
    }
}