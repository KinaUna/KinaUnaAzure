using System.ComponentModel.DataAnnotations;

namespace KinaUna.Data.Models
{
    public class KanbanBoardColumn
    {
        public int Id { get; set; }
        public int ColumnIndex { get; set; }
        [MaxLength(256)]
        public string Title { get; set; } = string.Empty;
        public int WipLimit { get; set; }
        public int SetStatus { get; set; } = -1;
    }
}
