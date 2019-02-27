using System.Text.Encodings.Web;
using System.Threading.Tasks;
using KinaUna.IDP.Services;

namespace KinaUna.IDP.Extensions
{
    public static class EmailSenderExtensions
    {
        public static Task SendEmailConfirmationAsync(this IEmailSender emailSender, string email, string link, string language = "en")
        {
            string mailTitle = "Confirm your email";
            string mailText = $"Please confirm your Kina Una account's email address by clicking this link: <a href='{HtmlEncoder.Default.Encode(link)}'>link</a>";

            if (language == "da")
            {
                mailTitle = "Bekræft email";
                mailText = $"Bekræft venligst din email for din Kina Una konto ved at klikke på dette link: <a href='{HtmlEncoder.Default.Encode(link)}'>link</a>";
            }

            if (language == "de")
            {
                mailTitle = "Bestätigen Sie Ihre E-Mail-Adresse";
                mailText = $"Bestätigen Sie Ihre E-Mail-Adresse für Kina Una durch Klicken auf den Link: <a href='{HtmlEncoder.Default.Encode(link)}'>link</a>";
            }

            return emailSender.SendEmailAsync(email, mailTitle, mailText);
        }

        public static Task SendEmailUpdateConfirmationAsync(this IEmailSender emailSender, string email, string link, string language = "en")
        {
            string mailTitle = "Confirm your email";
            string mailText = $"Please confirm your Kina Una account's email address by clicking this link: <a href='{HtmlEncoder.Default.Encode(link)}'>link</a>";

            if (language == "da")
            {
                mailTitle = "Bekræft email";
                mailText = $"Bekræft venligst din email for din Kina Una konto ved at klikke på dette link: <a href='{HtmlEncoder.Default.Encode(link)}'>link</a>";
            }

            if (language == "de")
            {
                mailTitle = "Bestätigen Sie Ihre E-Mail-Adresse";
                mailText = $"Bestätigen Sie Ihre E-Mail-Adresse für Kina Una durch Klicken auf den Link: <a href='{HtmlEncoder.Default.Encode(link)}'>link</a>";
            }

            return emailSender.SendEmailAsync(email, mailTitle, mailText);
        }

        public static Task SendEmailInviteAsync(this IEmailSender emailSender, string email, string link, string inviter, string password)
        {
            return emailSender.SendEmailAsync(email, "Invitation to Join KinaUna.com",
                $"{inviter} invited you to join KinaUna.com.\r\n<br/><br/>Please confirm your email address by clicking this link: <a href='{HtmlEncoder.Default.Encode(link)}'>link</a>\r\n<br/><br/>Your username is your email address. <br/><br/>Your temporary password is: {password}\r\n<br/><br/><br/>Once you are logged in, you can change your username and password by clicking on your username, then select My Account.<br/><br/><a href='https://kinauna.com'>kinauna.com</a>");
        }
    }
}
