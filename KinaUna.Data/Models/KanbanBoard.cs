using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

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

        /// <summary>
        /// Comma-separated list of tags associated with the entity for categorization and filtering.
        /// </summary>
        [MaxLength(256)]
        public string Tags { get; set; } = string.Empty;

        /// <summary>
        /// Context information associated with the entity, providing additional metadata or categorization.
        /// </summary>
        [MaxLength(256)]
        public string Context { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a value indicating whether the entity is marked as deleted.
        /// </summary>
        public bool IsDeleted { get; set; } = false;

        /// <summary>
        /// Gets or sets the associated progeny data for the current entity.
        /// </summary>
        [NotMapped]
        public Progeny Progeny { get; set; } = new Progeny();

        /// <summary>
        /// Gets or sets the list of columns associated with the Kanban board.
        /// </summary>
        [NotMapped]
        public List<KanbanBoardColumn> ColumnsList { get; set; } = [];
    }
}
