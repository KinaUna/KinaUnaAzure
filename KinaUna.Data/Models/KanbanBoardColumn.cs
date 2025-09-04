namespace KinaUna.Data.Models
{
    public class KanbanBoardColumn
    {
        public int Id { get; set; }
        public int ColumnIndex { get; set; }
        public string Title { get; set; } = string.Empty;
        public int WipLimit { get; set; }
        
    }
}
