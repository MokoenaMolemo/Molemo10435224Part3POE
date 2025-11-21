using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;
using System.IO;

namespace ClaimsManagementApp.Services
{
    public class FileService : IFileService
    {
        private readonly string _uploadPath = "Uploads";
        private readonly long _maxFileSize = 10 * 1024 * 1024; // Changed from 5MB to 10MB
        private readonly string[] _allowedExtensions = { ".pdf", ".docx", ".xlsx" };

        public FileService()
        {
            if (!Directory.Exists(_uploadPath))
                Directory.CreateDirectory(_uploadPath);
        }

        public async Task<string> SaveFileAsync(IFormFile file, string claimId)
        {
            if (!ValidateFile(file))
                throw new InvalidOperationException("File validation failed");

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            var fileName = $"{Guid.NewGuid()}{extension}";
            var filePath = Path.Combine(_uploadPath, fileName);

            // Encrypt file before saving
            await EncryptAndSaveFileAsync(file, filePath);

            return fileName;
        }

        public async Task<(byte[] content, string contentType, string fileName)> GetFileAsync(string storedFileName)
        {
            var filePath = Path.Combine(_uploadPath, storedFileName);
            if (!File.Exists(filePath))
                throw new FileNotFoundException("File not found");

            var decryptedContent = await DecryptFileAsync(filePath);
            var contentType = GetContentType(storedFileName);

            return (decryptedContent, contentType, storedFileName);
        }

        public bool ValidateFile(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return false;

            if (file.Length > _maxFileSize)
                return false;

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!_allowedExtensions.Contains(extension))
                return false;

            return true;
        }

        private async Task EncryptAndSaveFileAsync(IFormFile file, string filePath)
        {
            using var aes = Aes.Create();
            aes.Key = Encoding.UTF8.GetBytes("Your32ByteEncryptionKey123!"); // In production, use secure key storage
            aes.IV = new byte[16]; // Initialization vector

            using var inputStream = file.OpenReadStream();
            using var outputStream = new FileStream(filePath, FileMode.Create);
            using var cryptoStream = new CryptoStream(outputStream, aes.CreateEncryptor(), CryptoStreamMode.Write);

            await inputStream.CopyToAsync(cryptoStream);
        }

        private async Task<byte[]> DecryptFileAsync(string filePath)
        {
            using var aes = Aes.Create();
            aes.Key = Encoding.UTF8.GetBytes("Your32ByteEncryptionKey123!");
            aes.IV = new byte[16];

            using var inputStream = new FileStream(filePath, FileMode.Open);
            using var cryptoStream = new CryptoStream(inputStream, aes.CreateDecryptor(), CryptoStreamMode.Read);
            using var memoryStream = new MemoryStream();

            await cryptoStream.CopyToAsync(memoryStream);
            return memoryStream.ToArray();
        }

        private string GetContentType(string fileName)
        {
            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            return extension switch
            {
                ".pdf" => "application/pdf",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                _ => "application/octet-stream"
            };
        }
    }
}