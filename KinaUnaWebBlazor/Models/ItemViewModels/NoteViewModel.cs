using KinaUna.Data.Models;

namespace KinaUnaWebBlazor.Models.ItemViewModels
{
    public class NoteViewModel: BaseViewModel
    {
        public int NoteId { get; set; } = 0;
        public Note Note { get; set; } = new Note();
        public string Title { get; set; } = "";
        public string Content { get; set; } = "";
        public string Category { get; set; } = "";
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public int AccessLevel { get; set; } = 5;
        public int ProgenyId { get; set; } = 0;
        public string Owner { get; set; } = "";
        public string PathName { get; set; } = "";
        public Progeny Progeny { get; set; } = new Progeny();
        public bool IsAdmin { get; set; } = false;
    }
}
