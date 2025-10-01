namespace KinaUnaWeb.Models.TypeScriptModels.Kanbans
{
    public class KanbanBoardElementResponse
    {
        public int KanbanBoardId { get; set; }
        public int LanguageId { get; init; }
        public KanbanBoard KanbanBoard { get; set; }
    }
}
