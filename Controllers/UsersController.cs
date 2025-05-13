using Enquiry.API.Dtos;
using Enquiry.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Enquiry.API.Controllers
{
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    [ApiController]
    [EnableCors("allowCors")]

    public class UsersController : ControllerBase
    {

        private readonly EnquiryDbContext _context;
        private readonly EmailService _emailService;


        public UsersController(EnquiryDbContext context, EmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        [HttpGet("GetAllUsers")]
        public IActionResult GetAllUsers()
        {
            var users = _context.Users
                .Select(u => new
                {
                    u.UserId,
                    u.FullName,
                    u.Email,
                    u.Role,
                    u.IsConfirmed,
                    u.CreatedAt
                })
                .ToList();

            return Ok(users);
        }


        [HttpPost("RegisterUser")]
        public async Task<IActionResult> CreateUser(UserRegisterDto dto)
        {
            if (await _context.Users.AnyAsync(u => u.Email == dto.Email))
                return BadRequest("Email already registered");

            var token = Guid.NewGuid().ToString();
            var user = new User
            {
                Email = dto.Email,
                FullName = dto.FullName,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("123456"),
                Role = dto.Role ?? "User",
                IsConfirmed = false,
                MustChangePassword = false,
                ConfirmationToken = token
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Simular envío de correo
            Console.WriteLine($"🔗 Confirm account: {_context.Entry(user).Entity.ConfirmationToken}");

            try
            {
                await _emailService.SendConfirmationEmailAsync(
                    user.Email,
                    user.FullName,
                    user.ConfirmationToken!
                );
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Usuario creado pero ocurrió un error al enviar el correo.", error = ex.Message });
            }


            return Ok(new { message = "Usuario registrado y correo enviado con éxito." });

        }

        [HttpPut("UpdateUser/{id}")]
        public async Task<IActionResult> UpdateUser(int id, [FromBody] UpdateUserDto dto)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();

            user.FullName = dto.FullName;
            user.Role = dto.Role;
            user.IsConfirmed = dto.IsConfirmed;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "User updated successfully",
                userId = user.UserId
            });
        }

        [HttpDelete("DeleteUser/{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            return NoContent(); // Código 204
        }
    }
}
