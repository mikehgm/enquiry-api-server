using Enquiry.API.Dtos;
using Enquiry.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
namespace Enquiry.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly EnquiryDbContext _context;
        private readonly IConfiguration _config;

        public AuthController(EnquiryDbContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }

        [HttpGet("confirm")]
        public async Task<IActionResult> Confirm(string token)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.ConfirmationToken == token);

            if (user == null) return BadRequest("Invalid token");

            user.IsConfirmed = true;
            user.ConfirmationToken = null;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Email confirmed successfully." });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] UserLoginDto dto)
        {
            var user = await _context.Users
                .Include(u => u.RefreshTokens) // Asegúrate de incluir esto si usas EF
                .FirstOrDefaultAsync(u => u.Email == dto.Email);

            if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
                return Unauthorized("Invalid credentials");

            if (!user.IsConfirmed)
                return Unauthorized("Account not confirmed");

            var accessToken = GenerateJwtToken(user);
            var refreshToken = GenerateRefreshToken();

            // Reemplaza la línea problemática con el siguiente código:
            user.RefreshTokens = user.RefreshTokens
                .Where(t => !(t.IsRevoked || t.Expires < DateTime.UtcNow))
                .ToList();

            // Guarda nuevo refresh token
            user.RefreshTokens.Add(new RefreshToken
            {
                Token = refreshToken,
                Expires = DateTime.UtcNow.AddDays(7),
                IsUsed = false,
                IsRevoked = false
            });

            await _context.SaveChangesAsync();

            return Ok(new
            {
                access_token = accessToken,
                refresh_token = refreshToken,
                mustChangePassword = user.MustChangePassword
            });
        }


        private string GenerateJwtToken(User user)
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Name, user.FullName),
                new Claim(ClaimTypes.Role, user.Role)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var jwt = new JwtSecurityToken(
                claims: claims,
                expires: DateTime.UtcNow.AddHours(2),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(jwt);
        }

        private string GenerateRefreshToken()
        {
            return Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        }

        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.NewPassword) || request.NewPassword.Length < 6)
            {
                return BadRequest(new { message = "La nueva contraseña es inválida o demasiado corta." });
            }

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var user = await _context.Users.FindAsync(int.Parse(userId));
            if (user == null) return NotFound();

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            user.MustChangePassword = true;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Contraseña actualizada correctamente." });
        }

        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequestDto request)
        {
            var tokenFromDb = await _context.RefreshTokens
                .Include(t => t.User)
                .FirstOrDefaultAsync(t => t.Token == request.RefreshToken);

            if (tokenFromDb == null ||
                tokenFromDb.IsUsed ||
                tokenFromDb.IsRevoked ||
                tokenFromDb.Expires < DateTime.UtcNow)
            {
                return Unauthorized("Invalid or expired refresh token.");
            }

            // Marcar como usado
            tokenFromDb.IsUsed = true;

            var user = tokenFromDb.User;

            // Generar nuevos tokens
            var newAccessToken = GenerateJwtToken(user);
            var newRefreshToken = GenerateRefreshToken();

            user.RefreshTokens.Add(new RefreshToken
            {
                Token = newRefreshToken,
                Expires = DateTime.UtcNow.AddDays(7),
                IsUsed = false,
                IsRevoked = false
            });

            await _context.SaveChangesAsync();

            return Ok(new
            {
                access_token = newAccessToken,
                refresh_token = newRefreshToken
            });
        }

    }

}
