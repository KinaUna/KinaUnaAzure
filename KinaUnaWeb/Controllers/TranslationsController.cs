using System.Threading.Tasks;
using KinaUna.Data;
using KinaUna.Data.Models;
using KinaUnaWeb.Models.TypeScriptModels;
using KinaUnaWeb.Services.HttpClients;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KinaUnaWeb.Controllers
{
    [AllowAnonymous]
    public class TranslationsController(ITranslationsHttpClient translationsHttpClient) : Controller
    {
        [HttpPost]
        public async Task<IActionResult> GetTranslation([FromBody] TextTranslation translation)
        {
            translation.Translation = await translationsHttpClient.GetTranslation(translation.Word, translation.Page, translation.LanguageId);
            
            return Json(translation);
        }

        [HttpGet]
        public async Task<IActionResult> ZebraDatePicker(int languageId)
        {
            ZebraDatePickerTranslations translations = new();
            translations.DaysArray.Add(await translationsHttpClient.GetTranslation("Sunday", PageNames.CalendarTools, languageId));
            translations.DaysArray.Add(await translationsHttpClient.GetTranslation("Monday", PageNames.CalendarTools, languageId));
            translations.DaysArray.Add(await translationsHttpClient.GetTranslation("Tuesday", PageNames.CalendarTools, languageId));
            translations.DaysArray.Add(await translationsHttpClient.GetTranslation("Wednesday", PageNames.CalendarTools, languageId));
            translations.DaysArray.Add(await translationsHttpClient.GetTranslation("Thursday", PageNames.CalendarTools, languageId));
            translations.DaysArray.Add(await translationsHttpClient.GetTranslation("Friday", PageNames.CalendarTools, languageId));
            translations.DaysArray.Add(await translationsHttpClient.GetTranslation("Saturday", PageNames.CalendarTools, languageId));

            translations.MonthsArray.Add(await translationsHttpClient.GetTranslation("January", PageNames.CalendarTools, languageId));
            translations.MonthsArray.Add(await translationsHttpClient.GetTranslation("February", PageNames.CalendarTools, languageId));
            translations.MonthsArray.Add(await translationsHttpClient.GetTranslation("March", PageNames.CalendarTools, languageId));
            translations.MonthsArray.Add(await translationsHttpClient.GetTranslation("April", PageNames.CalendarTools, languageId));
            translations.MonthsArray.Add(await translationsHttpClient.GetTranslation("May", PageNames.CalendarTools, languageId));
            translations.MonthsArray.Add(await translationsHttpClient.GetTranslation("June", PageNames.CalendarTools, languageId));
            translations.MonthsArray.Add(await translationsHttpClient.GetTranslation("July", PageNames.CalendarTools, languageId));
            translations.MonthsArray.Add(await translationsHttpClient.GetTranslation("August", PageNames.CalendarTools, languageId));
            translations.MonthsArray.Add(await translationsHttpClient.GetTranslation("September", PageNames.CalendarTools, languageId));
            translations.MonthsArray.Add(await translationsHttpClient.GetTranslation("October", PageNames.CalendarTools, languageId));
            translations.MonthsArray.Add(await translationsHttpClient.GetTranslation("November", PageNames.CalendarTools, languageId));
            translations.MonthsArray.Add(await translationsHttpClient.GetTranslation("December", PageNames.CalendarTools, languageId));

            translations.TodayString = await translationsHttpClient.GetTranslation("Today", PageNames.CalendarTools, languageId);
            translations.ClearString = await translationsHttpClient.GetTranslation("Clear", PageNames.CalendarTools, languageId);

            return Json(translations);
        }
    }
}