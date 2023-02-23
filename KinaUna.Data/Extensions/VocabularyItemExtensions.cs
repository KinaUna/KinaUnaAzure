using KinaUna.Data.Models;
using System;

namespace KinaUna.Data.Extensions
{
    public static class VocabularyItemExtensions
    {
        public static void CopyPropertiesForUpdate(this VocabularyItem currentVocabularyItem, VocabularyItem otherVocabularyItem)
        {
            currentVocabularyItem.AccessLevel = otherVocabularyItem.AccessLevel;
            currentVocabularyItem.Description = otherVocabularyItem.Description;
            currentVocabularyItem.Date = otherVocabularyItem.Date;
            currentVocabularyItem.Language = otherVocabularyItem.Language;
            currentVocabularyItem.SoundsLike = otherVocabularyItem.SoundsLike;
            currentVocabularyItem.Word = otherVocabularyItem.Word;
        }

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
