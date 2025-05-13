namespace Enquiry.API.Dtos
{
    public class UserRegisterDto
    {
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public bool IsConfirmed { get; set; }
        public string? Role { get; set; } = "User"; // Admin o User
    }
}
