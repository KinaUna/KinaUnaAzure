using System.Collections.Generic;

namespace KinaUnaWeb.Models.TypeScriptModels.Sleep
{
    public class SleepDataModel
    {
        public List<KinaUna.Data.Models.Sleep> ChartList { get; set; } = [];
        public string SleepTotal { get; set; }
        public string SleepTotalHours { get; set; }
        public string SleepAveragePerDay { get; set; }
        public string SleepLastYear { get; set; }
        public string SleepAveragePerDayLastYear { get; set; }
        public string SleepLastMonth { get; set; }
        public string SleepAveragePerDayLastMonth { get; set; }
        public string SliderStart { get; set; }
        public string SliderEnd { get; set; }
    }
}
