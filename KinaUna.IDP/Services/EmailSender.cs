using Microsoft.Extensions.Configuration;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace KinaUna.IDP.Services
{
    public class EmailSender(IConfiguration configuration) : IEmailSender
    {
        public Task SendEmailAsync(string email, string subject, string message, string authClient)
        {
            SmtpClient client = new(configuration.GetValue<string>("SmtpServer"))
            {
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(configuration.GetValue<string>("SmtpUserName"), configuration.GetValue<string>("SupportMailPassword")),
                EnableSsl = true,
                Port = 587
            };

            MailMessage mailMessage = new()
            {
                From = new MailAddress(configuration.GetValue<string>("SmtpFrom"), "Support - " + authClient),
                Body = message,
                IsBodyHtml = true,
                Subject = subject
            };
            mailMessage.To.Add(email);

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
