using KinaUna.Data.Models;

namespace KinaUna.Data.Extensions
{
    public static class LanguageExtensions
    {
        public static string CodeToLongFormat(this KinaUnaLanguage language)
        {
            if (language.Code == "da")
            {
                return "da-DK";
            }

            if (language.Code == "de")
            {
                return "de-DE";
            }

            return "en-US";
        }
    }
}
