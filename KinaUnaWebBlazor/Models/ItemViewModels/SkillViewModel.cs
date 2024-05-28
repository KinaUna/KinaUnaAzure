using KinaUna.Data.Models;

namespace KinaUnaWebBlazor.Models.ItemViewModels
{
    public class SkillViewModel: BaseViewModel
    {
        public int SkillId { get; set; } = 0;
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public string Category { get; set; } = "";
        public DateTime? SkillFirstObservation { get; set; }
        public DateTime SkillAddedDate { get; set; } = DateTime.UtcNow;
        public string Author { get; set; } = "";
        public int ProgenyId { get; set; } = 0;
        public Progeny Progeny { get; set; } = new();
        public bool IsAdmin { get; set; } = false;
        public Skill Skill { get; set; } = new();
        public int AccessLevel { get; set; } = 5;
    }
}
