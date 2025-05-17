using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System;

namespace Enquiry.API.Models
{
    [Table("enquiry")]
    public class EnquiryModel
    {

        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int enquiryId { get; set; }
        public int enquiryTypeId { get; set; }
        public int enquiryStatusId { get; set; }
        public string customerName { get; set; } = string.Empty;
        public string phone { get; set; } = string.Empty;
        public string email { get; set; } = string.Empty;
        public string message { get; set; } = string.Empty;
        public DateTime createdDate { get; set; } = DateTime.Now;
        public string resolution { get; set; } = string.Empty;
        public string? createdBy { get; set; }
        public string? updatedBy { get; set; }
        public DateTime? updatedAt { get; set; } = DateTime.Now;
        public string? folio { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal? costo { get; set; }
        public DateTime? dueDate { get; set; }
        public bool isArchived { get; set; } = false;


    }
}
