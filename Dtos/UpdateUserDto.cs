namespace Enquiry.API.Dtos
{
    public class UpdateUserDto
    {
        public string FullName { get; set; } = string.Empty;
        public string Role { get; set; } = "User";
        public bool IsConfirmed { get; set; }
    }
}
