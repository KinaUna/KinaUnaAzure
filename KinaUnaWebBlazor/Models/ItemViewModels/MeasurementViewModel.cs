using KinaUna.Data.Models;

namespace KinaUnaWebBlazor.Models.ItemViewModels
{
    public class MeasurementViewModel: BaseViewModel
    {
        public List<Measurement> MeasurementsList { get; set; } = new List<Measurement>();
        public int MeasurementId { get; set; } = 0;
        public int ProgenyId { get; set; } = 0;
        public double Weight { get; set; } = 0;
        public double Height { get; set; } = 0;
        public double Circumference { get; set; } = 0;
        public string EyeColor { get; set; } = "";
        public string HairColor { get; set; } = "";
        public DateTime Date { get; set; } = DateTime.UtcNow;
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public int AccessLevel { get; set; } = 5;
        public string Author { get; set; } = "";
        public Progeny Progeny { get; set; } = new Progeny();
        public bool IsAdmin { get; set; } = false;
        public Measurement Measurement { get; set; } = new Measurement();
    }
}
