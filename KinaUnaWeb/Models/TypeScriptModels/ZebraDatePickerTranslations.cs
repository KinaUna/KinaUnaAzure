using System.Collections.Generic;

namespace KinaUnaWeb.Models.TypeScriptModels
{
    public class ZebraDatePickerTranslations
    {
        public List<string> DaysArray { get; set; } = new List<string>();
        public List<string> MonthsArray { get; set; } = new List<string>();
        public string TodayString { get; set; } = "Today";
        public string ClearString { get; set; } = "Clear";
    }
}
