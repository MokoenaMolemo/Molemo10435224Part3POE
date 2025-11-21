using System.Security.Claims;
using ClaimsManagementApp.Models;
using ClaimsManagementApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClaimsManagementApp.Controllers
{
    [Authorize(Roles = "Lecturer")]
    public class LecturerController : Controller
    {
        private readonly IClaimService _claimService;
        private readonly IFileService _fileService;

        public LecturerController(IClaimService claimService, IFileService fileService)
        {
            _claimService = claimService;
            _fileService = fileService;
        }

        public IActionResult Index()
        {
            // Get claims for the current logged-in lecturer only
            var userId = GetCurrentUserId();

            // Use GetAllClaims and filter by user ID
            var allClaims = _claimService.GetAllClaims();
            var userClaims = allClaims.Where(c => c.UserId == userId).ToList();

            return View(userClaims);
        }

        [HttpGet]
        public IActionResult SubmitClaim()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(10 * 1024 * 1024)]
        public async Task<IActionResult> SubmitClaim(ClaimsManagementApp.Models.Claim claim, List<IFormFile> documents)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Set the current user ID
                    claim.UserId = GetCurrentUserId();

                    // Ensure the lecturer name is set
                    if (string.IsNullOrEmpty(claim.LecturerName))
                    {
                        claim.LecturerName = User.Identity.Name;
                    }

                    // Add the claim to database
                    _claimService.AddClaim(claim);

                    // Handle document uploads
                    if (documents != null && documents.Any())
                    {
                        foreach (var document in documents)
                        {
                            if (document.Length > 0)
                            {
                                // Save the file and get the file path
                                var filePath = await SaveDocumentAsync(document, claim.Id);

                                var supportingDoc = new SupportingDocument
                                {
                                    ClaimId = claim.Id,
                                    FileName = document.FileName,
                                    FilePath = filePath,
                                    UploadDate = DateTime.Now
                                };

                                _claimService.AddDocumentToClaim(claim.Id, supportingDoc);
                            }
                        }
                    }

                    TempData["SuccessMessage"] = "Claim submitted successfully!";
                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = $"An error occurred while submitting your claim: {ex.Message}";
                    return View(claim);
                }
            }

            return View(claim);
        }

        [HttpGet]
        public IActionResult TrackClaim(int id)
        {
            var claim = _claimService.GetClaimById(id);
            if (claim == null)
            {
                return NotFound();
            }

            // Ensure the current user can only access their own claims
            var userId = GetCurrentUserId();
            if (claim.UserId != userId)
            {
                return Forbid();
            }

            return View(claim);
        }

        [HttpGet]
        public IActionResult ClaimDetails(int id)
        {
            var claim = _claimService.GetClaimById(id);
            if (claim == null)
            {
                return NotFound();
            }

            // Ensure the current user can only access their own claims
            var userId = GetCurrentUserId();
            if (claim.UserId != userId)
            {
                return Forbid();
            }

            return View(claim);
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst("UserId");
            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId))
            {
                return userId;
            }

            // Fallback
            var username = User.Identity.Name;
            if (!string.IsNullOrEmpty(username))
            {
                return Math.Abs(username.GetHashCode());
            }

            throw new InvalidOperationException("Unable to determine current user ID.");
        }

        private async Task<string> SaveDocumentAsync(IFormFile file, int claimId)
        {
            // Create uploads directory if it doesn't exist
            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "claims", claimId.ToString());
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            // Generate unique file name to prevent overwrites
            var uniqueFileName = Guid.NewGuid().ToString() + "_" + file.FileName;
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(fileStream);
            }

            return filePath;
        }
    }
}