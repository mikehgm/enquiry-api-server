using Enquiry.API.Dtos;
using Enquiry.API.Hubs;
using Enquiry.API.Models;
using Enquiry.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace Enquiry.API.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    [EnableCors("allowCors")]
    public class EnquiryDataController : ControllerBase
    {
        private readonly EnquiryDbContext _context;
        private readonly IHubContext<EnquiryHub> _hub;
        private readonly EmailService _emailService;

        public EnquiryDataController(EnquiryDbContext context, IHubContext<EnquiryHub> hub, EmailService emailService)
        {
            _context = context;
            _hub = hub;
            _emailService = emailService;
        }

        [HttpGet("GetAllStatus")]
        public List<EnquiryStatus> GetEnquiryStatus()
        {
            var list = _context.EnquiryStatuses.ToList();
            return list;
        }

        [HttpGet("GetAllTypes")]
        public List<EnquiryType> GetAllTypes()
        {
            var list = _context.EnquiryTypes.ToList();
            return list;
        }

        [HttpGet("GetEnquiries")]
        public async Task<IActionResult> GetEnquiries()
        {
            var enquiries = await _context.Enquiries
                .Where(e => !e.isArchived)
                .OrderByDescending(e => e.createdDate)
                .ToListAsync();

            return Ok(enquiries);
        }

        [HttpGet("GetAllEnquiries")]
        [Authorize(Roles = "Admin,SuperAdmin")]

        public async Task<IActionResult> GetAllEnquiries()
        {
            var allEnquiries = await _context.Enquiries
                .OrderByDescending(e => e.createdDate)
                .ToListAsync();

            return Ok(allEnquiries);
        }

        [HttpGet("GetEnquiryById/{enquiryId}")]
        public ActionResult<EnquiryModel> GetEnquiryById(int enquiryId)
        {
            var existingEnquiry = _context.Enquiries.SingleOrDefault(m => m.enquiryId == enquiryId);
            if (existingEnquiry == null)
            {
                return null;
            }

            return Ok(existingEnquiry);
        }

        [HttpPost("CreateNewEnquiry")]
        public async Task<IActionResult> CreateNewEnquiry([FromBody] EnquiryDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var enquiry = new EnquiryModel
            {
                enquiryTypeId = dto.EnquiryTypeId,
                enquiryStatusId = dto.EnquiryStatusId,
                customerName = dto.CustomerName,
                phone = dto.Phone,
                email = dto.Email,
                message = dto.Message,
                resolution = dto.Resolution,
                costo = dto.Costo,
                dueDate = TryParseDate(dto.DueDate),
                createdBy = User.FindFirst(ClaimTypes.Name)?.Value ?? "Unknown",
                createdDate = DateTime.Now,
                folio = await GenerateFolioAsync()
            };

            _context.Enquiries.Add(enquiry);
            await _context.SaveChangesAsync();

            // Sincronizar cliente leal
            var client = await _context.LoyalClients
                .FirstOrDefaultAsync(c => c.Email == enquiry.email && c.Phone == enquiry.phone);

            if (client != null)
            {
                client.TotalEnquiries += 1;
                client.LastInteraction = DateTime.Now;
                client.UpdatedAt = DateTime.Now;
            }
            else
            {
                _context.LoyalClients.Add(new LoyalClient
                {
                    Name = enquiry.customerName,
                    Email = enquiry.email,
                    Phone = enquiry.phone,
                    TotalEnquiries = 1,
                    LastInteraction = DateTime.Now,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                });
            }

            await _context.SaveChangesAsync();
            await _hub.Clients.All.SendAsync("EnquiryChanged");


            return Ok(enquiry);
        }

        [HttpPut("UpdateEnquiry")]
        public async Task<IActionResult> UpdateEnquiry([FromBody] EnquiryDto dto)

        {
            if (!dto.EnquiryId.HasValue)
                return BadRequest(new { message = "Enquiry ID requerido para actualizar." });

            var enquiry = await _context.Enquiries.FindAsync(dto.EnquiryId.Value);
            if (enquiry == null)
                return NotFound();

            enquiry.enquiryTypeId = dto.EnquiryTypeId;
            enquiry.enquiryStatusId = dto.EnquiryStatusId;
            enquiry.customerName = dto.CustomerName;
            enquiry.phone = dto.Phone;
            enquiry.email = dto.Email;
            enquiry.message = dto.Message;
            enquiry.resolution = dto.Resolution;
            enquiry.costo = dto.Costo;
            enquiry.dueDate = TryParseDate(dto.DueDate);
            enquiry.updatedBy = User.FindFirst(ClaimTypes.Name)?.Value ?? "Unknown";
            enquiry.updatedAt = DateTime.Now;

            await _context.SaveChangesAsync();
            await _hub.Clients.All.SendAsync("EnquiryChanged");

            return Ok(new { message = "Enquiry actualizado correctamente" });
        }

        [HttpDelete("DeleteEnquiry/{enquiryId}")]
        public async Task<IActionResult> DeleteEnquiry(int enquiryId)
        {
            var enquiry = _context.Enquiries.SingleOrDefault(m => m.enquiryId == enquiryId);
            if (enquiry == null)
            {
                return BadRequest(new { message = "Enquiry ID requerido para actualizar." });
            }

            _context.Enquiries.Remove(enquiry);
            _context.SaveChanges();
            await _hub.Clients.All.SendAsync("EnquiryChanged");
            return Ok(new { message = "Enquiry eliminado correctamente" }); ;
        }

        [HttpGet("GetUserClaims")]
        public IActionResult GetUserClaims()
        {
            var claims = User.Claims.Select(c => new { c.Type, c.Value }).ToList();
            return Ok(claims);
        }

        [HttpPost("SendTicketEmail/{enquiryId}")]
        public async Task<IActionResult> SendTicketEmail(int enquiryId)
        {
            var enquiry = await _context.Enquiries.FindAsync(enquiryId);
            if (enquiry == null)
            {
                return NotFound(new { message = "Enquiry no encontrado." });
            }

            var htmlBody = EmailTemplateGenerator.GenerateTicketHtml(enquiry);
            var subject = $"Tu ticket #{enquiry.folio}";

            try
            {
                await _emailService.SendHtmlEmailAsync(enquiry.email, subject, htmlBody);
                return Ok(new { message = "Correo enviado correctamente." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al enviar el correo.", error = ex.Message });
            }
        }

        [HttpPost("ArchiveEnquiry/{id}")]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<IActionResult> ArchiveEnquiry(int id)
        {
            var enquiry = await _context.Enquiries.FindAsync(id);
            if (enquiry == null)
                return NotFound();

            if (enquiry.enquiryStatusId != 4) // Asegura que esté en Resolved
                return BadRequest("Only resolved enquiries can be archived.");

            enquiry.isArchived = true;
            await _context.SaveChangesAsync();
            await _hub.Clients.All.SendAsync("EnquiryChanged");

            return Ok(new { message = "Enquiry archived successfully" });
        }

        [HttpGet("GetArchivedEnquiries")]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public IActionResult GetArchivedEnquiries()
        {
            var archived = _context.Enquiries
                .Where(e => e.enquiryStatusId == 4 && e.isArchived)
                .OrderByDescending(e => e.createdDate)
                .ToList();

            return Ok(archived);
        }


        private async Task<string> GenerateFolioAsync()
        {
            var today = DateTime.Now.ToString("yyyyMMdd");
            var countToday = await _context.Enquiries
                .CountAsync(e => e.createdDate.Date == DateTime.Now.Date);

            var folio = $"ENQ-{today}-{(countToday + 1).ToString("D4")}";
            return folio;
        }

        private DateTime? TryParseDate(string? dateStr)
        {
            if (DateTime.TryParse(dateStr, out DateTime result))
            {
                return result;
            }
            return null;
        }
    }
}
