using System;
using System.ComponentModel.DataAnnotations;

namespace KinaUna.Data.Models
{
    /// <summary>
    /// Represents a Kanban board, which is a visual tool for organizing tasks and workflows.
    /// </summary>
    /// <remarks>A Kanban board typically consists of tasks organized into columns that represent different
    /// stages of a workflow. This class provides properties to store metadata about the board, such as its name,
    /// creation and modification timestamps, and the users who created or last modified it.</remarks>
    public class KanbanBoard
    {
        [Key]
        public int KanbanBoardId { get; set; }
        
        [MaxLength(128)]
        public string UId { get; set; } = string.Empty;
        public int ProgenyId { get; set; }
        
        [MaxLength(256)]
        public string Title { get; set; } = string.Empty;
        
        [MaxLength(4096)]
        public string Description { get; set; } = string.Empty;

        [MaxLength(4096)]
        public string Columns { get; set; } = string.Empty; // JSON representation of the columns in the Kanban board.
        
        public DateTime CreatedTime { get; set; }
        
        public DateTime ModifiedTime { get; set; }
        
        [MaxLength(256)] 
        public string CreatedBy { get; set; } = string.Empty;
        
        [MaxLength(256)]
        public string ModifiedBy { get; set; } = string.Empty;
        
        public int AccessLevel { get; set; }
    }
}
