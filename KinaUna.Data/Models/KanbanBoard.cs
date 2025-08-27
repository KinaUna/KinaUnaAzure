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
        /// <summary>
        /// Gets or sets the unique identifier for the Kanban board.
        /// </summary>
        [Key]
        public int KanbanBoardId { get; set; }

        /// <summary>
        ///  Gets or sets the global identifier for this task, which can be used as a reference for copies and when importing/exporting kanban boards.
        /// </summary>
        [MaxLength(128)]
        public string UId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the unique identifier for the progeny that this Kanban board is associated with.
        /// </summary>
        public int ProgenyId { get; set; }

        // Todo: Add Family or Group association if needed in the future.

        /// <summary>
        /// Gets or sets the title of the Kanban board.
        /// </summary>
        [MaxLength(256)]
        public string Title { get; set; } = string.Empty;
        
        /// <summary>
        /// Gets or sets the description text. The maximum length is 4,096 characters.
        /// </summary>
        [MaxLength(4096)]
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the JSON representation of the columns in the Kanban board.
        /// </summary>
        /// <remarks>The JSON structure should represent the columns and their associated metadata  in a
        /// format that can be parsed and used to reconstruct the Kanban board layout.</remarks>
        [MaxLength(4096)]
        public string Columns { get; set; } = string.Empty; // JSON representation of the columns in the Kanban board.

        /// <summary>
        /// Gets or sets the date and time when the entity was created.
        /// </summary>
        public DateTime CreatedTime { get; set; }
        
        /// <summary>
        /// Gets or sets the date and time when the object was last modified.
        /// </summary>
        public DateTime ModifiedTime { get; set; }
        
        /// <summary>
        /// Gets or sets the identifier of the user or system that created the entity.
        /// </summary>
        [MaxLength(256)] 
        public string CreatedBy { get; set; } = string.Empty;
        
        /// <summary>
        /// Gets or sets the identifier of the user or system that last modified the entity.
        /// </summary>
        [MaxLength(256)]
        public string ModifiedBy { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the access level required to view this Kanban board.
        /// </summary>
        public int AccessLevel { get; set; }
    }
}
