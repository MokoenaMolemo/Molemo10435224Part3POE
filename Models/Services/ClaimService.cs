using System.Data;
using System.Data.SqlClient;
using Dapper;
using ClaimsManagementApp.Models;

namespace ClaimsManagementApp.Services
{
    public class ClaimService : IClaimService
    {
        private readonly string _connectionString;

        public ClaimService(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
            InitializeDatabase();
        }

        private void InitializeDatabase()
        {
            var createTableSql = @"
                IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Claims' AND xtype='U')
                CREATE TABLE Claims (
                    Id INT PRIMARY KEY IDENTITY(1,1),
                    UserId INT NOT NULL,
                    LecturerName NVARCHAR(100) NOT NULL,
                    HoursWorked DECIMAL(5,2) NOT NULL,
                    HourlyRate DECIMAL(8,2) NOT NULL,
                    TotalAmount DECIMAL(10,2) NOT NULL,
                    AdditionalNotes NVARCHAR(MAX),
                    SubmissionDate DATETIME2 DEFAULT GETDATE(),
                    Status NVARCHAR(50) DEFAULT 'Pending',
                    SettlementDate DATETIME2 NULL,
                    SettlementNotes NVARCHAR(MAX),
                    ProcessedBy NVARCHAR(100),
                    PaymentReference NVARCHAR(100),
                    FOREIGN KEY (UserId) REFERENCES Users(Id)
                )";

            // Create SupportingDocuments table if needed
            var createDocumentsTableSql = @"
                IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='SupportingDocuments' AND xtype='U')
                CREATE TABLE SupportingDocuments (
                    Id INT PRIMARY KEY IDENTITY(1,1),
                    ClaimId INT NOT NULL,
                    FileName NVARCHAR(255) NOT NULL,
                    FilePath NVARCHAR(500) NOT NULL,
                    UploadDate DATETIME2 DEFAULT GETDATE(),
                    FOREIGN KEY (ClaimId) REFERENCES Claims(Id) ON DELETE CASCADE
                )";

            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Execute(createTableSql);
                connection.Execute(createDocumentsTableSql);
            }
        }

        public List<Claim> GetAllClaims()
        {
            var sql = @"
                SELECT c.*, u.Username 
                FROM Claims c 
                INNER JOIN Users u ON c.UserId = u.Id 
                ORDER BY c.SubmissionDate DESC";

            using (var connection = new SqlConnection(_connectionString))
            {
                return connection.Query<Claim>(sql).ToList();
            }
        }

        public Claim? GetClaimById(int id)
        {
            var sql = @"
                SELECT c.*, u.Username 
                FROM Claims c 
                INNER JOIN Users u ON c.UserId = u.Id 
                WHERE c.Id = @Id";

            using (var connection = new SqlConnection(_connectionString))
            {
                return connection.QueryFirstOrDefault<Claim>(sql, new { Id = id });
            }
        }

        public void AddClaim(Claim claim)
        {
            var sql = @"
                INSERT INTO Claims (UserId, LecturerName, HoursWorked, HourlyRate, TotalAmount, AdditionalNotes, Status)
                VALUES (@UserId, @LecturerName, @HoursWorked, @HourlyRate, @TotalAmount, @AdditionalNotes, @Status);
                SELECT CAST(SCOPE_IDENTITY() as int)";

            using (var connection = new SqlConnection(_connectionString))
            {
                var id = connection.QuerySingle<int>(sql, new
                {
                    UserId = claim.UserId,
                    LecturerName = claim.LecturerName,
                    HoursWorked = claim.HoursWorked,
                    HourlyRate = claim.HourlyRate,
                    TotalAmount = claim.TotalAmount,
                    AdditionalNotes = claim.AdditionalNotes,
                    Status = ClaimStatus.Pending.ToString()
                });
                claim.Id = id;
            }
        }

        public void UpdateClaimStatus(int claimId, ClaimStatus status)
        {
            var sql = @"
                UPDATE Claims 
                SET Status = @Status
                WHERE Id = @Id";

            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Execute(sql, new
                {
                    Id = claimId,
                    Status = status.ToString()
                });
            }
        }

        public List<Claim> GetClaimsByStatus(ClaimStatus status)
        {
            var sql = @"
                SELECT c.*, u.Username 
                FROM Claims c 
                INNER JOIN Users u ON c.UserId = u.Id 
                WHERE c.Status = @Status 
                ORDER BY c.SubmissionDate DESC";

            using (var connection = new SqlConnection(_connectionString))
            {
                return connection.Query<Claim>(sql, new { Status = status.ToString() }).ToList();
            }
        }

        public void AddDocumentToClaim(int claimId, SupportingDocument document)
        {
            var sql = @"
                INSERT INTO SupportingDocuments (ClaimId, FileName, FilePath)
                VALUES (@ClaimId, @FileName, @FilePath)";

            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Execute(sql, new
                {
                    ClaimId = claimId,
                    FileName = document.FileName,
                    FilePath = document.FilePath
                });
            }
        }

        public SupportingDocument? GetDocument(int documentId)
        {
            var sql = "SELECT * FROM SupportingDocuments WHERE Id = @Id";

            using (var connection = new SqlConnection(_connectionString))
            {
                return connection.QueryFirstOrDefault<SupportingDocument>(sql, new { Id = documentId });
            }
        }

        // Settlement methods
        public void SettleClaim(int claimId, string settlementNotes, string paymentReference, string processedBy)
        {
            var sql = @"
                UPDATE Claims 
                SET Status = 'Settled', 
                    SettlementDate = GETDATE(),
                    SettlementNotes = @SettlementNotes,
                    PaymentReference = @PaymentReference,
                    ProcessedBy = @ProcessedBy
                WHERE Id = @Id";

            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Execute(sql, new
                {
                    Id = claimId,
                    SettlementNotes = settlementNotes,
                    PaymentReference = paymentReference,
                    ProcessedBy = processedBy
                });
            }
        }

        public List<Claim> GetClaimsReadyForSettlement()
        {
            return GetClaimsByStatus(ClaimStatus.ApprovedByManager);
        }

        public List<Claim> GetSettledClaims()
        {
            return GetClaimsByStatus(ClaimStatus.Settled);
        }

        // Additional helper methods that might be useful
        public List<Claim> GetUserClaims(int userId)
        {
            var sql = @"
                SELECT c.*, u.Username 
                FROM Claims c 
                INNER JOIN Users u ON c.UserId = u.Id 
                WHERE c.UserId = @UserId 
                ORDER BY c.SubmissionDate DESC";

            using (var connection = new SqlConnection(_connectionString))
            {
                return connection.Query<Claim>(sql, new { UserId = userId }).ToList();
            }
        }

        public List<Claim> GetPendingClaims()
        {
            return GetClaimsByStatus(ClaimStatus.Pending);
        }
    }
}