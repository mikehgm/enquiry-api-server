using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System;

namespace Enquiry.API.Models
{
    public class PromotionSent
    {
        [Key]
        public int PromotionId { get; set; }

        public int ClientId { get; set; }

        [Required]
        public string Method { get; set; } = string.Empty;

        [Required]
        public string Message { get; set; } = string.Empty;

        public string? ImageUrl { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [ForeignKey("ClientId")]
        public LoyalClient? Client { get; set; }
    }
}
