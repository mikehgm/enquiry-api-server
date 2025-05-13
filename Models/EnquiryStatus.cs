using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Enquiry.API.Models
{
    [Table("enquirystatus")]
    public class EnquiryStatus
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int statusId { get; set; }
        public string status { get; set; } = string.Empty;
    }
}
