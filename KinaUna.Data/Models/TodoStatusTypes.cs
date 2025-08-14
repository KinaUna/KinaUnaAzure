namespace KinaUna.Data.Models
{
    public static class TodoStatusTypes
    {
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
