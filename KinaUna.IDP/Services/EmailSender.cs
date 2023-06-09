﻿using Microsoft.Extensions.Configuration;
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
        public Task SendEmailAsync(string email, string subject, string message, string authclient)
        {
            SmtpClient client = new SmtpClient(_configuration.GetValue<string>("SmtpServer"));
            client.UseDefaultCredentials = false;
            client.Credentials = new NetworkCredential(_configuration.GetValue<string>("SmtpUserName"), _configuration.GetValue<string>("SupportMailPassword"));
            client.EnableSsl = true;
            client.Port = 587;

            MailMessage mailMessage = new MailMessage();
            mailMessage.From = new MailAddress(_configuration.GetValue<string>("SmtpFrom"), "Support - " + authclient);
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
