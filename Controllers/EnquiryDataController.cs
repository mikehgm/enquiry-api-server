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
        public EnquiryDataController(EnquiryDbContext context)
        {
            _context = context;
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

        [HttpGet("GetAllEnquiries")]
        public List<EnquiryModel> GetAllEnquiries()
        {
            var list = _context.Enquiries.ToList();
            return list;
        }

        [HttpGet("GetEnquiryById/{enquiryId}")]
        public ActionResult<EnquiryModel?> GetEnquiryById(int enquiryId)
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
                createdBy = User.FindFirst(ClaimTypes.Email)?.Value ?? "Unknown",
                createdDate = DateTime.UtcNow,
                folio = await GenerateFolioAsync()
            };

            _context.Enquiries.Add(enquiry);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Enquiry creado con éxito", folio = enquiry.folio });
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
            enquiry.updatedBy = User.FindFirst(ClaimTypes.Email)?.Value ?? "Unknown";
            enquiry.updatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Enquiry actualizado correctamente" });
        }

        [HttpDelete("DeleteEnquiry/{enquiryId}")]
        public bool? DeleteEnquiry(int enquiryId)
        {
            var enquiry = _context.Enquiries.SingleOrDefault(m => m.enquiryId == enquiryId);
            if (enquiry == null)
            {
                return false;
            }

            _context.Enquiries.Remove(enquiry);
            _context.SaveChanges();
            return true;
        }

        private async Task<string> GenerateFolioAsync()
        {
            var today = DateTime.UtcNow.ToString("yyyyMMdd");
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
