using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace UserAuthApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProtectedController : ControllerBase
    {
        [HttpGet("public")]
        public IActionResult GetPublic()
        {
            return Ok(new { message = "Bu endpoint herkese açık!" });
        }

        [HttpGet("user-only")]
        [Authorize]
        public IActionResult GetSecret() 
        {
            var username = User.Identity?.Name;
            return Ok(new { message = $"Welcome {username}, this is a protected resource!" });
        }

        [HttpGet("admin-only")]
        [Authorize(Roles = "Admin")]
        public IActionResult GetAdminOnly()
        {
            var username = User.Identity?.Name;
            return Ok(new { message = $"Welcome Admin {username}!" });
        }

        [HttpGet("user-or-admin")]
        [Authorize(Roles = "User,Admin")]
        public IActionResult GetUserOrAdmin()
        {
            var username = User.Identity?.Name;
            var role = User.FindFirst(ClaimTypes.Role)?.Value;
            return Ok(new { message = $"Welcome {role} {username}!" });

        }
    }
}
