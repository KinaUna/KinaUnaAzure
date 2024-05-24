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
        public async Task<KinaUnaText> GetTextByTitle(string title, string page, int languageId)
        {
            title = title.Trim();
            page = page.Trim();
            KinaUnaText textItem = await context.KinaUnaTexts.AsNoTracking().FirstOrDefaultAsync(t => t.Title.ToUpper() == title.Trim().ToUpper() && t.Page.ToUpper() == page.Trim().ToUpper() && t.LanguageId == languageId);

            return textItem;
        }

        public async Task<KinaUnaText> GetTextById(int id)
        {
            KinaUnaText kinaUnaText = await context.KinaUnaTexts.AsNoTracking().SingleOrDefaultAsync(t => t.Id == id);
            return kinaUnaText;
        }

        public async Task<KinaUnaText> GetTextByTextId(int textId, int languageId)
        {
            KinaUnaText kinaUnaText = await context.KinaUnaTexts.AsNoTracking().SingleOrDefaultAsync(t => t.TextId == textId && t.LanguageId == languageId);
            return kinaUnaText;
        }

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

        public async Task<List<KinaUnaText>> GetAllPageTextsList(int languageId)
        {
            if (languageId == 0)
            {
                languageId = 1;
            }
            List<KinaUnaText> texts = await context.KinaUnaTexts.AsNoTracking().Where(t => t.LanguageId == languageId).ToListAsync();
            return texts;
        }

        public async Task CheckLanguages()
        {
            List<KinaUnaTextNumber> textNumbers = await context.KinaUnaTextNumbers.AsNoTracking().ToListAsync();
            List<KinaUnaLanguage> languages = await context.Languages.AsNoTracking().ToListAsync();

            foreach (KinaUnaTextNumber tNumber in textNumbers)
            {
                List<KinaUnaText> texts = await context.KinaUnaTexts.AsNoTracking().Where(t => t.TextId == tNumber.Id).OrderBy(t => t.LanguageId).ToListAsync();
                if (texts.Any() && texts.Count < languages.Count)
                {
                    foreach (KinaUnaLanguage lang in languages)
                    {
                        KinaUnaText textItem = await context.KinaUnaTexts.SingleOrDefaultAsync(t => t.TextId == tNumber.Id && t.LanguageId == lang.Id);
                        if (textItem == null)
                        {
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
            }
        }

        public async Task<KinaUnaText> AddText(KinaUnaText text)
        {
            text.Title = text.Title.Trim();
            text.Page = text.Page.Trim();

            if (text.Title.StartsWith("__"))
            {
                // Title's starting with double underscore are considered unique system pages, so we make sure no other text has the same title on a page.
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

        private async Task<KinaUnaText> AddSystemPageText(KinaUnaText text)
        {
            KinaUnaText existingTextItem = await context.KinaUnaTexts.SingleOrDefaultAsync(t => t.Title == text.Title && t.Page == text.Page && t.LanguageId == text.LanguageId);
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
                context.KinaUnaTexts.Add(text);
                _ = await context.SaveChangesAsync();

                return text;

            }
            else
            {
                existingTextItem.Title = text.Title;
                existingTextItem.Text = text.Text;
                existingTextItem.Page = text.Page;
                existingTextItem.Created = DateTime.UtcNow;
                existingTextItem.Updated = existingTextItem.Created;
                _ = context.KinaUnaTexts.Update(existingTextItem);
                _ = await context.SaveChangesAsync();

                return existingTextItem;
            }
        }

        private async Task AddTextForOtherLanguages(KinaUnaText text)
        {
            List<KinaUnaLanguage> languages = await context.Languages.AsNoTracking().ToListAsync();
            foreach (KinaUnaLanguage lang in languages)
            {
                if (lang.Id != text.LanguageId)
                {
                    KinaUnaText textItem = await context.KinaUnaTexts.SingleOrDefaultAsync(t => t.TextId == text.TextId && t.LanguageId == lang.Id);
                    if (textItem == null)
                    {
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
            }
        }

        public async Task<KinaUnaText> UpdateText(int id, KinaUnaText text)
        {
            KinaUnaText textItem = await context.KinaUnaTexts.SingleOrDefaultAsync(t => t.Id == id);
            if (textItem != null)
            {
                textItem.LanguageId = text.LanguageId;
                textItem.Page = text.Page.Trim();
                textItem.Title = text.Title.Trim();
                textItem.Text = text.Text;
                textItem.Updated = DateTime.UtcNow;
                _ = context.KinaUnaTexts.Update(textItem);
                _ = await context.SaveChangesAsync();
            }

            return textItem;
        }

        public async Task<KinaUnaText> DeleteText(int id)
        {
            KinaUnaText textItem = await context.KinaUnaTexts.AsNoTracking().SingleOrDefaultAsync(t => t.Id == id);
            if (textItem != null)
            {
                List<KinaUnaText> textsList = await context.KinaUnaTexts.Where(t => t.TextId == textItem.TextId).ToListAsync();
                if (textsList.Any())
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

        public async Task<KinaUnaText> DeleteSingleText(int id)
        {
            KinaUnaText textItem = await context.KinaUnaTexts.SingleOrDefaultAsync(t => t.Id == id);
            if (textItem != null)
            {
                _ = context.KinaUnaTexts.Remove(textItem);
                _ = await context.SaveChangesAsync();
            }

            return textItem;
        }
    }
}
