using Microsoft.AspNetCore.Http;

namespace Enquiry.API.Dtos
{
    public class SendPromotionDto
    {
        public int ClientId { get; set; }
        public string Method { get; set; } = string.Empty; // "Email" o "WhatsApp"
        public string ToEmail { get; set; } = string.Empty;
        public string ToPhone { get; set; } = string.Empty;
        public string Subject { get; set; } = "Promoción exclusiva para ti";
        public string Message { get; set; } = string.Empty;
        public IFormFile? Image { get; set; }
    }
}
