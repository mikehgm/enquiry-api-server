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

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> GetLoyalClients([FromQuery] int min = 5, [FromQuery] int period = 30)
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-period);

            var clients = await _context.LoyalClients
                .Where(c => c.TotalEnquiries >= min && c.LastInteraction >= cutoffDate)
                .OrderByDescending(c => c.TotalEnquiries)
                .ToListAsync();

            return Ok(clients);
        }

        [HttpPost("send-promotion")]
        [RequestSizeLimit(10 * 1024 * 1024)]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> SendPromotionEmail([FromForm] SendPromotionDto dto)
        {
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
                Subject = dto.Subject,
                Body = dto.Message,
                IsBodyHtml = true
            };

            if (dto.Image != null && dto.Image.Length > 0)
            {
                var ms = new MemoryStream();
                await dto.Image.CopyToAsync(ms);
                ms.Seek(0, SeekOrigin.Begin); // importante

                var attachment = new Attachment(ms, dto.Image.FileName, dto.Image.ContentType);
                message.Attachments.Add(attachment);
            }

            await client.SendMailAsync(message);

            return Ok(new { success = true, message = "Promoción enviada con éxito." });
        }

    }
}
