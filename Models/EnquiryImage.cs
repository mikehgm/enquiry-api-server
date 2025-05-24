using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System;
using Enquiry.API.Models;

namespace Enquiry.API.Models
{
    [Table("enquiryimage")]
    public class EnquiryImage
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int EnquiryImageId { get; set; }

        [Required]
        public int EnquiryId { get; set; }

        [Required]
        [MaxLength(255)]
        public string FileName { get; set; } = string.Empty;

        [Required]
        public string FilePath { get; set; } = string.Empty;
        public string ThumbnailPath { get; set; } = string.Empty;
        public DateTime UploadedAt { get; set; } = DateTime.Now;

        [ForeignKey("EnquiryId")]
        public Enquiry Enquiry { get; set; } = null!;
    }
}
