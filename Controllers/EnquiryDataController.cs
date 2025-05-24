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
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;
using EnquiryEntity = Enquiry.API.Models.Enquiry;

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
            var enquiries = await ProjectToEnquiryModel(
                _context.Enquiries.Where(e => !e.isArchived).OrderByDescending(e => e.createdDate)
            ).ToListAsync();

            return Ok(enquiries);
        }

        [HttpGet("GetAllEnquiries")]
        [Authorize(Roles = "Admin,SuperAdmin")]

        public async Task<IActionResult> GetAllEnquiries()
        {
            var allEnquiries = await ProjectToEnquiryModel(
                _context.Enquiries.OrderByDescending(e => e.createdDate)
            ).ToListAsync();

            return Ok(allEnquiries);
        }

        [HttpGet("GetEnquiryById/{enquiryId}")]
        public ActionResult<EnquiryModel> GetEnquiryById(int enquiryId)
        {

            var existingEnquiry = ProjectToEnquiryModel(_context.Enquiries.Where(e => e.enquiryId == enquiryId)).FirstOrDefault();

            if (existingEnquiry == null)
                return NotFound();

            return Ok(existingEnquiry);

        }

        [HttpPost("CreateNewEnquiry")]
        public async Task<IActionResult> CreateNewEnquiry([FromBody] EnquiryDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var enquiry = new EnquiryEntity
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
                anticipo = dto.Anticipo,
                saldoPago = dto.SaldoPago,
                createdBy = User.FindFirst(ClaimTypes.Name)?.Value ?? "Unknown",
                updatedBy = User.FindFirst(ClaimTypes.Name)?.Value ?? "Unknown",
                createdDate = DateTime.Now,
                updatedAt = DateTime.Now,
                folio = await GenerateFolioAsync()
            };

            _context.Enquiries.Add(enquiry);
            await _context.SaveChangesAsync();

            // Additional logic remains unchanged.
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
            enquiry.anticipo = dto.Anticipo;
            enquiry.saldoPago = dto.SaldoPago;
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
            var enquiry = await ProjectToEnquiryModel(
                _context.Enquiries.Where(e => e.enquiryId == enquiryId)
            ).FirstOrDefaultAsync();

            if (enquiry == null)
            {
                return NotFound(new { message = "Enquiry no encontrado." });
            }

            var config = new AppConfigDto
            {
                CompanyName = GetConfig("ui_company"),
                CompanyAddress = GetConfig("ui_company_address"),
                CompanySucursal = GetConfig("ui_company_sucursal"),
                CompanyThanks = GetConfig("ui_thanks"),
                CompanyInfo = GetConfig("ui_info"),
                CompanyWebsite = GetConfig("ui_url_address"),
                Folio = GetConfig("ui_folio"),
                Cliente = GetConfig("ui_cliente"),
                Telefono = GetConfig("ui_phone"),
                Email = GetConfig("ui_email"),
                Servicio = GetConfig("ui_service_type"),
                Mensaje = GetConfig("ui_message"),
                Costo = GetConfig("ui_costo"),
                Anticipo = GetConfig("ui_anticipo"),
                Saldo = GetConfig("ui_saldo_pago"),
                Entrega = GetConfig("ui_fecha_de_entrega"),
                Atendio = GetConfig("ui_attended"),
                FechaCreacion = GetConfig("ui_date_attended"),
                TicketTitle = GetConfig("ui_enquiry_ticket")

            };

            var htmlBody = EmailTemplateGenerator.GenerateTicketHtml(enquiry, config);
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
        public async Task<IActionResult> GetArchivedEnquiries()
        {
            var archivedEnquiries = await ProjectToEnquiryModel(
                _context.Enquiries
                    .Where(e => e.enquiryStatusId == 4 && e.isArchived)
                    .OrderByDescending(e => e.createdDate)
            ).ToListAsync();

            return Ok(archivedEnquiries);
        }

        [HttpPost("UploadEnquiryImages/{enquiryId}")]
        public async Task<IActionResult> UploadEnquiryImages(int enquiryId)
        {
            var enquiry = await _context.Enquiries.FindAsync(enquiryId);
            if (enquiry == null)
                return NotFound();

            var files = Request.Form.Files;
            if (files == null || files.Count == 0)
                return BadRequest("No files uploaded.");

            var uploadDir = Path.Combine("wwwroot", "uploads", "enquiries", enquiryId.ToString());
            var thumbDir = Path.Combine(uploadDir, "thumbs");

            Directory.CreateDirectory(uploadDir);
            Directory.CreateDirectory(thumbDir);

            foreach (var file in files)
            {
                if (file.Length > 0)
                {
                    var fileExt = Path.GetExtension(file.FileName);
                    var fileName = $"{Guid.NewGuid()}{fileExt}";

                    var filePath = Path.Combine(uploadDir, fileName);
                    var thumbPath = Path.Combine(thumbDir, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await file.CopyToAsync(stream);
                    }

                    // Generar thumbnail
                    ImageHelper.GenerateThumbnail(filePath, thumbPath, 300, 300);

                    // Guardar en DB
                    var image = new EnquiryImage
                    {
                        EnquiryId = enquiryId,
                        FileName = file.FileName,
                        FilePath = $"/uploads/enquiries/{enquiryId}/{fileName}",
                        ThumbnailPath = $"/uploads/enquiries/{enquiryId}/thumbs/{fileName}"
                    };

                    _context.EnquiryImages.Add(image);
                }
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = "Imágenes subidas con thumbnail." });
        }


        [HttpGet("GetImagesByEnquiry/{enquiryId}")]
        public IActionResult GetEnquiryImages(int enquiryId)
        {
            var images = _context.EnquiryImages
                .Where(i => i.EnquiryId == enquiryId)
                .Select(i => new
                {
                    i.EnquiryImageId,
                    i.FileName,
                    i.FilePath,
                    i.ThumbnailPath,
                    i.UploadedAt
                })
                .ToList();

            return Ok(images);
        }


        [HttpDelete("DeleteImage/{imageId}")]
        public async Task<IActionResult> DeleteImage(int imageId)
        {
            var image = await _context.EnquiryImages.FindAsync(imageId);
            if (image == null)
                return NotFound(new { message = "Imagen no encontrada." });

            // Eliminar físicamente la imagen del sistema de archivos
            if (System.IO.File.Exists(image.FilePath))
            {
                try
                {
                    System.IO.File.Delete(image.FilePath); 
                }
                catch (Exception ex)
                {
                    return StatusCode(500, new { message = "Error al eliminar el archivo.", error = ex.Message });
                }
            }

            // Eliminar registro de la base de datos
            _context.EnquiryImages.Remove(image);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Imagen eliminada correctamente." });
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

        private string GetConfig(string key)
        {
            return _context.AppConfig
                    .AsEnumerable()
                    .FirstOrDefault(c => c.Key.Trim() == key.Trim())
                    ?.Value?.Trim() ?? string.Empty;
        }

        private IQueryable<EnquiryModel> ProjectToEnquiryModel(IQueryable<Enquiry.API.Models.Enquiry> baseQuery)
        {
            return from e in baseQuery
                   join t in _context.EnquiryTypes on e.enquiryTypeId equals t.typeId
                   join s in _context.EnquiryStatuses on e.enquiryStatusId equals s.statusId
                   select new EnquiryModel
                   {
                       enquiryId = e.enquiryId,
                       enquiryTypeId = e.enquiryTypeId,
                       enquiryTypeName = t.typeName,
                       enquiryStatusId = e.enquiryStatusId,
                       enquiryStatusName = s.status,
                       customerName = e.customerName,
                       phone = e.phone,
                       email = e.email,
                       message = e.message,
                       resolution = e.resolution,
                       createdDate = e.createdDate,
                       costo = e.costo,
                       anticipo = e.anticipo,
                       saldoPago = e.saldoPago,
                       dueDate = e.dueDate,
                       folio = e.folio,
                       createdBy = e.createdBy,
                       updatedBy = e.updatedBy,
                       updatedAt = e.updatedAt,
                       isArchived = e.isArchived
                   };
        }

    }
}
