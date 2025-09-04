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
        /// <summary>
        /// Gets or sets the unique identifier for the Kanban item.
        /// </summary>
        [Key]
        public int KanbanItemId { get; set; }

        /// <summary>
        /// Gets or sets the global identifier for this task, which can be used as a reference for copies and when importing/exporting KanbanItems.
        /// </summary>
        [MaxLength(128)]
        public string UId { get; set; } = string.Empty;

        /// <summary>
        /// The ID of the Kanban board this item belongs to.
        /// </summary>
        public int KanbanBoardId { get; set; }

        /// <summary>
        /// Gets or sets the unique identifier for the to-do item associated with this Kanban item.
        /// </summary>
        public int TodoItemId { get; set; }

        /// <summary>
        /// Gets or sets the id of the column in the Kanban board where this item is located.
        /// </summary>
        public int ColumnId { get; set; }

        /// <summary>
        /// Gets or sets the zero-based index of the row within the column, determining the vertical position of the item.
        /// </summary>
        public int RowIndex { get; set; }

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
        /// Gets or sets a value indicating whether the entity is marked as deleted.
        /// </summary>
        public bool IsDeleted { get; set; } = false;

        /// <summary>
        /// Gets or sets the associated <see cref="TodoItem"/> for this entity.  This property is not mapped to the
        /// database.
        /// </summary>
        /// <remarks>The <see cref="TodoItem"/> property is intended for use in scenarios where  an
        /// in-memory representation of the associated item is needed without persisting  it to the database. Ensure
        /// that this property is used only for transient or  computed data that does not require database
        /// storage.</remarks>
        [NotMapped]
        public TodoItem TodoItem { get; set; }
    }
}
