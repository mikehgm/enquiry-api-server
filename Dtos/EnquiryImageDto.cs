using System;

namespace Enquiry.API.Dtos
{
    public class EnquiryImageDto
    {
        public int EnquiryImageId { get; set; }
        public string FileName { get; set; }
        public string FilePath { get; set; }
        public DateTime UploadedAt { get; set; }
    }
}
