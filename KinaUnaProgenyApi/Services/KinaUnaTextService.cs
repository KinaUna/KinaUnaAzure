using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KinaUna.Data.Contexts;
using KinaUna.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace KinaUnaProgenyApi.Services
{
    public class KinaUnaTextService(ProgenyDbContext context) : IKinaUnaTextService
    {
        /// <summary>
        /// Gets a KinaUnaText by Title, Page and language.
        /// </summary>
        /// <param name="title">The Title of the KinaUnaText.</param>
        /// <param name="page">The Page the text belongs to.</param>
        /// <param name="languageId">The LanguageId of the language.</param>
        /// <returns>KinaUnaText.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1862:Use the 'StringComparison' method overloads to perform case-insensitive string comparisons", Justification = "StringComparison does not work with database queries.")]
        public async Task<KinaUnaText> GetTextByTitle(string title, string page, int languageId)
        {
            title = title.Trim();
            page = page.Trim();
            KinaUnaText textItem = await context.KinaUnaTexts.AsNoTracking()
                .FirstOrDefaultAsync(t => t.Title.ToUpper() == title.ToUpper() && t.Page.ToUpper() == page.ToUpper() && t.LanguageId == languageId);

            return textItem;
        }


        /// <summary>
        /// Gets a KinaUnaText by Id.
        /// </summary>
        /// <param name="id">The Id of the KinaUnaText to get.</param>
        /// <returns>KinaUnaText with the given Id.</returns>
        public async Task<KinaUnaText> GetTextById(int id)
        {
            KinaUnaText kinaUnaText = await context.KinaUnaTexts.AsNoTracking().SingleOrDefaultAsync(t => t.Id == id);
            return kinaUnaText;
        }

        /// <summary>
        /// Gets a KinaUnaText by TextId and language.
        /// Each text is translated into all languages, with the same TextId.
        /// </summary>
        /// <param name="textId">The TextId of the text to get.</param>
        /// <param name="languageId">The language of the translation to get.</param>
        /// <returns>The KinaUnaText with the TextId and in the language given.</returns>
        public async Task<KinaUnaText> GetTextByTextId(int textId, int languageId)
        {
            KinaUnaText kinaUnaText = await context.KinaUnaTexts.AsNoTracking().SingleOrDefaultAsync(t => t.TextId == textId && t.LanguageId == languageId);
            return kinaUnaText;
        }

        /// <summary>
        /// Gets a list of all KinaUnaTexts for a page, in the specified language.
        /// </summary>
        /// <param name="page">The Page to get KinaUnaTexts for.</param>
        /// <param name="languageId">The LanguageId of the language to get KinaUnaTexts for.</param>
        /// <returns>List of KinaUnaTexts.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1862:Use the 'StringComparison' method overloads to perform case-insensitive string comparisons", Justification = "<Pending>")]
        public async Task<List<KinaUnaText>> GetPageTextsList(string page, int languageId)
        {
            page = page.Trim();

            if (languageId == 0)
            {
                languageId = 1;
            }

            List<KinaUnaText> texts = await context.KinaUnaTexts.AsNoTracking().Where(t => t.LanguageId == languageId && t.Page.ToUpper() == page.ToUpper()).ToListAsync();
            return texts;
        }

        /// <summary>
        /// Gets a list of all KinaUnaTexts in a given language.
        /// </summary>
        /// <param name="languageId">The LanguageId of the language to get KinaUnaTexts for.</param>
        /// <returns>List of KinaUnaTexts.</returns>
        public async Task<List<KinaUnaText>> GetAllPageTextsList(int languageId)
        {
            if (languageId == 0)
            {
                languageId = 1;
            }
            List<KinaUnaText> texts = await context.KinaUnaTexts.AsNoTracking().Where(t => t.LanguageId == languageId).ToListAsync();
            return texts;
        }

        /// <summary>
        /// Checks each TextNumber to see if there are texts for all languages.
        /// If not, it adds a text for the missing languages.
        /// </summary>
        /// <returns></returns>
        public async Task CheckLanguages()
        {
            List<KinaUnaTextNumber> textNumbers = await context.KinaUnaTextNumbers.AsNoTracking().ToListAsync();
            List<KinaUnaLanguage> languages = await context.Languages.AsNoTracking().ToListAsync();

            foreach (KinaUnaTextNumber tNumber in textNumbers)
            {
                List<KinaUnaText> texts = await context.KinaUnaTexts.AsNoTracking().Where(t => t.TextId == tNumber.Id).OrderBy(t => t.LanguageId).ToListAsync();
                if (texts.Count == 0 || texts.Count >= languages.Count) continue;

                foreach (KinaUnaLanguage lang in languages)
                {
                    KinaUnaText textItem = await context.KinaUnaTexts.SingleOrDefaultAsync(t => t.TextId == tNumber.Id && t.LanguageId == lang.Id);
                    if (textItem != null) continue;

                    KinaUnaText oldKinaUnaText = texts.First();

                    KinaUnaText newKinaUnaText = new()
                    {
                        Page = oldKinaUnaText.Page,
                        Title = oldKinaUnaText.Title,
                        Text = oldKinaUnaText.Text,
                        Created = oldKinaUnaText.Created,
                        Updated = oldKinaUnaText.Updated,
                        LanguageId = lang.Id,
                        TextId = oldKinaUnaText.TextId
                    };
                    _ = await context.KinaUnaTexts.AddAsync(newKinaUnaText);
                    _ = await context.SaveChangesAsync();
                }
            }
        }

        /// <summary>
        /// Adds a new KinaUnaText to the database.
        /// Adds a KinaUnaTextNumber to the database for the KinaUnaText, so translations can be managed.
        /// Then adds translations for the text in all languages.
        /// </summary>
        /// <param name="text">The KinaUnaText object to add.</param>
        /// <returns>The added KinaUnaText object.</returns>
        public async Task<KinaUnaText> AddText(KinaUnaText text)
        {
            text.Title = text.Title.Trim();
            text.Page = text.Page.Trim();

            if (text.Title.StartsWith("__"))
            {
                // Title's starting with double underscore are considered unique system pages, so we need to make sure no other text has the same title on a page.
                text = await AddSystemPageText(text);
            }
            else
            {
                KinaUnaTextNumber textNumber = new()
                {
                    DefaultLanguage = 1
                };
                _ = await context.KinaUnaTextNumbers.AddAsync(textNumber);
                _ = await context.SaveChangesAsync();
                text.TextId = textNumber.Id;
                text.Created = DateTime.UtcNow;
                text.Updated = text.Created;
                _ = context.KinaUnaTexts.Add(text);
                _ = await context.SaveChangesAsync();
            }

            await AddTextForOtherLanguages(text);

            return text;
        }

        /// <summary>
        /// Adds a new KinaUnaText to the database for special system pages (Privacy, About, etc.).
        /// Ensures that Title is unique for the page and language.
        /// </summary>
        /// <param name="text">The KinaUnaText to add.</param>
        /// <returns>The added KinaUnaText.</returns>
        private async Task<KinaUnaText> AddSystemPageText(KinaUnaText text)
        {
            KinaUnaText existingTextItem = await context.KinaUnaTexts.SingleOrDefaultAsync(t => t.Title.ToUpper() == text.Title.ToUpper() && t.Page.ToUpper() == text.Page.ToUpper() && t.LanguageId == text.LanguageId);
            if (existingTextItem == null)
            {
                KinaUnaTextNumber textNumber = new()
                {
                    DefaultLanguage = 1
                };
                _ = await context.KinaUnaTextNumbers.AddAsync(textNumber);
                _ = await context.SaveChangesAsync();
                text.TextId = textNumber.Id;
                text.Created = DateTime.UtcNow;
                text.Updated = text.Created;
                _ = context.KinaUnaTexts.Add(text);
                _ = await context.SaveChangesAsync();

                return text;

            }

            existingTextItem.Title = text.Title;
            existingTextItem.Text = text.Text;
            existingTextItem.Page = text.Page;
            existingTextItem.Created = DateTime.UtcNow;
            existingTextItem.Updated = existingTextItem.Created;
            _ = context.KinaUnaTexts.Update(existingTextItem);
            _ = await context.SaveChangesAsync();

            return existingTextItem;
        }

        /// <summary>
        /// Adds translations for a text in all languages.
        /// </summary>
        /// <param name="text">The KinaUnaText to add translations for.</param>
        /// <returns></returns>
        private async Task AddTextForOtherLanguages(KinaUnaText text)
        {
            List<KinaUnaLanguage> languages = await context.Languages.AsNoTracking().ToListAsync();
            foreach (KinaUnaLanguage lang in languages)
            {
                if (lang.Id == text.LanguageId) continue;

                KinaUnaText textItem = await context.KinaUnaTexts.SingleOrDefaultAsync(t => t.TextId == text.TextId && t.LanguageId == lang.Id);
                if (textItem != null) continue;

                textItem = new KinaUnaText
                {
                    LanguageId = lang.Id,
                    Page = text.Page,
                    Title = text.Title,
                    Text = text.Text,
                    TextId = text.TextId,
                    Created = text.Created,
                    Updated = text.Updated
                };
                _ = context.KinaUnaTexts.Add(textItem);
                _ = await context.SaveChangesAsync();
            }
        }

        /// <summary>
        /// Updates a KinaUnaText in the database.
        /// </summary>
        /// <param name="id">The Id of the KinaUnaText</param>
        /// <param name="text">The KinaUnaText object with updated properties.</param>
        /// <returns>The updated KinaUnaText updated.</returns>
        public async Task<KinaUnaText> UpdateText(int id, KinaUnaText text)
        {
            KinaUnaText textItem = await context.KinaUnaTexts.SingleOrDefaultAsync(t => t.Id == id);
            if (textItem == null) return null;

            textItem.LanguageId = text.LanguageId;
            textItem.Page = text.Page.Trim();
            textItem.Title = text.Title.Trim();
            textItem.Text = text.Text;
            textItem.Updated = DateTime.UtcNow;
            _ = context.KinaUnaTexts.Update(textItem);
            _ = await context.SaveChangesAsync();

            return textItem;
        }

        /// <summary>
        /// Deletes a KinaUnaText from the database.
        /// Also deletes all translations of the text.
        /// </summary>
        /// <param name="id">The Id of the KinaUnaText to delete.</param>
        /// <returns>The deleted KinaUnaText object.</returns>
        public async Task<KinaUnaText> DeleteText(int id)
        {
            KinaUnaText textItem = await context.KinaUnaTexts.AsNoTracking().SingleOrDefaultAsync(t => t.Id == id);
            if (textItem == null) return null;

            {
                List<KinaUnaText> textsList = await context.KinaUnaTexts.Where(t => t.TextId == textItem.TextId).ToListAsync();
                if (textsList.Count != 0)
                {
                    foreach (KinaUnaText textEntity in textsList)
                    {
                        _ = context.KinaUnaTexts.Remove(textEntity);
                    }
                }

                _ = await context.SaveChangesAsync();
            }
            return textItem;
        }

        /// <summary>
        /// Deletes a single KinaUnaText from the database.
        /// Doesn't delete translations of the text.
        /// </summary>
        /// <param name="id">The Id of the KinaUnaText to delete.</param>
        /// <returns>The deleted KinaUnaText object.</returns>
        public async Task<KinaUnaText> DeleteSingleText(int id)
        {
            KinaUnaText textItem = await context.KinaUnaTexts.SingleOrDefaultAsync(t => t.Id == id);
            if (textItem == null) return null;

            _ = context.KinaUnaTexts.Remove(textItem);
            _ = await context.SaveChangesAsync();

            return textItem;
        }
    }
}
