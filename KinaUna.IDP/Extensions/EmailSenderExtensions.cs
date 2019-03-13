using System.Text.Encodings.Web;
using System.Threading.Tasks;
using KinaUna.Data;
using KinaUna.IDP.Services;

namespace KinaUna.IDP.Extensions
{
    public static class EmailSenderExtensions
    {
        public static Task SendEmailConfirmationAsync(this IEmailSender emailSender, string email, string link, string language = "en")
        {
            string mailTitle = "Confirm your email";
            string mailText = $"Please confirm your {Constants.AppName} account's email address by clicking this link: <a href='{HtmlEncoder.Default.Encode(link)}'>link</a>";

            if (language == "da")
            {
                mailTitle = "Bekræft email";
                mailText = $"Bekræft venligst din email for din {Constants.AppName} konto ved at klikke på dette link: <a href='{HtmlEncoder.Default.Encode(link)}'>link</a>";
            }

            if (language == "de")
            {
                mailTitle = "Bestätigen Sie Ihre E-Mail-Adresse";
                mailText = $"Bestätigen Sie Ihre E-Mail-Adresse für {Constants.AppName} durch Klicken auf den Link: <a href='{HtmlEncoder.Default.Encode(link)}'>link</a>";
            }

            return emailSender.SendEmailAsync(email, mailTitle, mailText);
        }

        public static Task SendEmailUpdateConfirmationAsync(this IEmailSender emailSender, string email, string link, string language = "en")
        {
            string mailTitle = "Confirm your email";
            string mailText = $"Please confirm your {Constants.AppName} account's email address by clicking this link: <a href='{HtmlEncoder.Default.Encode(link)}'>link</a>";

            if (language == "da")
            {
                mailTitle = "Bekræft email";
                mailText = $"Bekræft venligst din email for din {Constants.AppName} konto ved at klikke på dette link: <a href='{HtmlEncoder.Default.Encode(link)}'>link</a>";
            }

            if (language == "de")
            {
                mailTitle = "Bestätigen Sie Ihre E-Mail-Adresse";
                mailText = $"Bestätigen Sie Ihre E-Mail-Adresse für {Constants.AppName} durch Klicken auf den Link: <a href='{HtmlEncoder.Default.Encode(link)}'>link</a>";
            }

            return emailSender.SendEmailAsync(email, mailTitle, mailText);
        }

        public static Task SendEmailInviteAsync(this IEmailSender emailSender, string email, string link, string inviter, string password)
        {
            return emailSender.SendEmailAsync(email, "Invitation to Join " + Constants.AppName,
                $"{inviter} invited you to join {Constants.AppName}.\r\n<br/><br/>Please confirm your email address by clicking this link: <a href='{HtmlEncoder.Default.Encode(link)}'>link</a>\r\n<br/><br/>Your username is your email address. <br/><br/>Your temporary password is: {password}\r\n<br/><br/><br/>Once you are logged in, you can change your username and password by clicking on your username, then select My Account.<br/><br/><a href='{Constants.WebAppUrl}'>{Constants.WebAppUrl}</a>");
        }
    }
}
