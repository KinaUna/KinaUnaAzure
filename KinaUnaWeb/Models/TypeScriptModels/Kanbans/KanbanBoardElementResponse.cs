namespace KinaUnaWeb.Models.TypeScriptModels.Kanbans
{
    public class KanbanBoardElementResponse
    {
        public int KanbanBoardId { get; set; }
        public int LanguageId { get; init; }
        public bool IsCurrentUserProgenyAdmin { get; set; }
        public KanbanBoard KanbanBoard { get; set; }
    }
}
