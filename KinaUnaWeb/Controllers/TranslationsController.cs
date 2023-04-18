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
    public class TranslationsController : Controller
    {
        private readonly ITranslationsHttpClient _translationsHttpClient;
        public TranslationsController(ITranslationsHttpClient translationsHttpClient)
        {
            _translationsHttpClient = translationsHttpClient;
        }

        [HttpPost]
        public async Task<IActionResult> GetTranslation([FromBody] TextTranslation translation)
        {
            translation.Translation = await _translationsHttpClient.GetTranslation(translation.Word, translation.Page, translation.LanguageId);
            
            return Json(translation);
        }

        [HttpGet]
        public async Task<IActionResult> ZebraDatePicker(int languageId)
        {
            ZebraDatePickerTranslations translations = new();
            translations.DaysArray.Add(await _translationsHttpClient.GetTranslation("Sunday", PageNames.CalendarTools, languageId));
            translations.DaysArray.Add(await _translationsHttpClient.GetTranslation("Monday", PageNames.CalendarTools, languageId));
            translations.DaysArray.Add(await _translationsHttpClient.GetTranslation("Tuesday", PageNames.CalendarTools, languageId));
            translations.DaysArray.Add(await _translationsHttpClient.GetTranslation("Wednesday", PageNames.CalendarTools, languageId));
            translations.DaysArray.Add(await _translationsHttpClient.GetTranslation("Thursday", PageNames.CalendarTools, languageId));
            translations.DaysArray.Add(await _translationsHttpClient.GetTranslation("Friday", PageNames.CalendarTools, languageId));
            translations.DaysArray.Add(await _translationsHttpClient.GetTranslation("Saturday", PageNames.CalendarTools, languageId));

            translations.MonthsArray.Add(await _translationsHttpClient.GetTranslation("January", PageNames.CalendarTools, languageId));
            translations.MonthsArray.Add(await _translationsHttpClient.GetTranslation("February", PageNames.CalendarTools, languageId));
            translations.MonthsArray.Add(await _translationsHttpClient.GetTranslation("March", PageNames.CalendarTools, languageId));
            translations.MonthsArray.Add(await _translationsHttpClient.GetTranslation("April", PageNames.CalendarTools, languageId));
            translations.MonthsArray.Add(await _translationsHttpClient.GetTranslation("May", PageNames.CalendarTools, languageId));
            translations.MonthsArray.Add(await _translationsHttpClient.GetTranslation("June", PageNames.CalendarTools, languageId));
            translations.MonthsArray.Add(await _translationsHttpClient.GetTranslation("July", PageNames.CalendarTools, languageId));
            translations.MonthsArray.Add(await _translationsHttpClient.GetTranslation("August", PageNames.CalendarTools, languageId));
            translations.MonthsArray.Add(await _translationsHttpClient.GetTranslation("September", PageNames.CalendarTools, languageId));
            translations.MonthsArray.Add(await _translationsHttpClient.GetTranslation("October", PageNames.CalendarTools, languageId));
            translations.MonthsArray.Add(await _translationsHttpClient.GetTranslation("November", PageNames.CalendarTools, languageId));
            translations.MonthsArray.Add(await _translationsHttpClient.GetTranslation("December", PageNames.CalendarTools, languageId));

            translations.TodayString = await _translationsHttpClient.GetTranslation("Today", PageNames.CalendarTools, languageId);
            translations.ClearString = await _translationsHttpClient.GetTranslation("Clear", PageNames.CalendarTools, languageId);

            return Json(translations);
        }
    }
}