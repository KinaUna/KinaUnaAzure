using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KinaUna.Data.Models
{
    /// <summary>
    /// Represents an item on a Kanban board, linking a to-do item to a specific position within a board column and row.
    /// </summary>
    /// <remarks>A <see cref="KanbanItem"/> is associated with a specific Kanban board and to-do item, and it
    /// tracks its position within the board using column and row indices. It also includes metadata such as creation
    /// and modification timestamps and user information.</remarks>
    public class KanbanItem
    {
        [Key]
        public int KanbanItemId { get; set; }
        [MaxLength(128)]
        public string UId { get; set; } = string.Empty;
        public int KanbanBoardId { get; set; }
        public int TodoItemId { get; set; }
        public int ColumnIndex { get; set; }
        public int RowIndex { get; set; }
        public DateTime CreatedTime { get; set; }
        public DateTime ModifiedTime { get; set; }
        [MaxLength(256)]
        public string CreatedBy { get; set; } = string.Empty;
        [MaxLength(256)]
        public string ModifiedBy { get; set; } = string.Empty;

        [NotMapped]
        public TodoItem TodoItem { get; set; }
    }
}
