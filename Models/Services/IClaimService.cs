using ClaimsManagementApp.Models;
using System.Collections.Generic;

namespace ClaimsManagementApp.Services
{
    public interface IClaimService
    {
        List<Claim> GetAllClaims();
        Claim? GetClaimById(int id);
        void AddClaim(Claim claim);
        void UpdateClaimStatus(int claimId, ClaimStatus status);
        List<Claim> GetClaimsByStatus(ClaimStatus status);
        void AddDocumentToClaim(int claimId, SupportingDocument document);
        SupportingDocument? GetDocument(int documentId);

        // Settlement methods
        void SettleClaim(int claimId, string settlementNotes, string paymentReference, string processedBy);
        List<Claim> GetClaimsReadyForSettlement();
        List<Claim> GetSettledClaims();
    }
}