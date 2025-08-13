namespace KinaUnaWeb.Models.TypeScriptModels.TodoItems
{
    public class TodoItemResponse
    {
        public int TodoItemId { get; set; }
        public int LanguageId { get; init; }
        public bool IsCurrentUserProgenyAdmin { get; set; }
        public TodoItem TodoItem { get; set; }
    }
}
