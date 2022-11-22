using KinaUna.Data.Contexts;
using KinaUna.Data.Models;
using KinaUnaProgenyApi.Services;
using Microsoft.EntityFrameworkCore;

namespace KinaUnaProgenyApi.Tests.Services
{
    public class LanguageServiceTests
    {
        [Fact]
        public async Task GetLanguagesList_Should_Return_List_Of_KinaUnaLanguage_When_Any_Language_Exist()
        {
            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>().UseInMemoryDatabase("GetLanguagesList_Should_Return_List_Of_KinaUnaLanguage_When_Any_Language_Exist").Options;
            await using ProgenyDbContext context = new ProgenyDbContext(dbOptions);

            KinaUnaLanguage language1 = new KinaUnaLanguage
            {
                Name = "Language1", Code = "A", Icon = "A"
            };

            KinaUnaLanguage language2 = new KinaUnaLanguage
            {
                Name = "Language2",
                Code = "B",
                Icon = "B"
            };

            context.Add(language1);
            context.Add(language2);
            await context.SaveChangesAsync();

            LanguageService languageService = new LanguageService(context);

            List<KinaUnaLanguage> allLanguages = await languageService.GetAllLanguages();
            KinaUnaLanguage firstLanguage = allLanguages.First();

            Assert.NotNull(allLanguages);
            Assert.IsType<List<KinaUnaLanguage>>(allLanguages);
            Assert.Equal(2, allLanguages.Count);
            Assert.NotNull(firstLanguage);
            Assert.IsType<KinaUnaLanguage>(firstLanguage);
        }

        [Fact]
        public async Task GetLanguagesList_Should_Return_Empty_List_Of_KinaUnaLanguage_When_No_Languages_Exist()
        {

            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>().UseInMemoryDatabase("GetLanguagesList_Should_Return_Empty_List_Of_Language_When_Progeny_Has_No_Saved_Languages").Options;
            await using ProgenyDbContext context = new ProgenyDbContext(dbOptions);
            
            LanguageService languageService = new LanguageService(context);

            List<KinaUnaLanguage> languagesList = await languageService.GetAllLanguages();
            
            Assert.NotNull(languagesList);
            Assert.IsType<List<KinaUnaLanguage>>(languagesList);
            Assert.Empty(languagesList);
        }

        [Fact]
        public async Task GetLanguage_Should_Return_KinaUnaLanguage_Object_When_Id_Is_Valid()
        {
            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>().UseInMemoryDatabase("GetLanguage_Should_Return_KinaUnaLanguage_Object_When_Id_Is_Valid").Options;
            await using ProgenyDbContext context = new ProgenyDbContext(dbOptions);

            KinaUnaLanguage language1 = new KinaUnaLanguage
            {
                Name = "Language1",
                Code = "A",
                Icon = "A"
            };

            KinaUnaLanguage language2 = new KinaUnaLanguage
            {
                Name = "Language2",
                Code = "B",
                Icon = "B"
            };

            context.Add(language1);
            context.Add(language2);
            await context.SaveChangesAsync();

            LanguageService languageService = new LanguageService(context);

            KinaUnaLanguage resultLanguage1 = await languageService.GetLanguage(1);
           
            Assert.NotNull(resultLanguage1);
            Assert.IsType<KinaUnaLanguage>(resultLanguage1);
            Assert.Equal(language1.Name, resultLanguage1.Name);
            Assert.Equal(language1.Code, resultLanguage1.Code);
            Assert.Equal(language1.Icon, resultLanguage1.Icon);
        }

        [Fact]
        public async Task GetLanguage_Should_Return_Null_When_Id_Is_Invalid()
        {
            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>().UseInMemoryDatabase("GetLanguage_Should_Return_Null_When_Id_Is_Invalid").Options;
            await using ProgenyDbContext context = new ProgenyDbContext(dbOptions);

            KinaUnaLanguage language1 = new KinaUnaLanguage
            {
                Name = "Language1",
                Code = "A",
                Icon = "A"
            };

            context.Add(language1);
            await context.SaveChangesAsync();

            LanguageService languageService = new LanguageService(context);

            KinaUnaLanguage resultLanguage1 = await languageService.GetLanguage(2);
            
            Assert.Null(resultLanguage1);
        }

        [Fact]
        public async Task AddLanguage_Should_Save_KinaUnaLanguage()
        {
            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>().UseInMemoryDatabase("AddLanguage_Should_Save_KinaUnaLanguage").Options;
            await using ProgenyDbContext context = new ProgenyDbContext(dbOptions);

            KinaUnaLanguage language1 = new KinaUnaLanguage
            {
                Name = "Language1",
                Code = "A",
                Icon = "A"
            };

            context.Add(language1);
            await context.SaveChangesAsync();

            LanguageService languageService = new LanguageService(context);

            KinaUnaLanguage languageToAdd = new KinaUnaLanguage
            {
                Name = "Language2",
                Code = "B",
                Icon = "B"
            };

            KinaUnaLanguage addedLanguage = await languageService.AddLanguage(languageToAdd);
            KinaUnaLanguage? dbLanguage = await context.Languages.AsNoTracking().SingleOrDefaultAsync(f => f.Id == addedLanguage.Id);
            KinaUnaLanguage savedLanguage = await languageService.GetLanguage(addedLanguage.Id);

            Assert.NotNull(addedLanguage);
            Assert.IsType<KinaUnaLanguage>(addedLanguage);
            Assert.Equal(languageToAdd.Name, addedLanguage.Name);
            Assert.Equal(languageToAdd.Code, addedLanguage.Code);
            Assert.Equal(languageToAdd.Icon, addedLanguage.Icon);

            if (dbLanguage != null)
            {
                Assert.IsType<KinaUnaLanguage>(dbLanguage);
                Assert.Equal(languageToAdd.Name, dbLanguage.Name);
                Assert.Equal(languageToAdd.Code, dbLanguage.Code);
                Assert.Equal(languageToAdd.Icon, dbLanguage.Icon);
            }
            Assert.NotNull(savedLanguage);
            Assert.IsType<KinaUnaLanguage>(savedLanguage);
            Assert.Equal(languageToAdd.Name, savedLanguage.Name);
            Assert.Equal(languageToAdd.Code, savedLanguage.Code);
            Assert.Equal(languageToAdd.Icon, savedLanguage.Icon);

        }

        [Fact]
        public async Task UpdateLanguage_Should_Save_KinaUnaLanguage()
        {
            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>().UseInMemoryDatabase("UpdateLanguage_Should_Save_KinaUnaLanguage").Options;
            await using ProgenyDbContext context = new ProgenyDbContext(dbOptions);

            KinaUnaLanguage language1 = new KinaUnaLanguage
            {
                Name = "Language1",
                Code = "A",
                Icon = "A"
            };

            KinaUnaLanguage language2 = new KinaUnaLanguage
            {
                Name = "Language2",
                Code = "B",
                Icon = "B"
            };
            context.Add(language1);
            context.Add(language2);
            await context.SaveChangesAsync();

            LanguageService languageService = new LanguageService(context);

            KinaUnaLanguage languageToUpdate = await languageService.GetLanguage(1);
            languageToUpdate.Code = "C";
            languageToUpdate.Icon = "C";
            KinaUnaLanguage updatedLanguage = await languageService.UpdateLanguage(languageToUpdate);
            KinaUnaLanguage? dbLanguage = await context.Languages.AsNoTracking().SingleOrDefaultAsync(f => f.Id == 1);
            KinaUnaLanguage savedLanguage = await languageService.GetLanguage(1);

            Assert.NotNull(updatedLanguage);
            Assert.IsType<KinaUnaLanguage>(updatedLanguage);
            Assert.NotEqual(0, updatedLanguage.Id);
            Assert.Equal("Language1", updatedLanguage.Name);
            Assert.Equal("C", updatedLanguage.Code);
            Assert.Equal("C", updatedLanguage.Icon);

            if (dbLanguage != null)
            {
                Assert.IsType<KinaUnaLanguage>(dbLanguage);
                Assert.NotEqual(0, dbLanguage.Id);
                Assert.Equal("Language1", dbLanguage.Name);
                Assert.Equal("C", dbLanguage.Code);
                Assert.Equal("C", dbLanguage.Icon);
            }

            Assert.NotNull(savedLanguage);
            Assert.IsType<KinaUnaLanguage>(savedLanguage);
            Assert.NotEqual(0, savedLanguage.Id);
            Assert.Equal("Language1", savedLanguage.Name);
            Assert.Equal("C", savedLanguage.Code);
            Assert.Equal("C", savedLanguage.Icon);
        }

        [Fact]
        public async Task DeleteLanguage_Should_Remove_KinaUnaLanguage()
        {
            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>().UseInMemoryDatabase("DeleteLanguage_Should_Remove_KinaUnaLanguage").Options;
            await using ProgenyDbContext context = new ProgenyDbContext(dbOptions);

            KinaUnaLanguage language1 = new KinaUnaLanguage
            {
                Name = "Language1",
                Code = "A",
                Icon = "A"
            };

            KinaUnaLanguage language2 = new KinaUnaLanguage
            {
                Name = "Language2",
                Code = "B",
                Icon = "B"
            };
            context.Add(language1);
            context.Add(language2);
            await context.SaveChangesAsync();
            
            LanguageService languageService = new LanguageService(context);

            int languageItemsCountBeforeDelete = context.Languages.Count();
            KinaUnaLanguage languageToDelete = await languageService.GetLanguage(1);

            await languageService.DeleteLanguage(languageToDelete.Id);
            KinaUnaLanguage? deletedLanguage = await context.Languages.SingleOrDefaultAsync(f => f.Id == 1);
            int languageItemsCountAfterDelete = context.Languages.Count();

            Assert.Null(deletedLanguage);
            Assert.Equal(2, languageItemsCountBeforeDelete);
            Assert.Equal(1, languageItemsCountAfterDelete);
        }

        
    }
}
