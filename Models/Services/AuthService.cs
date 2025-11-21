using System.Data.SqlClient;
using Dapper;
using ClaimsManagementApp.Models;

namespace ClaimsManagementApp.Services
{
    public interface IAuthService
    {
        bool Register(User user, string password);
        User Login(string username, string password);
        bool UserExists(string username, string email);
    }

    public class AuthService : IAuthService
    {
        private readonly string _connectionString;

        public AuthService(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
            InitializeDatabase();
            CreateDefaultUsers();
        }

        private void InitializeDatabase()
        {
            try
            {
                var createTableSql = @"
                    IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Users' AND xtype='U')
                    CREATE TABLE Users (
                        Id INT PRIMARY KEY IDENTITY(1,1),
                        Username NVARCHAR(50) UNIQUE NOT NULL,
                        Email NVARCHAR(100) UNIQUE NOT NULL,
                        PasswordHash NVARCHAR(255) NOT NULL,
                        Role NVARCHAR(20) NOT NULL,
                        CreatedAt DATETIME2 DEFAULT GETDATE(),
                        IsActive BIT DEFAULT 1
                    )";

                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Execute(createTableSql);
                    Console.WriteLine("✅ Users table created successfully!");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error creating table: {ex.Message}");
            }
        }

        private void CreateDefaultUsers()
        {
            try
            {
                var defaultUsers = new[]
                {
                    new { Username = "coordinator", Email = "coordinator@university.com", Password = "coord123", Role = "Coordinator" },
                    new { Username = "hr", Email = "hr@university.com", Password = "hr123", Role = "HR" },
                    new { Username = "manager", Email = "manager@university.com", Password = "manager123", Role = "Manager" }
                };

                foreach (var user in defaultUsers)
                {
                    if (!UserExists(user.Username, user.Email))
                    {
                        var passwordHash = BCrypt.Net.BCrypt.HashPassword(user.Password);
                        var sql = @"INSERT INTO Users (Username, Email, PasswordHash, Role) 
                                    VALUES (@Username, @Email, @PasswordHash, @Role)";

                        using (var connection = new SqlConnection(_connectionString))
                        {
                            connection.Execute(sql, new
                            {
                                Username = user.Username,
                                Email = user.Email,
                                PasswordHash = passwordHash,
                                Role = user.Role
                            });
                        }
                    }
                }
                Console.WriteLine("✅ Default users created successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error creating default users: {ex.Message}");
            }
        }

        public bool Register(User user, string password)
        {
            if (UserExists(user.Username, user.Email))
                return false;

            user.Role = "Lecturer";
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(password);

            var sql = @"INSERT INTO Users (Username, Email, PasswordHash, Role) 
                        VALUES (@Username, @Email, @PasswordHash, @Role)";

            using (var connection = new SqlConnection(_connectionString))
            {
                var affectedRows = connection.Execute(sql, user);
                return affectedRows > 0;
            }
        }

        public User Login(string username, string password)
        {
            var sql = "SELECT * FROM Users WHERE Username = @Username AND IsActive = 1";

            using (var connection = new SqlConnection(_connectionString))
            {
                var user = connection.QueryFirstOrDefault<User>(sql, new { Username = username });

                if (user != null && BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
                {
                    return user;
                }
            }
            return null;
        }

        public bool UserExists(string username, string email)
        {
            var sql = "SELECT COUNT(1) FROM Users WHERE Username = @Username OR Email = @Email";

            using (var connection = new SqlConnection(_connectionString))
            {
                var count = connection.ExecuteScalar<int>(sql, new { Username = username, Email = email });
                return count > 0;
            }
        }
    }
}
