using ClaimsManagementApp.Models;
using ClaimsManagementApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClaimsManagementApp.Controllers


{
    [Authorize(Roles = "Manager")]
    public class ManagerController : Controller
    {
        private readonly IClaimService _claimService;
        private readonly IFileService _fileService;

        public ManagerController(IClaimService claimService, IFileService fileService)
        {
            _claimService = claimService;
            _fileService = fileService;
        }

        public IActionResult Index()
        {
            return RedirectToAction("PendingClaims");
        }

        public IActionResult PendingClaims()
        {
            // Manager sees claims that have been approved by coordinator
            var pendingClaims = _claimService.GetClaimsByStatus(ClaimStatus.ApprovedByCoordinator);
            return View(pendingClaims);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ApproveClaim(int claimId)
        {
            try
            {
                _claimService.UpdateClaimStatus(claimId, ClaimStatus.ApprovedByManager);
                TempData["SuccessMessage"] = $"Claim #{claimId} has been approved successfully!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error approving claim: {ex.Message}";
            }

            return RedirectToAction("PendingClaims");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RejectClaim(int claimId)
        {
            try
            {
                _claimService.UpdateClaimStatus(claimId, ClaimStatus.RejectedByManager);
                TempData["SuccessMessage"] = $"Claim #{claimId} has been rejected.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error rejecting claim: {ex.Message}";
            }

            return RedirectToAction("PendingClaims");
        }

        public async Task<IActionResult> DownloadDocument(int documentId)
        {
            try
            {
                var document = _claimService.GetDocument(documentId);
                if (document == null)
                {
                    TempData["ErrorMessage"] = "Document not found.";
                    return RedirectToAction("PendingClaims");
                }

                var (content, contentType, fileName) = await _fileService.GetFileAsync(document.StoredFileName);
                return File(content, contentType, document.FileName);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error downloading file: {ex.Message}";
                return RedirectToAction("PendingClaims");
            }
        }

        public IActionResult AllClaims()
        {
            var allClaims = _claimService.GetAllClaims();
            return View(allClaims);
        }
    }
}