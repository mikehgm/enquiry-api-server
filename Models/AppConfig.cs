using System.ComponentModel.DataAnnotations;
using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Enquiry.API.Models
{

    [Table("app_config")]
    public class AppConfig
    {
        [Key]
        [MaxLength(100)]
        public string Key { get; set; } = string.Empty;

        public string? Value { get; set; }

        [Required]
        public string Default { get; set; } = string.Empty;

        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }
}
