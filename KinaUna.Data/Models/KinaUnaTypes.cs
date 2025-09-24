namespace KinaUna.Data.Models
{
    /// <summary>
    /// Enumeration of types of timeline items.
    /// </summary>
    public static class KinaUnaTypes
    {
        public enum TimeLineType { Photo = 1, Video = 2, Calendar = 3, Vocabulary = 4, Skill = 5,
            Friend = 6, Measurement = 7, Sleep = 8, Note = 9, Contact = 10,
            Vaccination = 11, Location = 12, User = 13, UserAccess = 14, TodoItem = 15,
            KanbanBoard = 16, KanbanItem= 17,  Progeny = 100 }

        public enum TodoStatusType
        {
            /// <summary>
            /// The item is not started.
            /// </summary>
            NotStarted = 0,

            /// <summary>
            /// The item is in progress.
            /// </summary>
            InProgress = 1,

            /// <summary>
            /// The item is completed.
            /// </summary>
            Completed = 2,

            /// <summary>
            /// The item is cancelled.
            /// </summary>
            Cancelled = 3,

            /// <summary>
            /// The item is overdue.
            /// </summary>
            Overdue = 4
        }
    }
}
