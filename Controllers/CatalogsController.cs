using Enquiry.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Linq;

namespace Enquiry.API.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    [EnableCors("allowCors")]
    public class CatalogsController : ControllerBase
    {

        private readonly EnquiryDbContext _context;

        public CatalogsController(EnquiryDbContext context)
        {
            _context = context;
        }

        [HttpGet("enquiry-status")]
        public IActionResult GetEnquiryStatuses()
        {
            var statuses = _context.EnquiryStatuses
                .OrderBy(s => s.statusId)
                .ToList();

            return Ok(statuses);
        }

        [Authorize(Roles = "Admin,SuperAdmin")]
        [HttpPut("enquiry-status/{id}")]
        public IActionResult UpdateEnquiryStatus(int id, [FromBody] EnquiryStatus updatedStatus)
        {
            var existing = _context.EnquiryStatuses.Find(id);
            if (existing == null)
                return NotFound();

            existing.status = updatedStatus.status;

            _context.SaveChanges();

            return NoContent();
        }

        [HttpGet("enquiry-type")]
        public IActionResult GetEnquiryTypes()
        {
            var types = _context.EnquiryTypes
                .OrderBy(t => t.typeId)
                .ToList();

            return Ok(types);
        }

        [Authorize(Roles = "Admin,SuperAdmin")]
        [HttpPut("enquiry-type/{id}")]
        public IActionResult UpdateEnquiryType(int id, [FromBody] EnquiryType updatedType)
        {
            var existing = _context.EnquiryTypes.Find(id);
            if (existing == null)
                return NotFound();

            existing.typeName = updatedType.typeName;
            _context.SaveChanges();

            return NoContent();
        }

    }
}
