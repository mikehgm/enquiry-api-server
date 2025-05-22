using Enquiry.API.Dtos;
using Enquiry.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace Enquiry.API.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    [EnableCors("allowCors")]
    public class LoyalClientsController : Controller
    {
        private readonly EnquiryDbContext _context;
        private readonly IConfiguration _config;
        public LoyalClientsController(EnquiryDbContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }

        [Authorize(Roles = "Admin,SuperAdmin")]
        [HttpGet]
        public async Task<IActionResult> GetLoyalClients([FromQuery] int min = 5, [FromQuery] int period = 30)
        {
            var cutoffDate = DateTime.Now.AddDays(-period);

            var clients = await _context.LoyalClients
                .Where(c => c.TotalEnquiries >= min && c.LastInteraction >= cutoffDate)
                .OrderByDescending(c => c.TotalEnquiries)
                .Select(client => new
                {
                    client.ClientId,
                    client.Name,
                    client.Email,
                    client.Phone,
                    client.CreatedAt,
                    client.TotalEnquiries,
                    client.LastInteraction,
                    HasPromotionSent = _context.PromotionsSent.Any(p => p.ClientId == client.ClientId)
                })
                .ToListAsync();

            return Ok(clients);
        }

        [HttpPost("send-promotion")]
        [RequestSizeLimit(10 * 1024 * 1024)]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<IActionResult> SendPromotion([FromForm] SendPromotionDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Method))
                return BadRequest("Se debe especificar el método (email o whatsapp)");

            var method = dto.Method.ToLower();
            if (method != "email" && method != "whatsapp")
                return BadRequest("Método inválido. Usa 'email' o 'whatsapp'.");

            // Validación de duplicado en las últimas 24 horas
            var recentCutoff = DateTime.Now.AddHours(-24);
            bool alreadySent = await _context.PromotionsSent
                .AnyAsync(p => p.ClientId == dto.ClientId && p.Method == method && p.CreatedAt >= recentCutoff);

            if (alreadySent)
                return Conflict(new { message = "Ya se envió una promoción a este cliente recientemente por este método." });

            string? imageUrlToSave = null;

            // EMAIL
            if (method == "email")
            {
                if (string.IsNullOrWhiteSpace(dto.ToEmail))
                    return BadRequest("El correo del cliente es requerido para enviar por email.");

                var smtpHost = _config["Email:SmtpHost"];
                var smtpPort = int.Parse(_config["Email:SmtpPort"]);
                var smtpUser = _config["Email:SmtpUser"];
                var smtpPass = _config["Email:SmtpPass"];
                var fromEmail = _config["Email:From"];

                using var client = new SmtpClient(smtpHost, smtpPort)
                {
                    Credentials = new NetworkCredential(smtpUser, smtpPass),
                    EnableSsl = true
                };

                var message = new MailMessage(fromEmail, dto.ToEmail)
                {
                    Subject = dto.Subject ?? "Promoción exclusiva para ti",
                    Body = dto.Message,
                    IsBodyHtml = true
                };

                if (dto.Image != null && dto.Image.Length > 0)
                {
                    var uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "promos");
                    Directory.CreateDirectory(uploadsPath);

                    var fileName = $"{Guid.NewGuid()}_{Path.GetFileName(dto.Image.FileName)}";
                    var filePath = Path.Combine(uploadsPath, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await dto.Image.CopyToAsync(stream);
                    }

                    imageUrlToSave = $"/uploads/promos/{fileName}";
                }

                await client.SendMailAsync(message);
            }

            // WHATSAPP (solo se registra, no se envía desde backend)
            else if (method == "whatsapp")
            {
                if (string.IsNullOrWhiteSpace(dto.ToPhone))
                    return BadRequest("El número de teléfono es requerido para enviar por WhatsApp.");
            }

            // Registro del envío
            _context.PromotionsSent.Add(new PromotionSent
            {
                ClientId = dto.ClientId,
                Method = method,
                Message = dto.Message,
                ImageUrl = imageUrlToSave,
                CreatedAt = DateTime.Now
            });

            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Promoción registrada con éxito." });
        }



        [HttpGet("getPromotionHistory/{clientId}")]
        public async Task<IActionResult> GetPromotionHistory(int clientId)
        {
            var records = await _context.PromotionsSent
                .Where(p => p.ClientId == clientId)
                .OrderByDescending(p => p.CreatedAt)
                .Select(p => new
                {
                    p.Method,
                    p.Message,
                    p.ImageUrl,
                    p.CreatedAt
                })
                .ToListAsync();

            return Ok(records);
        }


    }
}
