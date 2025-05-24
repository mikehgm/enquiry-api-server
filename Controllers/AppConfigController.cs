using Enquiry.API.Hubs;
using Enquiry.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System.Linq;
using System.Threading.Tasks;

namespace Enquiry.API.Controllers
{
    [Route("api/app-config")]
    [ApiController]
    [EnableCors("allowCors")]
    public class AppConfigController : ControllerBase
    {
        private readonly EnquiryDbContext _context;
        private readonly IHubContext<AppConfigHub> _hub;

        public AppConfigController(EnquiryDbContext context, IHubContext<AppConfigHub> hub)
        {
            _context = context;
            _hub = hub;
        }

        [HttpGet]
        public IActionResult GetAll()
        {
            var items = _context.AppConfig
                .Select(c => new {
                    c.Key,
                    Label = string.IsNullOrEmpty(c.Value) ? c.Default : c.Value
                })
                .ToList();

            return Ok(items);
        }

        [Authorize(Roles = "SuperAdmin")]
        [HttpPut("{key}")]
        public async Task<IActionResult> UpdateValue(string key, [FromBody] string newValue)
        {
            var config = _context.AppConfig.Find(key);
            if (config == null) return NotFound();

            config.Value = newValue;
            _context.SaveChanges();
            await _hub.Clients.All.SendAsync("AppConfigChanged");


            return Ok(new { message = "Config updated successfully" });
        }
    }
}
