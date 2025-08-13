using KinaUna.Data.Models.ItemInterfaces;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KinaUna.Data.Models
{
    /// <summary>
    /// Represents a to-do item with details such as title, description, status, due date, and associated metadata.
    /// </summary>
    /// <remarks>This class is used to manage and track individual to-do items, including their assignment,
    /// status, and lifecycle events. It includes properties for metadata such as creation and modification details, as
    /// well as optional notes and tags for categorization.</remarks>
    public class TodoItem : IContexted, ITaggable
    {
        /// <summary>
        /// Gets or sets the unique identifier for the entity.
        /// </summary>
        [Key]
        public int TodoItemId { get; set; }
        /// <summary>
        /// Gets or sets the global identifier for this task, which can be used as a reference for copies and when importing/exporting TodoItems.
        /// </summary>
        [MaxLength(128)] public string UId { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets the unique identifier for the progeny this task is assigned to.
        /// </summary>
        public int ProgenyId { get; set; } // Assigned to.
        /// <summary>
        /// Gets or sets the title associated with the task.
        /// </summary>
        [MaxLength(256)]
        public string Title { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets the description for the task.
        /// </summary>
        [MaxLength(4096)]
        public string Description { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets the status code representing the current state of the task.
        /// 0 = Not started, 1 = In progress, 2 = Completed, 3 = Cancelled, 4 = Overdue.
        /// </summary>
        public int Status { get; set; }
        /// <summary>
        /// Gets or sets the start date of the event or process.
        /// </summary>
        public DateTime? StartDate { get; set; }
        /// <summary>
        /// Gets or sets the due date for the task.
        /// </summary>
        public DateTime? DueDate { get; set; }
        /// <summary>
        /// Gets or sets the date and time when the task was completed.
        /// </summary>
        public DateTime? CompletedDate { get; set; }
        /// <summary>
        /// Gets or sets the notes or additional information associated with the task.
        /// </summary>
        [MaxLength(4096)]
        public string Notes { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets the access level required to view this task.
        /// </summary>
        public int AccessLevel { get; set; }
        /// <summary>
        /// Gets or sets a comma-separated list of tags associated with the task.
        /// </summary>
        [MaxLength(256)]
        public string Tags { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets the context information associated with the current task.
        /// </summary>
        [MaxLength(256)]
        public string Context { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets the id for the user who created the task.
        /// </summary>
        [MaxLength(256)]
        public string CreatedBy { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets the date and time when the task was created.
        /// </summary>
        public DateTime CreatedTime { get; set; }
        /// <summary>
        /// Gets or sets the date and time when the task was last modified.
        /// </summary>
        public DateTime ModifiedTime { get; set; }
        /// <summary>
        /// Gets or sets the id of the user or system that last modified the task.
        /// </summary>
        [MaxLength(256)]
        public string ModifiedBy { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets a value indicating whether the entity is marked as deleted.
        /// </summary>
        public bool IsDeleted { get; set; } = false;

        [NotMapped]
        public Progeny Progeny { get; set; } = new Progeny();

    }
}
