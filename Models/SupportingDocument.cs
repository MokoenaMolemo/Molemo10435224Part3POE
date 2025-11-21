using System;
using System.ComponentModel.DataAnnotations;

namespace ClaimsManagementApp.Models
{
    public class SupportingDocument
    {
        public int Id { get; set; }
        public int ClaimId { get; set; }

        [Required]
        public string FileName { get; set; } = string.Empty;

        [Required]
        public string StoredFileName { get; set; } = string.Empty;

        public string ContentType { get; set; } = string.Empty;

        public long FileSize { get; set; }

        public DateTime UploadDate { get; set; } = DateTime.Now;

        // Alias for StoredFileName to maintain compatibility with existing code
        public string FilePath
        {
            get => StoredFileName;
            set => StoredFileName = value;
        }
    }
}