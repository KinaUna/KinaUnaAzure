using KinaUna.Data.Models;

namespace KinaUnaWebBlazor.Models.ItemViewModels
{
    public class VaccinationViewModel: BaseViewModel
    {
        public int VaccinationId { get; set; } = 0;
        public string VaccinationName { get; set; } = String.Empty;
        public string VaccinationDescription { get; set; } = string.Empty;
        public DateTime VaccinationDate { get; set; } = DateTime.UtcNow;
        public string Notes { get; set; } = string.Empty;
        public int ProgenyId { get; set; } = 0;
        public int AccessLevel { get; set; } = 5;
        public string Author { get; set; } = string.Empty;
        public List<Vaccination> VaccinationList { get; set; } = new List<Vaccination>();
        public Progeny Progeny { get; set; } = new Progeny();
        public bool IsAdmin { get; set; } = false;
        public Vaccination Vaccination { get; set; } = new Vaccination();
    }
}
