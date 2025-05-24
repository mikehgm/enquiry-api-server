using System;

namespace Enquiry.API.Dtos
{
    public class EnquiryModel
    {
        public int enquiryId { get; set; }
        public int enquiryTypeId { get; set; }
        public string enquiryTypeName { get; set; } = string.Empty;
        public int enquiryStatusId { get; set; }
        public string enquiryStatusName { get; set; } = string.Empty;
        public string customerName { get; set; } = string.Empty;
        public string phone { get; set; } = string.Empty;
        public string email { get; set; } = string.Empty;
        public string message { get; set; } = string.Empty;
        public DateTime createdDate { get; set; }
        public string resolution { get; set; } = string.Empty;
        public string? createdBy { get; set; }
        public string? updatedBy { get; set; }
        public DateTime? updatedAt { get; set; }
        public string? folio { get; set; }
        public decimal? costo { get; set; }
        public DateTime? dueDate { get; set; }
        public bool isArchived { get; set; }
        public decimal? anticipo { get; set; }
        public decimal? saldoPago { get; set; }
    }

}
