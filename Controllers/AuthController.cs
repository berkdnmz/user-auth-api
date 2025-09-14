using BCrypt.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using UserAuthApi.Data;
using UserAuthApi.Models;
using UserAuthApi.Services;
using UserAuthApi.Validators;
using System;

namespace UserAuthApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _config;
        private readonly ILoggerService _logger;

        public AuthController(AppDbContext context, IConfiguration config, ILoggerService logger)
        {
            _context = context;
            _config = config;
            _logger = logger;
        }

        [HttpPost("signup")]
        public async Task<IActionResult> Signup([FromBody] UserRegisterDto dto)
        {
            _logger.LogInformation("Signup attempt for username: {Username}", dto.Username);

            try
            {
                var validator = new UserRegisterDtoValidator();
                var validationResult = validator.Validate(dto);

                if (!validationResult.IsValid)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Validation failed",
                        errors = validationResult.ToDictionary()
                    });
                }

                if (await _context.Users.AnyAsync(u => u.Username == dto.Username))
                    return BadRequest(new { message = "Username already exists" });

                if (await _context.Users.AnyAsync(u => u.Email == dto.Email))
                    return BadRequest(new { message = "Email already exists" });

                var user = new User
                {
                    Username = dto.Username,
                    Email = dto.Email,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                    Role = string.IsNullOrEmpty(dto.Role) ? "User" : dto.Role,
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                _logger.LogInformation("User registered successfully: {Username}", dto.Username);
                return Ok(new { message = "User registered successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError("Signup failed for username: {Username}", dto.Username, ex);
                return StatusCode(500, new { message = "An error occurred during registration" });
            }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] UserLoginDto dto)
        {
            // ARTIK MANUEL VALIDATION GEREKMIYOR

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == dto.Username);

            if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
                return Unauthorized("Invalid username or password");

            // Eski refresh token'ları temizle
            var oldTokens = _context.RefreshTokens
                    .Where(rt => rt.UserId == user.Id && (rt.Expires < DateTime.UtcNow || rt.IsRevoked));

            if (oldTokens.Any())
            {
                _context.RefreshTokens.RemoveRange(oldTokens);
                await _context.SaveChangesAsync();
            }

            // JWT Access Token oluştur
            var accessToken = GenerateJwtToken(user);

            // Refresh token oluştur
            var refreshToken = new RefreshToken
            {
                Token = GenerateRefreshToken(),
                Expires = DateTime.UtcNow.AddDays(int.Parse(_config["Jwt:RefreshTokenDays"] ?? "7")),
                UserId = user.Id
            };

            _context.RefreshTokens.Add(refreshToken);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken.Token,
                ExpiresIn = int.Parse(_config["Jwt:AccessTokenMinutes"] ?? "15") * 60
            });
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request)
        {
            // ARTIK MANUEL VALIDATION GEREKMIYOR

            // Refresh tokeni DB'den bul
            var token = await _context.RefreshTokens
                .Include(rt => rt.User)
                .FirstOrDefaultAsync(t => t.Token == request.RefreshToken && !t.IsRevoked);

            if (token == null)
                return Unauthorized("Invalid refresh token");

            if (token.Expires < DateTime.UtcNow)
            {
                _context.RefreshTokens.Remove(token);
                await _context.SaveChangesAsync();
                return Unauthorized("Refresh token expired");
            }

            // Yeni access token oluştur
            var newAccessToken = GenerateJwtToken(token.User);

            return Ok(new
            {
                AccessToken = newAccessToken,
                ExpiresIn = int.Parse(_config["Jwt:AccessTokenMinutes"] ?? "15") * 60
            });
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout([FromBody] LogoutDto dto)
        {
            // Refresh token kontrolü (iş mantığı - validation değil)
            if (string.IsNullOrEmpty(dto.RefreshToken))
                return BadRequest("Refresh token is required");

            var refreshToken = await _context.RefreshTokens
                .FirstOrDefaultAsync(rt => rt.Token == dto.RefreshToken);

            if (refreshToken == null)
                return BadRequest("Invalid refresh token");

            _context.RefreshTokens.Remove(refreshToken);
            await _context.SaveChangesAsync();

            return Ok(new { message = "User logged out successfully" });
        }

        [HttpPost("logout-all")]
        public async Task<IActionResult> LogoutAll([FromBody] LogoutAllDto dto)
        {
            // İş mantığı kontrolleri (validation değil)
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == dto.Username);

            if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
                return Unauthorized("Invalid credentials");

            var userTokens = _context.RefreshTokens.Where(rt => rt.UserId == user.Id);
            _context.RefreshTokens.RemoveRange(userTokens);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Logged out from all devices" });
        }

        private string GenerateJwtToken(User user)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_config["Jwt:Key"] ?? throw new Exception("JWT Key is missing"));

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role)
            };

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddMinutes(int.Parse(_config["Jwt:AccessTokenMinutes"] ?? "15")),
                Issuer = _config["Jwt:Issuer"],
                Audience = _config["Jwt:Audience"],
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        private string GenerateRefreshToken()
        {
            return Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        }
    }
}