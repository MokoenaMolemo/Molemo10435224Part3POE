using ClaimsManagementApp.Models;
using ClaimsManagementApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ClaimsManagementApp.Controllers
{
    [Authorize(Roles = "HR")]
    public class HRController : Controller
    {
        private readonly IClaimService _claimService;
        private readonly IReportService _reportService;

        public HRController(IClaimService claimService, IReportService reportService)
        {
            _claimService = claimService;
            _reportService = reportService;
        }

        public IActionResult Index()
        {
            return RedirectToAction("Dashboard");
        }

        public IActionResult Dashboard()
        {
            var claims = _claimService.GetAllClaims();
            var approvedClaims = claims.Where(c => c.Status == ClaimStatus.ApprovedByManager).ToList();
            var settledClaims = claims.Where(c => c.Status == ClaimStatus.Settled).ToList();

            var viewModel = new HRDashboardViewModel
            {
                TotalClaims = claims.Count,
                ApprovedClaims = approvedClaims.Count,
                PendingSettlement = approvedClaims.Count,
                SettledClaims = settledClaims.Count,
                TotalAmount = settledClaims.Sum(c => c.TotalAmount),
                PendingAmount = approvedClaims.Sum(c => c.TotalAmount),
                RecentApprovedClaims = approvedClaims.OrderByDescending(c => c.SubmissionDate).Take(5).ToList(),
                ReadyForSettlement = approvedClaims.OrderByDescending(c => c.SubmissionDate).Take(10).ToList()
            };

            return View(viewModel);
        }

        public IActionResult SettlementQueue()
        {
            var readyForSettlement = _claimService.GetClaimsReadyForSettlement();
            return View(readyForSettlement);
        }

        [HttpGet]
        public IActionResult SettleClaim(int id)
        {
            var claim = _claimService.GetClaimById(id);
            if (claim == null || claim.Status != ClaimStatus.ApprovedByManager)
            {
                TempData["ErrorMessage"] = "Claim not found or not ready for settlement.";
                return RedirectToAction("SettlementQueue");
            }

            var viewModel = new SettlementViewModel
            {
                Claim = claim,
                SettlementRequest = new SettlementRequest { ClaimId = id }
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SettleClaim(SettlementViewModel model)
        {
            // COMPLETELY bypass ModelState validation by clearing all errors
            ModelState.Clear();

            // Manually validate only the SettlementRequest fields we care about
            var errors = new List<string>();

            // Validate Payment Reference
            if (string.IsNullOrWhiteSpace(model.SettlementRequest.PaymentReference))
            {
                errors.Add("Payment reference is required");
            }
            else if (model.SettlementRequest.PaymentReference.Trim().Length < 3)
            {
                errors.Add("Payment reference must be at least 3 characters long");
            }

            // Validate Settlement Notes
            if (string.IsNullOrWhiteSpace(model.SettlementRequest.SettlementNotes))
            {
                errors.Add("Settlement notes are required");
            }
            else if (model.SettlementRequest.SettlementNotes.Trim().Length < 10)
            {
                errors.Add("Settlement notes must be at least 10 characters long");
            }

            // If validation passes, process the settlement
            if (!errors.Any())
            {
                try
                {
                    _claimService.SettleClaim(
                        model.SettlementRequest.ClaimId,
                        model.SettlementRequest.SettlementNotes.Trim(),
                        model.SettlementRequest.PaymentReference.Trim(),
                        User.Identity?.Name ?? "HR User"
                    );

                    TempData["SuccessMessage"] = $"Claim #{model.SettlementRequest.ClaimId} has been settled successfully!";
                    return RedirectToAction("SettlementQueue");
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = $"Error settling claim: {ex.Message}";
                }
            }
            else
            {
                TempData["ErrorMessage"] = $"Please fix the following errors: {string.Join(", ", errors)}";
            }

            // If we get here, there was an error - reload the claim data
            model.Claim = _claimService.GetClaimById(model.SettlementRequest.ClaimId);
            return View(model);
        }

        public IActionResult SettledClaims()
        {
            var settledClaims = _claimService.GetSettledClaims();
            return View(settledClaims);
        }

        public IActionResult GeneratePaymentReport()
        {
            var settledClaims = _claimService.GetSettledClaims();
            var report = _reportService.GeneratePaymentReport(settledClaims);

            return File(System.Text.Encoding.UTF8.GetBytes(report), "text/csv", $"Payment_Report_{DateTime.Now:yyyyMMdd}.csv");
        }

        public IActionResult Analytics()
        {
            var claims = _claimService.GetAllClaims();

            var analytics = new HRAnalyticsViewModel
            {
                TotalClaims = claims.Count,
                TotalAmount = claims.Where(c => c.Status == ClaimStatus.ApprovedByManager || c.Status == ClaimStatus.Settled).Sum(c => c.TotalAmount),
                AverageClaimAmount = claims.Where(c => c.Status == ClaimStatus.ApprovedByManager || c.Status == ClaimStatus.Settled).Average(c => c.TotalAmount),
                ClaimsByStatus = claims.GroupBy(c => c.Status)
                    .ToDictionary(g => g.Key.ToString(), g => g.Count()),
                MonthlyTrends = GetMonthlyTrends(claims)
            };

            return View(analytics);
        }

        public IActionResult MonthlyReport()
        {
            var claims = _claimService.GetAllClaims();
            var report = _reportService.GenerateMonthlyClaimsReport(claims);

            return File(System.Text.Encoding.UTF8.GetBytes(report), "text/csv", $"Monthly_Claims_Report_{DateTime.Now:yyyyMMdd}.csv");
        }

        public IActionResult ClaimsSummary()
        {
            var claims = _claimService.GetAllClaims();
            var summary = _reportService.GenerateClaimsSummary(claims);

            return Content(summary, "text/plain");
        }

        public IActionResult Invoices()
        {
            var approvedClaims = _claimService.GetClaimsByStatus(ClaimStatus.ApprovedByManager);
            var invoices = _reportService.GenerateInvoices(approvedClaims);

            return View(invoices);
        }

        private Dictionary<string, int> GetMonthlyTrends(List<Claim> claims)
        {
            return claims
                .GroupBy(c => c.SubmissionDate.ToString("yyyy-MM"))
                .OrderBy(g => g.Key)
                .ToDictionary(g => g.Key, g => g.Count());
        }
    }
}