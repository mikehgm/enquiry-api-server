using System.Threading.Tasks;

namespace Enquiry.API.Services
{
    public interface IEmailService
    {
        Task SendHtmlEmailAsync(string to, string subject, string htmlBody);
        Task SendConfirmationEmailAsync(string toEmail, string userName, string confirmationToken);
    }
}
