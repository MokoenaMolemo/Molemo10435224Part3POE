using System.Text;
using ClaimsManagementApp.Models;
using System.Linq;

namespace ClaimsManagementApp.Services
{
    public class ReportService : IReportService
    {
        public string GenerateMonthlyClaimsReport(List<Claim> claims)
        {
            var report = new StringBuilder();
            report.AppendLine("ClaimID,LecturerName,HoursWorked,HourlyRate,TotalAmount,Status,SubmissionDate");

            foreach (var claim in claims)
            {
                report.AppendLine($"{claim.Id},{claim.LecturerName},{claim.HoursWorked},{claim.HourlyRate},{claim.TotalAmount},{claim.Status},{claim.SubmissionDate:yyyy-MM-dd}");
            }

            return report.ToString();
        }

        public List<Invoice> GenerateInvoices(List<Claim> claims)
        {
            var invoices = new List<Invoice>();
            var approvedClaims = claims.Where(c => c.Status == ClaimStatus.ApprovedByManager).ToList();

            foreach (var claim in approvedClaims)
            {
                invoices.Add(new Invoice
                {
                    ClaimId = claim.Id,
                    LecturerName = claim.LecturerName,
                    Amount = claim.TotalAmount,
                    GeneratedDate = DateTime.Now,
                    InvoiceNumber = $"INV-{DateTime.Now:yyyyMMdd}-{claim.Id:000}"
                });
            }

            return invoices;
        }

        public string GenerateClaimsSummary(List<Claim> claims)
        {
            var summary = new StringBuilder();
            summary.AppendLine("=== CLAIMS SUMMARY REPORT ===");
            summary.AppendLine($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            summary.AppendLine($"Total Claims: {claims.Count}");
            summary.AppendLine($"Pending: {claims.Count(c => c.Status == ClaimStatus.Pending)}");
            summary.AppendLine($"Approved by Coordinator: {claims.Count(c => c.Status == ClaimStatus.ApprovedByCoordinator)}");
            summary.AppendLine($"Approved by Manager: {claims.Count(c => c.Status == ClaimStatus.ApprovedByManager)}");
            summary.AppendLine($"Rejected: {claims.Count(c => c.Status == ClaimStatus.RejectedByCoordinator || c.Status == ClaimStatus.RejectedByManager)}");
            summary.AppendLine($"Settled: {claims.Count(c => c.Status == ClaimStatus.Settled)}");
            summary.AppendLine($"Total Amount: R {claims.Where(c => c.Status == ClaimStatus.ApprovedByManager || c.Status == ClaimStatus.Settled).Sum(c => c.TotalAmount):N2}");

            return summary.ToString();
        }

        public string GeneratePaymentReport(List<Claim> claims)
        {
            var report = new StringBuilder();
            report.AppendLine("ClaimID,LecturerName,HoursWorked,HourlyRate,TotalAmount,SubmissionDate,SettlementDate,PaymentReference,SettlementNotes");

            foreach (var claim in claims.Where(c => c.Status == ClaimStatus.Settled))
            {
                report.AppendLine($"{claim.Id},{claim.LecturerName},{claim.HoursWorked},{claim.HourlyRate},{claim.TotalAmount},{claim.SubmissionDate:yyyy-MM-dd},{claim.SettlementDate:yyyy-MM-dd},{claim.PaymentReference},{EscapeCsvField(claim.SettlementNotes ?? "")}");
            }

            return report.ToString();
        }

        private string EscapeCsvField(string field)
        {
            if (string.IsNullOrEmpty(field)) return string.Empty;

            // Escape quotes and wrap in quotes if contains comma
            if (field.Contains(",") || field.Contains("\"") || field.Contains("\n"))
            {
                field = field.Replace("\"", "\"\"");
                return $"\"{field}\"";
            }
            return field;
        }
    }

    public class Invoice
    {
        public int ClaimId { get; set; }
        public string LecturerName { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public DateTime GeneratedDate { get; set; }
        public string InvoiceNumber { get; set; } = string.Empty;
    }
}