using KinaUnaWeb.Models.TypeScriptModels;

namespace KinaUnaWeb.Models.Kanbans
{
    public class KanbanBoardsPageParameters: BasePageParameters
    {
        public string ContextFilter { get; set; } = "";
    }
}
