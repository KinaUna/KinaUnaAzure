using System.Threading.Tasks;
using KinaUna.Data;
using KinaUnaWeb.Models.TypeScriptModels;
using KinaUnaWeb.Services.HttpClients;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KinaUnaWeb.Controllers
{
    [AllowAnonymous]
    public class TranslationsController(ITranslationsHttpClient translationsHttpClient) : Controller
    {
        /// <summary>
        /// HttpPost method for getting a translation.
        /// </summary>
        /// <param name="translation">TextTranslation object with the word, page and languageId for getting a translation.</param>
        /// <returns>Json of TextTranslation object.</returns>
        [HttpPost]
        public async Task<IActionResult> GetTranslation([FromBody] TextTranslation translation)
        {
            translation.Translation = await translationsHttpClient.GetTranslation(translation.Word, translation.Page, translation.LanguageId);
            
            return Json(translation);
        }

        /// <summary>
        /// HttpGet method for getting translations for the ZebraDatePicker.
        /// </summary>
        /// <param name="id"></param>
        /// <returns>Json of ZebraDatePickerTranslations object.</returns>
        [HttpGet]
        public async Task<IActionResult> ZebraDatePicker(int id)
        {
            ZebraDatePickerTranslations translations = new();
            translations.DaysArray.Add(await translationsHttpClient.GetTranslation("Sunday", PageNames.CalendarTools, id));
            translations.DaysArray.Add(await translationsHttpClient.GetTranslation("Monday", PageNames.CalendarTools, id));
            translations.DaysArray.Add(await translationsHttpClient.GetTranslation("Tuesday", PageNames.CalendarTools, id));
            translations.DaysArray.Add(await translationsHttpClient.GetTranslation("Wednesday", PageNames.CalendarTools, id));
            translations.DaysArray.Add(await translationsHttpClient.GetTranslation("Thursday", PageNames.CalendarTools, id));
            translations.DaysArray.Add(await translationsHttpClient.GetTranslation("Friday", PageNames.CalendarTools, id));
            translations.DaysArray.Add(await translationsHttpClient.GetTranslation("Saturday", PageNames.CalendarTools, id));

            translations.MonthsArray.Add(await translationsHttpClient.GetTranslation("January", PageNames.CalendarTools, id));
            translations.MonthsArray.Add(await translationsHttpClient.GetTranslation("February", PageNames.CalendarTools, id));
            translations.MonthsArray.Add(await translationsHttpClient.GetTranslation("March", PageNames.CalendarTools, id));
            translations.MonthsArray.Add(await translationsHttpClient.GetTranslation("April", PageNames.CalendarTools, id));
            translations.MonthsArray.Add(await translationsHttpClient.GetTranslation("May", PageNames.CalendarTools, id));
            translations.MonthsArray.Add(await translationsHttpClient.GetTranslation("June", PageNames.CalendarTools, id));
            translations.MonthsArray.Add(await translationsHttpClient.GetTranslation("July", PageNames.CalendarTools, id));
            translations.MonthsArray.Add(await translationsHttpClient.GetTranslation("August", PageNames.CalendarTools, id));
            translations.MonthsArray.Add(await translationsHttpClient.GetTranslation("September", PageNames.CalendarTools, id));
            translations.MonthsArray.Add(await translationsHttpClient.GetTranslation("October", PageNames.CalendarTools, id));
            translations.MonthsArray.Add(await translationsHttpClient.GetTranslation("November", PageNames.CalendarTools, id));
            translations.MonthsArray.Add(await translationsHttpClient.GetTranslation("December", PageNames.CalendarTools, id));

            translations.TodayString = await translationsHttpClient.GetTranslation("Today", PageNames.CalendarTools, id);
            translations.ClearString = await translationsHttpClient.GetTranslation("Clear", PageNames.CalendarTools, id);

            return Json(translations);
        }
    }
}