using KinaUna.Data.Contexts;
using KinaUna.Data.Models;
using KinaUnaProgenyApi.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace KinaUnaProgenyApi.Tests.Services
{
    public class TextTranslationServiceTests
    {
        [Fact]
        public async Task GetAllTranslations_Should_Return_List_Of_TextTranslation()
        {
            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>().UseInMemoryDatabase("GetAllTranslations_Should_Return_List_Of_KinaUnaText").Options;
            await using ProgenyDbContext context = new ProgenyDbContext(dbOptions);

            KinaUnaLanguage language1 = new KinaUnaLanguage {Name = "Language1", Code = "Code1", Icon = "Icon1", IconLink = "IconLink1"};
            KinaUnaLanguage language2 = new KinaUnaLanguage { Name = "Language2", Code = "Code2", Icon = "Icon2", IconLink = "IconLink2" };

            context.Add(language1);
            context.Add(language2);

            TextTranslation textTranslation1 = new TextTranslation { LanguageId = 1, Page = "Page1", Word="Word1", Translation = "Translation1.1"};
            TextTranslation textTranslation2 = new TextTranslation { LanguageId = 2, Page = "Page1", Word = "Word1", Translation = "Translation1.2" };
            TextTranslation textTranslation3 = new TextTranslation { LanguageId = 1, Page = "Page1", Word = "Word2", Translation = "Translation2.1" };
            TextTranslation textTranslation4 = new TextTranslation { LanguageId = 2, Page = "Page1", Word = "Word2", Translation = "Translation2.2" };

            context.Add(textTranslation1);
            context.Add(textTranslation2);
            context.Add(textTranslation3);
            context.Add(textTranslation4);

            await context.SaveChangesAsync();
            
            TextTranslationService textTranslationService = new TextTranslationService(context);

            List<TextTranslation> translationsList = await textTranslationService.GetAllTranslations(1);
            
            TextTranslation firstTextTranslation = translationsList.First();

            Assert.NotNull(translationsList);
            Assert.IsType<List<TextTranslation>>(translationsList);
            Assert.Equal(2, translationsList.Count);
            Assert.NotNull(firstTextTranslation);
            Assert.IsType<TextTranslation>(firstTextTranslation);
        }

        [Fact]
        public async Task GetTranslationById_Returns_TextTranslation_Object_When_Id_Is_Valid()
        {
            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>().UseInMemoryDatabase("GetSleep_Returns_Sleep_Object_When_Id_Is_Valid").Options;
            await using ProgenyDbContext context = new ProgenyDbContext(dbOptions);

            KinaUnaLanguage language1 = new KinaUnaLanguage { Name = "Language1", Code = "Code1", Icon = "Icon1", IconLink = "IconLink1" };
            KinaUnaLanguage language2 = new KinaUnaLanguage { Name = "Language2", Code = "Code2", Icon = "Icon2", IconLink = "IconLink2" };

            context.Add(language1);
            context.Add(language2);

            TextTranslation textTranslation1 = new TextTranslation { LanguageId = 1, Page = "Page1", Word = "Word1", Translation = "Translation1.1" };
            TextTranslation textTranslation2 = new TextTranslation { LanguageId = 2, Page = "Page1", Word = "Word1", Translation = "Translation1.2" };
            
            context.Add(textTranslation1);
            context.Add(textTranslation2);
            
            await context.SaveChangesAsync();
            
            TextTranslationService textTranslationService = new TextTranslationService(context);

            TextTranslation resultTranslation = await textTranslationService.GetTranslationById(1);
            
            Assert.NotNull(resultTranslation);
            Assert.IsType<TextTranslation>(resultTranslation);
            Assert.Equal(textTranslation1.Page, resultTranslation.Page);
            Assert.Equal(textTranslation1.Word, resultTranslation.Word);
            Assert.Equal(textTranslation1.Translation, resultTranslation.Translation);
            Assert.Equal(textTranslation1.LanguageId, resultTranslation.LanguageId);
        }

        [Fact]
        public async Task GetTranslationById_Returns_Null_When_Id_Is_Invalid()
        {
            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>().UseInMemoryDatabase("GetTranslationById_Returns_Null_When_Id_Is_Invalid").Options;
            await using ProgenyDbContext context = new ProgenyDbContext(dbOptions);

            KinaUnaLanguage language1 = new KinaUnaLanguage { Name = "Language1", Code = "Code1", Icon = "Icon1", IconLink = "IconLink1" };
            KinaUnaLanguage language2 = new KinaUnaLanguage { Name = "Language2", Code = "Code2", Icon = "Icon2", IconLink = "IconLink2" };

            context.Add(language1);
            context.Add(language2);

            TextTranslation textTranslation1 = new TextTranslation { LanguageId = 1, Page = "Page1", Word = "Word1", Translation = "Translation1.1" };
            TextTranslation textTranslation2 = new TextTranslation { LanguageId = 2, Page = "Page1", Word = "Word1", Translation = "Translation1.2" };
            
            context.Add(textTranslation1);
            context.Add(textTranslation2);
            
            await context.SaveChangesAsync();

            TextTranslationService textTranslationService = new TextTranslationService(context);

            TextTranslation resultTranslation = await textTranslationService.GetTranslationById(3);

            Assert.Null(resultTranslation);
        }

        [Fact]
        public async Task GetPageTranslations_Returns_List_Of_TextTranslation_When_Page_Is_Valid()
        {
            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>().UseInMemoryDatabase("GetPageTranslations_Returns_List_Of_TextTranslation_When_Page_Is_Valid").Options;
            await using ProgenyDbContext context = new ProgenyDbContext(dbOptions);

            KinaUnaLanguage language1 = new KinaUnaLanguage { Name = "Language1", Code = "Code1", Icon = "Icon1", IconLink = "IconLink1" };
            KinaUnaLanguage language2 = new KinaUnaLanguage { Name = "Language2", Code = "Code2", Icon = "Icon2", IconLink = "IconLink2" };

            context.Add(language1);
            context.Add(language2);

            TextTranslation textTranslation1 = new TextTranslation { LanguageId = 1, Page = "Page1", Word = "Word1", Translation = "Translation1.1" };
            TextTranslation textTranslation2 = new TextTranslation { LanguageId = 2, Page = "Page1", Word = "Word1", Translation = "Translation1.2" };
            TextTranslation textTranslation3 = new TextTranslation { LanguageId = 1, Page = "Page1", Word = "Word2", Translation = "Translation2.1" };
            TextTranslation textTranslation4 = new TextTranslation { LanguageId = 2, Page = "Page1", Word = "Word2", Translation = "Translation2.2" };

            context.Add(textTranslation1);
            context.Add(textTranslation2);
            context.Add(textTranslation3);
            context.Add(textTranslation4);

            await context.SaveChangesAsync();

            TextTranslationService textTranslationService = new TextTranslationService(context);

            List<TextTranslation> resultTranslationsList = await textTranslationService.GetPageTranslations(1, "Page1");

            Assert.NotNull(resultTranslationsList);
            Assert.IsType<List<TextTranslation>>(resultTranslationsList);
            Assert.Equal(2, resultTranslationsList.Count);
        }

        [Fact]
        public async Task GetPageTranslations_Returns_Empty_List_Of_TextTranslation_When_Page_Is_Invalid()
        {
            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>().UseInMemoryDatabase("GetPageTranslations_Returns_Empty_List_Of_TextTranslation_When_Page_Is_Invalid").Options;
            await using ProgenyDbContext context = new ProgenyDbContext(dbOptions);

            KinaUnaLanguage language1 = new KinaUnaLanguage { Name = "Language1", Code = "Code1", Icon = "Icon1", IconLink = "IconLink1" };
            KinaUnaLanguage language2 = new KinaUnaLanguage { Name = "Language2", Code = "Code2", Icon = "Icon2", IconLink = "IconLink2" };

            context.Add(language1);
            context.Add(language2);

            TextTranslation textTranslation1 = new TextTranslation { LanguageId = 1, Page = "Page1", Word = "Word1", Translation = "Translation1.1" };
            TextTranslation textTranslation2 = new TextTranslation { LanguageId = 2, Page = "Page1", Word = "Word1", Translation = "Translation1.2" };
            TextTranslation textTranslation3 = new TextTranslation { LanguageId = 1, Page = "Page1", Word = "Word2", Translation = "Translation2.1" };
            TextTranslation textTranslation4 = new TextTranslation { LanguageId = 2, Page = "Page1", Word = "Word2", Translation = "Translation2.2" };

            context.Add(textTranslation1);
            context.Add(textTranslation2);
            context.Add(textTranslation3);
            context.Add(textTranslation4);

            await context.SaveChangesAsync();

            TextTranslationService textTranslationService = new TextTranslationService(context);

            List<TextTranslation> resultTranslationsList = await textTranslationService.GetPageTranslations(1, "Page2");

            Assert.NotNull(resultTranslationsList);
            Assert.IsType<List<TextTranslation>>(resultTranslationsList);
            Assert.Empty(resultTranslationsList);
        }

        [Fact]
        public async Task GetTranslationByWord_Returns_TextTranslation_Object_When_Word_And_Page_Is_Valid()
        {
            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>().UseInMemoryDatabase("GetTranslationByWord_Returns_TextTranslation_Object_When_Word_And_Page_Is_Valid").Options;
            await using ProgenyDbContext context = new ProgenyDbContext(dbOptions);

            KinaUnaLanguage language1 = new KinaUnaLanguage { Name = "Language1", Code = "Code1", Icon = "Icon1", IconLink = "IconLink1" };
            KinaUnaLanguage language2 = new KinaUnaLanguage { Name = "Language2", Code = "Code2", Icon = "Icon2", IconLink = "IconLink2" };

            context.Add(language1);
            context.Add(language2);

            TextTranslation textTranslation1 = new TextTranslation { LanguageId = 1, Page = "Page1", Word = "Word1", Translation = "Translation1.1" };
            TextTranslation textTranslation2 = new TextTranslation { LanguageId = 2, Page = "Page1", Word = "Word1", Translation = "Translation1.2" };

            context.Add(textTranslation1);
            context.Add(textTranslation2);

            await context.SaveChangesAsync();

            TextTranslationService textTranslationService = new TextTranslationService(context);

            TextTranslation resultTranslation = await textTranslationService.GetTranslationByWord("Word1", "Page1", 1);

            Assert.NotNull(resultTranslation);
            Assert.IsType<TextTranslation>(resultTranslation);
            Assert.Equal(textTranslation1.Page, resultTranslation.Page);
            Assert.Equal(textTranslation1.Word, resultTranslation.Word);
            Assert.Equal(textTranslation1.Translation, resultTranslation.Translation);
            Assert.Equal(textTranslation1.LanguageId, resultTranslation.LanguageId);
        }

        [Fact]
        public async Task GetTranslationByWord_Returns_Null_When_Word_Or_Page_Is_Invalid()
        {
            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>().UseInMemoryDatabase("GetTranslationByWord_Returns_Null_When_Word_Or_Page_Is_Invalid").Options;
            await using ProgenyDbContext context = new ProgenyDbContext(dbOptions);

            KinaUnaLanguage language1 = new KinaUnaLanguage { Name = "Language1", Code = "Code1", Icon = "Icon1", IconLink = "IconLink1" };
            KinaUnaLanguage language2 = new KinaUnaLanguage { Name = "Language2", Code = "Code2", Icon = "Icon2", IconLink = "IconLink2" };

            context.Add(language1);
            context.Add(language2);

            TextTranslation textTranslation1 = new TextTranslation { LanguageId = 1, Page = "Page1", Word = "Word1", Translation = "Translation1.1" };
            TextTranslation textTranslation2 = new TextTranslation { LanguageId = 2, Page = "Page1", Word = "Word1", Translation = "Translation1.2" };

            context.Add(textTranslation1);
            context.Add(textTranslation2);

            await context.SaveChangesAsync();

            TextTranslationService textTranslationService = new TextTranslationService(context);

            TextTranslation resultTranslation = await textTranslationService.GetTranslationByWord("Word2", "Page1", 1);
            TextTranslation resultTranslation2 = await textTranslationService.GetTranslationByWord("Word1", "Page2", 1);
            
            Assert.Null(resultTranslation);
            Assert.Null(resultTranslation2);
        }

        [Fact]
        public async Task AddTranslation_Should_Save_TextTranslation()
        {
            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>().UseInMemoryDatabase("AddTranslation_Should_Save_TextTranslation").Options;
            await using ProgenyDbContext context = new ProgenyDbContext(dbOptions);

            KinaUnaLanguage language1 = new KinaUnaLanguage { Name = "Language1", Code = "Code1", Icon = "Icon1", IconLink = "IconLink1" };
            KinaUnaLanguage language2 = new KinaUnaLanguage { Name = "Language2", Code = "Code2", Icon = "Icon2", IconLink = "IconLink2" };

            context.Add(language1);
            context.Add(language2);

            TextTranslation textTranslation1 = new TextTranslation { LanguageId = 1, Page = "Page1", Word = "Word1", Translation = "Translation1.1" };
            TextTranslation textTranslation2 = new TextTranslation { LanguageId = 2, Page = "Page1", Word = "Word1", Translation = "Translation1.2" };

            context.Add(textTranslation1);
            context.Add(textTranslation2);

            await context.SaveChangesAsync();
            
            TextTranslationService textTranslationService = new TextTranslationService(context);

            TextTranslation textTranslationToAdd = new TextTranslation { LanguageId = 1, Page = "Page2", Word = "Word2", Translation = "Translation2.1"};

            TextTranslation addedTextTranslation = await textTranslationService.AddTranslation(textTranslationToAdd);
            TextTranslation? dbTextTranslation = await context.TextTranslations.AsNoTracking().SingleOrDefaultAsync(tt => tt.Id == addedTextTranslation.Id);
            TextTranslation savedTextTranslation = await textTranslationService.GetTranslationById(addedTextTranslation.Id);

            Assert.NotNull(addedTextTranslation);
            Assert.IsType<TextTranslation>(addedTextTranslation);
            Assert.Equal(textTranslationToAdd.Page, addedTextTranslation.Page);
            Assert.Equal(textTranslationToAdd.Word, addedTextTranslation.Word);
            Assert.Equal(textTranslationToAdd.Translation, addedTextTranslation.Translation);
            Assert.Equal(textTranslationToAdd.LanguageId, addedTextTranslation.LanguageId);

            if (dbTextTranslation != null)
            {
                Assert.IsType<TextTranslation>(dbTextTranslation);
                Assert.Equal(textTranslationToAdd.Page, dbTextTranslation.Page);
                Assert.Equal(textTranslationToAdd.Word, dbTextTranslation.Word);
                Assert.Equal(textTranslationToAdd.Translation, dbTextTranslation.Translation);
                Assert.Equal(textTranslationToAdd.LanguageId, dbTextTranslation.LanguageId);
            }
            Assert.NotNull(savedTextTranslation);
            Assert.IsType<TextTranslation>(savedTextTranslation);
            Assert.Equal(textTranslationToAdd.Page, savedTextTranslation.Page);
            Assert.Equal(textTranslationToAdd.Word, savedTextTranslation.Word);
            Assert.Equal(textTranslationToAdd.Translation, savedTextTranslation.Translation);
            Assert.Equal(textTranslationToAdd.LanguageId, savedTextTranslation.LanguageId);
        }

        [Fact]
        public async Task AddTranslation_Should_Add_TextTranslation_For_All_Languages()
        {
            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>().UseInMemoryDatabase("AddTranslation_Should_Save_TextTranslation").Options;
            await using ProgenyDbContext context = new ProgenyDbContext(dbOptions);

            KinaUnaLanguage language1 = new KinaUnaLanguage { Name = "Language1", Code = "Code1", Icon = "Icon1", IconLink = "IconLink1" };
            KinaUnaLanguage language2 = new KinaUnaLanguage { Name = "Language2", Code = "Code2", Icon = "Icon2", IconLink = "IconLink2" };
            KinaUnaLanguage language3 = new KinaUnaLanguage { Name = "Language3", Code = "Code3", Icon = "Icon3", IconLink = "IconLink3" };

            context.Add(language1);
            context.Add(language2);
            context.Add(language3);
            TextTranslation textTranslation1 = new TextTranslation { LanguageId = 1, Page = "Page1", Word = "Word1", Translation = "Translation1.1" };
            TextTranslation textTranslation2 = new TextTranslation { LanguageId = 2, Page = "Page1", Word = "Word1", Translation = "Translation1.2" };

            context.Add(textTranslation1);
            context.Add(textTranslation2);

            await context.SaveChangesAsync();

            TextTranslationService textTranslationService = new TextTranslationService(context);

            TextTranslation textTranslationToAdd = new TextTranslation { LanguageId = 1, Page = "Page2", Word = "Word2", Translation = "Translation2.1" };

            TextTranslation addedTextTranslation = await textTranslationService.AddTranslation(textTranslationToAdd);
            List<TextTranslation> allLanguageVersionsOfAddedTextTranslation = await context.TextTranslations.Where(tt => tt.Page == textTranslationToAdd.Page && tt.Word == textTranslationToAdd.Word).ToListAsync();

            Assert.NotEmpty(allLanguageVersionsOfAddedTextTranslation);
            Assert.Equal(3, allLanguageVersionsOfAddedTextTranslation.Count);
        }

        [Fact]
        public async Task UpdateTranslation_Should_Save_TextTranslation()
        {
            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>().UseInMemoryDatabase("UpdateTranslation_Should_Save_TextTranslation").Options;
            await using ProgenyDbContext context = new ProgenyDbContext(dbOptions);

            KinaUnaLanguage language1 = new KinaUnaLanguage { Name = "Language1", Code = "Code1", Icon = "Icon1", IconLink = "IconLink1" };
            KinaUnaLanguage language2 = new KinaUnaLanguage { Name = "Language2", Code = "Code2", Icon = "Icon2", IconLink = "IconLink2" };

            context.Add(language1);
            context.Add(language2);

            TextTranslation textTranslation1 = new TextTranslation { LanguageId = 1, Page = "Page1", Word = "Word1", Translation = "Translation1.1" };
            TextTranslation textTranslation2 = new TextTranslation { LanguageId = 2, Page = "Page1", Word = "Word1", Translation = "Translation1.2" };
            
            context.Add(textTranslation1);
            context.Add(textTranslation2);
            
            await context.SaveChangesAsync();
            TextTranslationService textTranslationService = new TextTranslationService(context);
            
            TextTranslation textTranslationToUpdate = await textTranslationService.GetTranslationById(1);
            textTranslationToUpdate.Translation = "Translation1.1 Updated";
            TextTranslation updatedTextTranslation = await textTranslationService.UpdateTranslation(1, textTranslationToUpdate);
            TextTranslation? dbTextTranslation = await context.TextTranslations.AsNoTracking().SingleOrDefaultAsync(tt => tt.Id == 1);
            TextTranslation savedTextTranslation = await textTranslationService.GetTranslationById(1);

            Assert.NotNull(updatedTextTranslation);
            Assert.IsType<TextTranslation>(updatedTextTranslation);
            Assert.NotEqual(0, updatedTextTranslation.Id);
            Assert.Equal("Translation1.1 Updated", updatedTextTranslation.Translation);
            Assert.Equal("Word1", updatedTextTranslation.Word);
            Assert.Equal("Page1", updatedTextTranslation.Page);

            if (dbTextTranslation != null)
            {
                Assert.IsType<TextTranslation>(dbTextTranslation);
                Assert.NotEqual(0, dbTextTranslation.Id);
                Assert.Equal("Translation1.1 Updated", dbTextTranslation.Translation);
                Assert.Equal("Word1", dbTextTranslation.Word);
                Assert.Equal("Page1", dbTextTranslation.Page);
            }

            Assert.NotNull(savedTextTranslation);
            Assert.IsType<TextTranslation>(savedTextTranslation);
            Assert.NotEqual(0, savedTextTranslation.Id);
            Assert.Equal("Translation1.1 Updated", savedTextTranslation.Translation);
            Assert.Equal("Word1", savedTextTranslation.Word);
            Assert.Equal("Page1", savedTextTranslation.Page);
        }

        [Fact]
        public async Task DeleteTranslation_Should_Remove_All_Language_Versions_Of_The_TextTranslation()
        {
            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>().UseInMemoryDatabase("DeleteTranslation_Should_Remove_All_Language_Versions_Of_The_TextTranslation").Options;
            await using ProgenyDbContext context = new ProgenyDbContext(dbOptions);

            KinaUnaLanguage language1 = new KinaUnaLanguage { Name = "Language1", Code = "Code1", Icon = "Icon1", IconLink = "IconLink1" };
            KinaUnaLanguage language2 = new KinaUnaLanguage { Name = "Language2", Code = "Code2", Icon = "Icon2", IconLink = "IconLink2" };
            KinaUnaLanguage language3 = new KinaUnaLanguage { Name = "Language3", Code = "Code3", Icon = "Icon3", IconLink = "IconLink3" };
            KinaUnaLanguage language4 = new KinaUnaLanguage { Name = "Language4", Code = "Code4", Icon = "Icon4", IconLink = "IconLink4" };

            context.Add(language1);
            context.Add(language2);
            context.Add(language3);
            context.Add(language4);

            TextTranslation textTranslation1 = new TextTranslation { LanguageId = 1, Page = "Page1", Word = "Word1", Translation = "Translation1.1" };
            TextTranslation textTranslation2 = new TextTranslation { LanguageId = 2, Page = "Page1", Word = "Word1", Translation = "Translation1.2" };
            TextTranslation textTranslation3 = new TextTranslation { LanguageId = 3, Page = "Page1", Word = "Word1", Translation = "Translation1.3" };
            TextTranslation textTranslation4 = new TextTranslation { LanguageId = 4, Page = "Page1", Word = "Word1", Translation = "Translation1.4" };

            TextTranslation textTranslation5 = new TextTranslation { LanguageId = 1, Page = "Page1", Word = "Word2", Translation = "Translation2.1" };
            TextTranslation textTranslation6 = new TextTranslation { LanguageId = 2, Page = "Page1", Word = "Word2", Translation = "Translation2.2" };
            TextTranslation textTranslation7 = new TextTranslation { LanguageId = 3, Page = "Page1", Word = "Word2", Translation = "Translation2.3" };
            TextTranslation textTranslation8 = new TextTranslation { LanguageId = 4, Page = "Page1", Word = "Word2", Translation = "Translation2.4" };

            context.Add(textTranslation1);
            context.Add(textTranslation2);
            context.Add(textTranslation3);
            context.Add(textTranslation4);
            context.Add(textTranslation5);
            context.Add(textTranslation6);
            context.Add(textTranslation7);
            context.Add(textTranslation8);

            await context.SaveChangesAsync();

            TextTranslationService textTranslationService = new TextTranslationService(context);

            TextTranslation textTranslationToDelete = await textTranslationService.GetTranslationById(1);
            List<TextTranslation> textTranslationListBeforeDelete = await context.TextTranslations.Where(tt => tt.Page == textTranslationToDelete.Page && tt.Word == textTranslationToDelete.Word).ToListAsync();
            int allTextTranslationsCountBeforeDelete = context.TextTranslations.Count();
            await textTranslationService.DeleteTranslation(1);
            List<TextTranslation> textTranslationListAfterDelete = await context.TextTranslations.Where(tt => tt.Page == textTranslationToDelete.Page && tt.Word == textTranslationToDelete.Word).ToListAsync();
            int allTextTranslationsCountAfterDelete = context.TextTranslations.Count();
            TextTranslation? deletedTextTranslation = await context.TextTranslations.SingleOrDefaultAsync(tt => tt.Id == 1);

            Assert.Null(deletedTextTranslation);
            Assert.Equal(4, textTranslationListBeforeDelete.Count);
            Assert.Empty(textTranslationListAfterDelete);
            Assert.Equal(8, allTextTranslationsCountBeforeDelete);
            Assert.Equal(4, allTextTranslationsCountAfterDelete);
        }

        [Fact]
        public async Task DeleteSingleTranslation_Should_Remove_Only_1_Language_Version_Of_The_TextTranslation()
        {
            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>().UseInMemoryDatabase("DeleteSingleTranslation_Should_Remove_Only_1_Language_Version_Of_The_TextTranslation").Options;
            await using ProgenyDbContext context = new ProgenyDbContext(dbOptions);

            KinaUnaLanguage language1 = new KinaUnaLanguage { Name = "Language1", Code = "Code1", Icon = "Icon1", IconLink = "IconLink1" };
            KinaUnaLanguage language2 = new KinaUnaLanguage { Name = "Language2", Code = "Code2", Icon = "Icon2", IconLink = "IconLink2" };
            KinaUnaLanguage language3 = new KinaUnaLanguage { Name = "Language3", Code = "Code3", Icon = "Icon3", IconLink = "IconLink3" };
            KinaUnaLanguage language4 = new KinaUnaLanguage { Name = "Language4", Code = "Code4", Icon = "Icon4", IconLink = "IconLink4" };

            context.Add(language1);
            context.Add(language2);
            context.Add(language3);
            context.Add(language4);

            TextTranslation textTranslation1 = new TextTranslation { LanguageId = 1, Page = "Page1", Word = "Word1", Translation = "Translation1.1" };
            TextTranslation textTranslation2 = new TextTranslation { LanguageId = 2, Page = "Page1", Word = "Word1", Translation = "Translation1.2" };
            TextTranslation textTranslation3 = new TextTranslation { LanguageId = 3, Page = "Page1", Word = "Word1", Translation = "Translation1.3" };
            TextTranslation textTranslation4 = new TextTranslation { LanguageId = 4, Page = "Page1", Word = "Word1", Translation = "Translation1.4" };

            TextTranslation textTranslation5 = new TextTranslation { LanguageId = 1, Page = "Page1", Word = "Word2", Translation = "Translation2.1" };
            TextTranslation textTranslation6 = new TextTranslation { LanguageId = 2, Page = "Page1", Word = "Word2", Translation = "Translation2.2" };
            TextTranslation textTranslation7 = new TextTranslation { LanguageId = 3, Page = "Page1", Word = "Word2", Translation = "Translation2.3" };
            TextTranslation textTranslation8 = new TextTranslation { LanguageId = 4, Page = "Page1", Word = "Word2", Translation = "Translation2.4" };

            context.Add(textTranslation1);
            context.Add(textTranslation2);
            context.Add(textTranslation3);
            context.Add(textTranslation4);
            context.Add(textTranslation5);
            context.Add(textTranslation6);
            context.Add(textTranslation7);
            context.Add(textTranslation8);

            await context.SaveChangesAsync();

            TextTranslationService textTranslationService = new TextTranslationService(context);

            TextTranslation textTranslationToDelete = await textTranslationService.GetTranslationById(1);
            List<TextTranslation> textTranslationListBeforeDelete = await context.TextTranslations.Where(tt => tt.Page == textTranslationToDelete.Page && tt.Word == textTranslationToDelete.Word).ToListAsync();
            int allTextTranslationsCountBeforeDelete = context.TextTranslations.Count();
            await textTranslationService.DeleteSingleTranslation(1);
            List<TextTranslation> textTranslationListAfterDelete = await context.TextTranslations.Where(tt => tt.Page == textTranslationToDelete.Page && tt.Word == textTranslationToDelete.Word).ToListAsync();
            int allTextTranslationsCountAfterDelete = context.TextTranslations.Count();
            TextTranslation? deletedTextTranslation = await context.TextTranslations.SingleOrDefaultAsync(tt => tt.Id == 1);

            Assert.Null(deletedTextTranslation);
            Assert.Equal(4, textTranslationListBeforeDelete.Count);
            Assert.NotEmpty(textTranslationListAfterDelete);
            Assert.Equal(8, allTextTranslationsCountBeforeDelete);
            Assert.Equal(7, allTextTranslationsCountAfterDelete);
        }
    }
}
