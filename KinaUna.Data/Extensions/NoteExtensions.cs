using KinaUna.Data.Models;

namespace KinaUna.Data.Extensions
{
    public static class NoteExtensions
    {
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
