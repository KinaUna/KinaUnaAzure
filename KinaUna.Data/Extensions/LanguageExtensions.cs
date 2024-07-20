using KinaUna.Data.Models;

namespace KinaUna.Data.Extensions
{
    /// <summary>
    /// Extension methods for the KinaUnaLanguage class.
    /// </summary>
    public static class LanguageExtensions
    {
        /// <summary>
        /// Gets the long format of the language code.
        /// </summary>
        /// <param name="language"></param>
        /// <returns>string with the long format language code.</returns>
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
