using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ClaimsManagementApp.Models
{
    public class Claim
    {
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required(ErrorMessage = "Lecturer name is required")]
        public string LecturerName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Hours worked is required")]
        [Range(0.5, 100, ErrorMessage = "Hours worked must be between 0.5 and 100")]
        public decimal HoursWorked { get; set; }

        [Required(ErrorMessage = "Hourly rate is required")]
        [Range(50, 500, ErrorMessage = "Hourly rate must be between 50 and 500")]
        public decimal HourlyRate { get; set; }

        public decimal TotalAmount => HoursWorked * HourlyRate;

        public string AdditionalNotes { get; set; } = string.Empty;

        public DateTime SubmissionDate { get; set; } = DateTime.Now;

        public ClaimStatus Status { get; set; } = ClaimStatus.Pending;

        public List<SupportingDocument> Documents { get; set; } = new List<SupportingDocument>();

        // Settlement properties
        public DateTime? SettlementDate { get; set; }
        public string? SettlementNotes { get; set; }
        public string? ProcessedBy { get; set; }
        public string? PaymentReference { get; set; }

        // Navigation property for user
        public User? User { get; set; }
        public string? Username { get; set; } // For display purposes
    }

    public enum ClaimStatus
    {
        Pending,
        ApprovedByCoordinator,
        RejectedByCoordinator,
        ApprovedByManager,
        RejectedByManager,
        Settled
    }
}