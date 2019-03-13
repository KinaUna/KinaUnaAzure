using Microsoft.Extensions.Configuration;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using KinaUna.Data;

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
            SmtpClient client = new SmtpClient(Constants.SmtpServer);
            client.UseDefaultCredentials = false;
            client.Credentials = new NetworkCredential(Constants.SmtpUsername, _configuration["SupportMailPassword"]);
            client.EnableSsl = true;
            client.Port = 587;

            MailMessage mailMessage = new MailMessage();
            mailMessage.From = new MailAddress(Constants.SmtpFrom, "Support - " + Constants.AppName);
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
