using System.Collections.Generic;

namespace KinaUna.Data.Models.DTOs
{
    public class TodoItemsResponse
    {
        public List<TodoItem> TodoItems { get; set; }

        public List<Progeny> ProgenyList { get; set; }

        public TodoItemsRequest TodoItemsRequest { get; set; }
    }
}
