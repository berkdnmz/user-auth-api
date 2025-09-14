using System.Collections.Generic;

namespace UserAuthApi.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;

        public string Role { get; set; } = "User";

        // RefreshTokens ilişkisi (opsiyonel)
        public List<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    }
}