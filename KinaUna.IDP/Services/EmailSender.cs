using Microsoft.Extensions.Configuration;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace KinaUna.IDP.Services
{
    public class EmailSender : IEmailSender
    {
        private readonly IConfiguration _configuration;
        public EmailSender(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        public Task SendEmailAsync(string email, string subject, string message)
        {
            SmtpClient client = new SmtpClient("smtp.office365.com");
            client.UseDefaultCredentials = false;
            client.Credentials = new NetworkCredential("support@kinauna.com", _configuration["SupportMailPassword"]);
            client.EnableSsl = true;
            client.Port = 587;

            MailMessage mailMessage = new MailMessage();
            mailMessage.From = new MailAddress("support@kinauna.com", "Support - KinaUna");
            mailMessage.To.Add(email);
            mailMessage.Body = message;
            mailMessage.IsBodyHtml = true;
            mailMessage.Subject = subject;

            try
            {
                client.SendMailAsync(mailMessage);
                return Task.CompletedTask;
            }
            catch
            {
                return Task.FromResult(0);
            }
        }
    }
}
