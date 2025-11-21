using ClaimsManagementApp.Models;
using System.Collections.Generic;

namespace ClaimsManagementApp.Services
{
    public interface IReportService
    {
        string GenerateMonthlyClaimsReport(List<Claim> claims);
        List<Invoice> GenerateInvoices(List<Claim> claims);
        string GenerateClaimsSummary(List<Claim> claims);
        string GeneratePaymentReport(List<Claim> claims);
    }
}