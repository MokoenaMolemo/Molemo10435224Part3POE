using Microsoft.AspNetCore.Http;

namespace ClaimsManagementApp.Services
{
    public interface IFileService
    {
        Task<string> SaveFileAsync(IFormFile file, string claimId);
        Task<(byte[] content, string contentType, string fileName)> GetFileAsync(string storedFileName);
        bool ValidateFile(IFormFile file);
    }
}