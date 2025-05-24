using System.ComponentModel.DataAnnotations;

namespace Enquiry.API.Dtos
{
    public class EnquiryDto
    {
        public int? EnquiryId { get; set; } 

        [Required]
        public int EnquiryTypeId { get; set; }

        [Required]
        public int EnquiryStatusId { get; set; }

        [Required]
        public string CustomerName { get; set; } = string.Empty;

        [Required]
        public string Phone { get; set; } = string.Empty;

        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Message { get; set; } = string.Empty;

        public string? Resolution { get; set; }

        public string? DueDate { get; set; }  // formato string 'yyyy-MM-dd'

        public decimal? Costo { get; set; }
        public decimal? Anticipo { get; set; }
        public decimal? SaldoPago { get; set; }

        // El folio, usuario y fechas no vienen del cliente
    }

}
