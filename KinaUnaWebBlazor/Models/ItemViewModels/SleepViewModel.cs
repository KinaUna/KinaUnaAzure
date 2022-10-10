using KinaUna.Data.Models;

namespace KinaUnaWebBlazor.Models.ItemViewModels
{
    public class SleepViewModel: BaseViewModel
    {
        public List<Sleep> SleepList { get; set; } = new List<Sleep>();
        public List<Sleep> ChartList { get; set; } = new List<Sleep>();
        public int SleepId { get; set; } = 0;
        public int ProgenyId { get; set; } = 0;
        public DateTime? SleepStart { get; set; }
        public DateTime? SleepEnd { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public int SleepRating { get; set; } = 0;
        public string SleepNotes { get; set; } = "";
        public int AccessLevel { get; set; } = 5;
        public string Author { get; set; } = "";
        public Progeny Progeny { get; set; } = new Progeny();
        public bool IsAdmin { get; set; } = false;
        public TimeSpan SleepDuration { get; set; } = TimeSpan.MinValue;
        public string StartString { get; set; } = ""; // For calendar view.
        public string EndString { get; set; } = ""; // For calendar view.
        public TimeSpan SleepTotal { get; set; } = TimeSpan.MinValue;
        public TimeSpan TotalAverage { get; set; } = TimeSpan.MinValue;
        public TimeSpan SleepLastMonth { get; set; } = TimeSpan.MinValue;
        public TimeSpan LastMonthAverage { get; set; } = TimeSpan.MinValue;
        public TimeSpan SleepLastYear { get; set; } = TimeSpan.MinValue;
        public TimeSpan LastYearAverage { get; set; } = TimeSpan.MinValue;
        public Sleep Sleep { get; set; } = new Sleep();
    }
}
