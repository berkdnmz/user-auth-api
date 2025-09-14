using System.ComponentModel.DataAnnotations;

namespace UserAuthApi.Models
{
    public class UserRegisterDto
    {
        [Required]
        public string Username { get; set; } = string.Empty;

        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required, MinLength(6)]
        public string Password { get; set; } = string.Empty;

        public string Role { get; set; } = "User"; // Varsayılan rol
    }
}