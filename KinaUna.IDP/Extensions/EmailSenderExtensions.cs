using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using KinaUna.IDP.Services;

namespace KinaUna.IDP.Extensions
{
    public static class EmailSenderExtensions
    {
        public static Task SendEmailConfirmationAsync(this IEmailSender emailSender, string email, string link)
        {
            return emailSender.SendEmailAsync(email, "Confirm your email",
                $"Please confirm your account by clicking this link: <a href='{HtmlEncoder.Default.Encode(link)}'>link</a>");
        }

        public static Task SendEmailInviteAsync(this IEmailSender emailSender, string email, string link, string inviter, string password)
        {
            return emailSender.SendEmailAsync(email, "Invitation to Join KinaUna.com",
                $"{inviter} invited you to join KinaUna.com.\r\n<br/><br/>Please confirm your email address by clicking this link: <a href='{HtmlEncoder.Default.Encode(link)}'>link</a>\r\n<br/><br/>Your username is your email address. <br/><br/>Your temporary password is: {password}\r\n<br/><br/><br/>Once you are logged in, you can change your username and password by clicking on your username, then select My Account.<br/><br/><a href='https://kinauna.com'>kinauna.com</a>");
        }
    }
}
