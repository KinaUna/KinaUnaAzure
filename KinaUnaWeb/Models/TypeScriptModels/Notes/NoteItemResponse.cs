using KinaUna.Data.Models;

namespace KinaUnaWeb.Models.TypeScriptModels.Notes
{
    public class NoteItemResponse
    {
        public int NoteId { get; set; }
        public int LanguageId { get; set; }
        public bool IsCurrentUserProgenyAdmin { get; set; }
        public Note Note { get; set; }
    }
}
