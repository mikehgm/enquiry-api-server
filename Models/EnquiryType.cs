using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Enquiry.API.Models
{
    [Table("enquirytype")]
    public class EnquiryType
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int typeId { get; set; }
        public string typeName { get; set; } = string.Empty;
    }
}
