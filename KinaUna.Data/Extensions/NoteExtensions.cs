using KinaUna.Data.Models;

namespace KinaUna.Data.Extensions
{
    /// <summary>
    /// Extension methods for the Note class.
    /// </summary>
    public static class NoteExtensions
    {
        /// <summary>
        /// Copies the properties needed for updating a Note entity from one Note object to another.
        /// </summary>
        /// <param name="currentNote"></param>
        /// <param name="otherNote"></param>
        public static void CopyPropertiesForUpdate(this Note currentNote, Note otherNote )
        {
            currentNote.AccessLevel = otherNote.AccessLevel;
            currentNote.ProgenyId = otherNote.ProgenyId;
            currentNote.Category = otherNote.Category;
            currentNote.Content = otherNote.Content;
            currentNote.CreatedDate = otherNote.CreatedDate;
            currentNote.NoteNumber = otherNote.NoteNumber;
            currentNote.Owner = otherNote.Owner;
            currentNote.Title = otherNote.Title;
            currentNote.Progeny = otherNote.Progeny;
        }

        /// <summary>
        /// Copies the properties needed for adding a Note entity from one Note object to another.
        /// </summary>
        /// <param name="currentNote"></param>
        /// <param name="otherNote"></param>
        public static void CopyPropertiesForAdd(this Note currentNote, Note otherNote)
        {
            currentNote.AccessLevel = otherNote.AccessLevel;
            currentNote.Owner = otherNote.Owner;
            currentNote.Content = otherNote.Content;
            currentNote.Category = otherNote.Category;
            currentNote.ProgenyId = otherNote.ProgenyId;
            currentNote.Title = otherNote.Title;
            currentNote.CreatedDate = otherNote.CreatedDate;
        }
    }
}
