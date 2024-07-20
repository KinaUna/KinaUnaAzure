using KinaUna.Data.Models;
using System;

namespace KinaUna.Data.Extensions
{
    /// <summary>
    /// Extension methods for the VocabularyItem class.
    /// </summary>
    public static class VocabularyItemExtensions
    {
        /// <summary>
        /// Copies the properties needed for updating a VocabularyItem entity from one VocabularyItem object to another.
        /// </summary>
        /// <param name="currentVocabularyItem"></param>
        /// <param name="otherVocabularyItem"></param>
        public static void CopyPropertiesForUpdate(this VocabularyItem currentVocabularyItem, VocabularyItem otherVocabularyItem)
        {
            currentVocabularyItem.AccessLevel = otherVocabularyItem.AccessLevel;
            currentVocabularyItem.Description = otherVocabularyItem.Description;
            currentVocabularyItem.Date = otherVocabularyItem.Date;
            currentVocabularyItem.Language = otherVocabularyItem.Language;
            currentVocabularyItem.SoundsLike = otherVocabularyItem.SoundsLike;
            currentVocabularyItem.Word = otherVocabularyItem.Word;
        }

        /// <summary>
        /// Copies the properties needed for adding a VocabularyItem entity from one VocabularyItem object to another.
        /// </summary>
        /// <param name="currentVocabularyItem"></param>
        /// <param name="otherVocabularyItem"></param>
        public static void CopyPropertiesForAdd(this VocabularyItem currentVocabularyItem, VocabularyItem otherVocabularyItem)
        {
            currentVocabularyItem.AccessLevel = otherVocabularyItem.AccessLevel;
            currentVocabularyItem.Author = otherVocabularyItem.Author;
            currentVocabularyItem.Date = otherVocabularyItem.Date;
            currentVocabularyItem.DateAdded = DateTime.UtcNow;
            currentVocabularyItem.ProgenyId = otherVocabularyItem.ProgenyId;
            currentVocabularyItem.Description = otherVocabularyItem.Description;
            currentVocabularyItem.Language = otherVocabularyItem.Language;
            currentVocabularyItem.SoundsLike = otherVocabularyItem.SoundsLike;
            currentVocabularyItem.Word = otherVocabularyItem.Word;
        }
    }
}
