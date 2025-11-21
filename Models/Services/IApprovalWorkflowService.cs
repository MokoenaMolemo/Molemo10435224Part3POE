using ClaimsManagementApp.Models;

namespace ClaimsManagementApp.Services
{
    public interface IApprovalWorkflowService
    {
        bool AutoApproveClaim(Claim claim);
        List<string> ValidateClaimAgainstPolicies(Claim claim);
        void AutoEscalateDelayedClaims();
        decimal CalculateRiskScore(Claim claim);
    }
 
    public class ApprovalWorkflowService : IApprovalWorkflowService
    {
        private readonly IConfiguration _configuration;

        public ApprovalWorkflowService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public bool AutoApproveClaim(Claim claim)
        {
            // Auto-approve claims that meet all criteria
            var violations = ValidateClaimAgainstPolicies(claim);

            // Auto-approve if no violations and within safe limits
            if (!violations.Any() && CalculateRiskScore(claim) < 30)
            {
                claim.Status = ClaimStatus.ApprovedByCoordinator;
                return true;
            }

            return false;
        }

        public List<string> ValidateClaimAgainstPolicies(Claim claim)
        {
            var violations = new List<string>();
            var maxHours = _configuration.GetValue<decimal>("ClaimPolicies:MaxHoursPerClaim", 100);
            var maxRate = _configuration.GetValue<decimal>("ClaimPolicies:MaxHourlyRate", 500);
            var maxAmount = _configuration.GetValue<decimal>("ClaimPolicies:MaxTotalAmount", 50000);

            if (claim.HoursWorked > maxHours)
                violations.Add($"Hours worked ({claim.HoursWorked}) exceeds maximum allowed ({maxHours})");

            if (claim.HourlyRate > maxRate)
                violations.Add($"Hourly rate (R{claim.HourlyRate}) exceeds maximum allowed (R{maxRate})");

            if (claim.TotalAmount > maxAmount)
                violations.Add($"Total amount (R{claim.TotalAmount}) exceeds maximum allowed (R{maxAmount})");

            // Check for unusual patterns
            if (claim.HoursWorked > 80)
                violations.Add("High hours detected - requires manual review");

            if (claim.HourlyRate > 450)
                violations.Add("Premium rate detected - requires justification");

            return violations;
        }

        public decimal CalculateRiskScore(Claim claim)
        {
            decimal riskScore = 0;

            // Hours risk (40+ hours = higher risk)
            if (claim.HoursWorked > 40) riskScore += 20;
            if (claim.HoursWorked > 60) riskScore += 30;

            // Rate risk
            if (claim.HourlyRate > 300) riskScore += 15;
            if (claim.HourlyRate > 400) riskScore += 25;

            // Amount risk
            if (claim.TotalAmount > 10000) riskScore += 20;
            if (claim.TotalAmount > 20000) riskScore += 30;

            // Documentation risk
            if (claim.Documents == null || !claim.Documents.Any())
                riskScore += 40;

            return Math.Min(riskScore, 100);
        }

        public void AutoEscalateDelayedClaims()
        {
            // This would typically query the database for delayed claims
            // and escalate them automatically
        }
    }
}