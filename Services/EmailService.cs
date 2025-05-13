using Microsoft.Extensions.Configuration;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

public class EmailService
{
    private readonly IConfiguration _config;

    public EmailService(IConfiguration config)
    {
        _config = config;
    }

    public async Task SendConfirmationEmailAsync(string toEmail, string userName, string confirmationToken)
    {
        var smtpHost = _config["Email:SmtpHost"];
        var smtpPort = int.Parse(_config["Email:SmtpPort"]);
        var smtpUser = _config["Email:SmtpUser"];
        var smtpPass = _config["Email:SmtpPass"];
        var fromEmail = _config["Email:From"];
        var frontendUrl = _config["Email:ConfirmationBaseUrl"]; // ex: https://tuapp.com/api/Auth/confirm

        var confirmLink = $"{frontendUrl}?token={confirmationToken}";

        var subject = "Confirma tu cuenta";
        var body = $@"
        <html>
          <body style='font-family: Arial, sans-serif; width:100%'>
            <div style='width:570px; margin:0 auto'> 
              <h2>¡Hola, {userName}!</h2>
            <p>Gracias por registrarte. Para activar tu cuenta, haz clic en el botón de abajo:</p>
            <p style='text-align:center; margin: 30px 0;'>
              <a href='{confirmLink}' style='
                background-color: #0d6efd;
                color: white;
                padding: 12px 20px;
                text-decoration: none;
                border-radius: 5px;
                font-weight: bold;'>Confirmar cuenta</a>
            </p>
            <p>O copia y pega este enlace en tu navegador:</p>
            <p><a href='{confirmLink}'>{confirmLink}</a></p>
            <hr />
            <small>Si no solicitaste esta cuenta, puedes ignorar este mensaje.</small>
            </div>
          </body>
        </html>";

        using var client = new SmtpClient(smtpHost, smtpPort)
        {
            Credentials = new NetworkCredential(smtpUser, smtpPass),
            EnableSsl = true
        };

        var message = new MailMessage(fromEmail, toEmail, subject, body)
        {
            IsBodyHtml = true
        };

        await client.SendMailAsync(message);
    }
}
