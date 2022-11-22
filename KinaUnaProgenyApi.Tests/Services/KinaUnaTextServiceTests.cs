using KinaUna.Data.Contexts;
using KinaUna.Data.Models;
using KinaUnaProgenyApi.Services;
using Microsoft.EntityFrameworkCore;

namespace KinaUnaProgenyApi.Tests.Services
{
    public class KinaUnaTextServiceTests
    {
        [Fact]
        public async Task GetTextByTitle_Should_Return_KinaUnaText_Object_When_Parameters_Are_Valid()
        {
            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>().UseInMemoryDatabase("GetTextByTitle_Should_Return_KinaUnaText_Object_When_Parameters_Are_Valid").Options;
            await using ProgenyDbContext context = new ProgenyDbContext(dbOptions);

            KinaUnaLanguage language1 = new KinaUnaLanguage { Name = "Language1", Code = "Code1", Icon = "Icon1", IconLink = "IconLink1" };
            KinaUnaLanguage language2 = new KinaUnaLanguage { Name = "Language2", Code = "Code2", Icon = "Icon2", IconLink = "IconLink2" };
            KinaUnaLanguage language3 = new KinaUnaLanguage { Name = "Language3", Code = "Code3", Icon = "Icon3", IconLink = "IconLink3" };

            context.Add(language1);
            context.Add(language2);
            context.Add(language3);

            KinaUnaText text1 = new KinaUnaText { LanguageId = 1, Title = "Title1", Page = "Page1", Text = "Text1.1", Created = DateTime.UtcNow, TextId = 1, Updated = DateTime.UtcNow};
            KinaUnaText text2 = new KinaUnaText { LanguageId = 2, Title = "Title1", Page = "Page1", Text = "Text1.2", Created = DateTime.UtcNow, TextId = 1, Updated = DateTime.UtcNow };
            KinaUnaText text3 = new KinaUnaText { LanguageId = 3, Title = "Title1", Page = "Page1", Text = "Text1.3", Created = DateTime.UtcNow, TextId = 1, Updated = DateTime.UtcNow };

            context.Add(text1);
            context.Add(text2);
            context.Add(text3);

            await context.SaveChangesAsync();

            KinaUnaTextService kinaUnaTextService = new KinaUnaTextService(context);

            KinaUnaText resultKinaUnaText = await kinaUnaTextService.GetTextByTitle("Title1", "Page1", 1);

            Assert.NotNull(resultKinaUnaText);
            Assert.IsType<KinaUnaText>(resultKinaUnaText);
            Assert.Equal(text1.Page, resultKinaUnaText.Page);
            Assert.Equal(text1.Title, resultKinaUnaText.Title);
            Assert.Equal(text1.Text, resultKinaUnaText.Text);
            Assert.Equal(text1.LanguageId, resultKinaUnaText.LanguageId);
        }

        [Fact]
        public async Task GetTextByTitle_Should_Return_Null_When_Title_Is_Invalid()
        {
            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>().UseInMemoryDatabase("GetTextByTitle_Should_Return_Null_When_Title_Is_Invalid").Options;
            await using ProgenyDbContext context = new ProgenyDbContext(dbOptions);

            KinaUnaLanguage language1 = new KinaUnaLanguage { Name = "Language1", Code = "Code1", Icon = "Icon1", IconLink = "IconLink1" };
            KinaUnaLanguage language2 = new KinaUnaLanguage { Name = "Language2", Code = "Code2", Icon = "Icon2", IconLink = "IconLink2" };
            KinaUnaLanguage language3 = new KinaUnaLanguage { Name = "Language3", Code = "Code3", Icon = "Icon3", IconLink = "IconLink3" };

            context.Add(language1);
            context.Add(language2);
            context.Add(language3);

            KinaUnaText text1 = new KinaUnaText { LanguageId = 1, Title = "Title1", Page = "Page1", Text = "Text1.1", Created = DateTime.UtcNow, TextId = 1, Updated = DateTime.UtcNow };
            KinaUnaText text2 = new KinaUnaText { LanguageId = 2, Title = "Title1", Page = "Page1", Text = "Text1.2", Created = DateTime.UtcNow, TextId = 1, Updated = DateTime.UtcNow };
            KinaUnaText text3 = new KinaUnaText { LanguageId = 3, Title = "Title1", Page = "Page1", Text = "Text1.3", Created = DateTime.UtcNow, TextId = 1, Updated = DateTime.UtcNow };

            context.Add(text1);
            context.Add(text2);
            context.Add(text3);

            await context.SaveChangesAsync();

            KinaUnaTextService kinaUnaTextService = new KinaUnaTextService(context);

            KinaUnaText resultKinaUnaText = await kinaUnaTextService.GetTextByTitle("Title2", "Page1", 1);

            Assert.Null(resultKinaUnaText);
        }

        [Fact]
        public async Task GetTextByTitle_Should_Return_Null_When_Page_Is_Invalid()
        {
            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>().UseInMemoryDatabase("GetTextByTitle_Should_Return_Null_When_Parameters_Are_Invalid").Options;
            await using ProgenyDbContext context = new ProgenyDbContext(dbOptions);

            KinaUnaLanguage language1 = new KinaUnaLanguage { Name = "Language1", Code = "Code1", Icon = "Icon1", IconLink = "IconLink1" };
            KinaUnaLanguage language2 = new KinaUnaLanguage { Name = "Language2", Code = "Code2", Icon = "Icon2", IconLink = "IconLink2" };
            KinaUnaLanguage language3 = new KinaUnaLanguage { Name = "Language3", Code = "Code3", Icon = "Icon3", IconLink = "IconLink3" };

            context.Add(language1);
            context.Add(language2);
            context.Add(language3);

            KinaUnaText text1 = new KinaUnaText { LanguageId = 1, Title = "Title1", Page = "Page1", Text = "Text1.1", Created = DateTime.UtcNow, TextId = 1, Updated = DateTime.UtcNow };
            KinaUnaText text2 = new KinaUnaText { LanguageId = 2, Title = "Title1", Page = "Page1", Text = "Text1.2", Created = DateTime.UtcNow, TextId = 1, Updated = DateTime.UtcNow };
            KinaUnaText text3 = new KinaUnaText { LanguageId = 3, Title = "Title1", Page = "Page1", Text = "Text1.3", Created = DateTime.UtcNow, TextId = 1, Updated = DateTime.UtcNow };

            context.Add(text1);
            context.Add(text2);
            context.Add(text3);

            await context.SaveChangesAsync();

            KinaUnaTextService kinaUnaTextService = new KinaUnaTextService(context);

            KinaUnaText resultKinaUnaText = await kinaUnaTextService.GetTextByTitle("Title1", "Page2", 1);

            Assert.Null(resultKinaUnaText);
        }

        [Fact]
        public async Task GetTextByTitle_Should_Return_Null_When_Language_Is_Invalid()
        {
            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>().UseInMemoryDatabase("GetTextByTitle_Should_Return_Null_When_Parameters_Are_Invalid").Options;
            await using ProgenyDbContext context = new ProgenyDbContext(dbOptions);

            KinaUnaLanguage language1 = new KinaUnaLanguage { Name = "Language1", Code = "Code1", Icon = "Icon1", IconLink = "IconLink1" };
            KinaUnaLanguage language2 = new KinaUnaLanguage { Name = "Language2", Code = "Code2", Icon = "Icon2", IconLink = "IconLink2" };
            KinaUnaLanguage language3 = new KinaUnaLanguage { Name = "Language3", Code = "Code3", Icon = "Icon3", IconLink = "IconLink3" };

            context.Add(language1);
            context.Add(language2);
            context.Add(language3);

            KinaUnaText text1 = new KinaUnaText { LanguageId = 1, Title = "Title1", Page = "Page1", Text = "Text1.1", Created = DateTime.UtcNow, TextId = 1, Updated = DateTime.UtcNow };
            KinaUnaText text2 = new KinaUnaText { LanguageId = 2, Title = "Title1", Page = "Page1", Text = "Text1.2", Created = DateTime.UtcNow, TextId = 1, Updated = DateTime.UtcNow };
            KinaUnaText text3 = new KinaUnaText { LanguageId = 3, Title = "Title1", Page = "Page1", Text = "Text1.3", Created = DateTime.UtcNow, TextId = 1, Updated = DateTime.UtcNow };

            context.Add(text1);
            context.Add(text2);
            context.Add(text3);

            await context.SaveChangesAsync();

            KinaUnaTextService kinaUnaTextService = new KinaUnaTextService(context);

            KinaUnaText resultKinaUnaText = await kinaUnaTextService.GetTextByTitle("Title1", "Page1", 4);

            Assert.Null(resultKinaUnaText);
        }

        [Fact]
        public async Task GetTextById_Should_Return_KinaUnaText_Object_When_Id_Is_Valid()
        {
            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>().UseInMemoryDatabase("GetTextById_Should_Return_KinaUnaText_Object_When_Id_Is_Valid").Options;
            await using ProgenyDbContext context = new ProgenyDbContext(dbOptions);

            KinaUnaLanguage language1 = new KinaUnaLanguage { Name = "Language1", Code = "Code1", Icon = "Icon1", IconLink = "IconLink1" };
            KinaUnaLanguage language2 = new KinaUnaLanguage { Name = "Language2", Code = "Code2", Icon = "Icon2", IconLink = "IconLink2" };
            KinaUnaLanguage language3 = new KinaUnaLanguage { Name = "Language3", Code = "Code3", Icon = "Icon3", IconLink = "IconLink3" };

            context.Add(language1);
            context.Add(language2);
            context.Add(language3);

            KinaUnaText text1 = new KinaUnaText { LanguageId = 1, Title = "Title1", Page = "Page1", Text = "Text1.1", Created = DateTime.UtcNow, TextId = 1, Updated = DateTime.UtcNow };
            KinaUnaText text2 = new KinaUnaText { LanguageId = 2, Title = "Title1", Page = "Page1", Text = "Text1.2", Created = DateTime.UtcNow, TextId = 1, Updated = DateTime.UtcNow };
            KinaUnaText text3 = new KinaUnaText { LanguageId = 3, Title = "Title1", Page = "Page1", Text = "Text1.3", Created = DateTime.UtcNow, TextId = 1, Updated = DateTime.UtcNow };

            context.Add(text1);
            context.Add(text2);
            context.Add(text3);

            await context.SaveChangesAsync();

            KinaUnaTextService kinaUnaTextService = new KinaUnaTextService(context);

            KinaUnaText resultKinaUnaText = await kinaUnaTextService.GetTextById(1);

            Assert.NotNull(resultKinaUnaText);
            Assert.IsType<KinaUnaText>(resultKinaUnaText);
            Assert.Equal(text1.Page, resultKinaUnaText.Page);
            Assert.Equal(text1.Title, resultKinaUnaText.Title);
            Assert.Equal(text1.Text, resultKinaUnaText.Text);
            Assert.Equal(text1.LanguageId, resultKinaUnaText.LanguageId);
        }

        [Fact]
        public async Task GetTextById_Should_Return_Null_When_Id_Is_Invalid()
        {
            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>().UseInMemoryDatabase("GetTextById_Should_Return_Null_When_Id_Is_Invalid").Options;
            await using ProgenyDbContext context = new ProgenyDbContext(dbOptions);

            KinaUnaLanguage language1 = new KinaUnaLanguage { Name = "Language1", Code = "Code1", Icon = "Icon1", IconLink = "IconLink1" };
            KinaUnaLanguage language2 = new KinaUnaLanguage { Name = "Language2", Code = "Code2", Icon = "Icon2", IconLink = "IconLink2" };
            KinaUnaLanguage language3 = new KinaUnaLanguage { Name = "Language3", Code = "Code3", Icon = "Icon3", IconLink = "IconLink3" };

            context.Add(language1);
            context.Add(language2);
            context.Add(language3);

            KinaUnaText text1 = new KinaUnaText { LanguageId = 1, Title = "Title1", Page = "Page1", Text = "Text1.1", Created = DateTime.UtcNow, TextId = 1, Updated = DateTime.UtcNow };
            KinaUnaText text2 = new KinaUnaText { LanguageId = 2, Title = "Title1", Page = "Page1", Text = "Text1.2", Created = DateTime.UtcNow, TextId = 1, Updated = DateTime.UtcNow };
            KinaUnaText text3 = new KinaUnaText { LanguageId = 3, Title = "Title1", Page = "Page1", Text = "Text1.3", Created = DateTime.UtcNow, TextId = 1, Updated = DateTime.UtcNow };

            context.Add(text1);
            context.Add(text2);
            context.Add(text3);

            await context.SaveChangesAsync();

            KinaUnaTextService kinaUnaTextService = new KinaUnaTextService(context);

            KinaUnaText resultKinaUnaText = await kinaUnaTextService.GetTextById(4);

            Assert.Null(resultKinaUnaText);
        }

        [Fact]
        public async Task GetTextByTextId_Should_Return_KinaUnaText_Object_When_TextId_Is_Valid()
        {
            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>().UseInMemoryDatabase("GetTextByTextId_Should_Return_KinaUnaText_Object_When_TextId_Is_Valid").Options;
            await using ProgenyDbContext context = new ProgenyDbContext(dbOptions);

            KinaUnaLanguage language1 = new KinaUnaLanguage { Name = "Language1", Code = "Code1", Icon = "Icon1", IconLink = "IconLink1" };
            KinaUnaLanguage language2 = new KinaUnaLanguage { Name = "Language2", Code = "Code2", Icon = "Icon2", IconLink = "IconLink2" };
            KinaUnaLanguage language3 = new KinaUnaLanguage { Name = "Language3", Code = "Code3", Icon = "Icon3", IconLink = "IconLink3" };

            context.Add(language1);
            context.Add(language2);
            context.Add(language3);

            KinaUnaText text1 = new KinaUnaText { LanguageId = 1, Title = "Title1", Page = "Page1", Text = "Text1.1", Created = DateTime.UtcNow, TextId = 1, Updated = DateTime.UtcNow, };
            KinaUnaText text2 = new KinaUnaText { LanguageId = 2, Title = "Title1", Page = "Page1", Text = "Text1.2", Created = DateTime.UtcNow, TextId = 1, Updated = DateTime.UtcNow };
            KinaUnaText text3 = new KinaUnaText { LanguageId = 3, Title = "Title1", Page = "Page1", Text = "Text1.3", Created = DateTime.UtcNow, TextId = 1, Updated = DateTime.UtcNow };

            context.Add(text1);
            context.Add(text2);
            context.Add(text3);

            await context.SaveChangesAsync();

            KinaUnaTextService kinaUnaTextService = new KinaUnaTextService(context);

            KinaUnaText resultKinaUnaText = await kinaUnaTextService.GetTextByTextId(1, 1);

            Assert.NotNull(resultKinaUnaText);
            Assert.IsType<KinaUnaText>(resultKinaUnaText);
            Assert.Equal(text1.Page, resultKinaUnaText.Page);
            Assert.Equal(text1.Title, resultKinaUnaText.Title);
            Assert.Equal(text1.Text, resultKinaUnaText.Text);
            Assert.Equal(text1.LanguageId, resultKinaUnaText.LanguageId);
        }

        [Fact]
        public async Task GetTextByTextId_Should_Return_Null_When_TextId_Is_Invalid()
        {
            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>().UseInMemoryDatabase("GetTextByTextId_Should_Return_Null_When_TextId_Is_Invalid").Options;
            await using ProgenyDbContext context = new ProgenyDbContext(dbOptions);

            KinaUnaLanguage language1 = new KinaUnaLanguage { Name = "Language1", Code = "Code1", Icon = "Icon1", IconLink = "IconLink1" };
            KinaUnaLanguage language2 = new KinaUnaLanguage { Name = "Language2", Code = "Code2", Icon = "Icon2", IconLink = "IconLink2" };
            KinaUnaLanguage language3 = new KinaUnaLanguage { Name = "Language3", Code = "Code3", Icon = "Icon3", IconLink = "IconLink3" };

            context.Add(language1);
            context.Add(language2);
            context.Add(language3);

            KinaUnaText text1 = new KinaUnaText { LanguageId = 1, Title = "Title1", Page = "Page1", Text = "Text1.1", Created = DateTime.UtcNow, TextId = 1, Updated = DateTime.UtcNow };
            KinaUnaText text2 = new KinaUnaText { LanguageId = 2, Title = "Title1", Page = "Page1", Text = "Text1.2", Created = DateTime.UtcNow, TextId = 1, Updated = DateTime.UtcNow };
            KinaUnaText text3 = new KinaUnaText { LanguageId = 3, Title = "Title1", Page = "Page1", Text = "Text1.3", Created = DateTime.UtcNow, TextId = 1, Updated = DateTime.UtcNow };

            context.Add(text1);
            context.Add(text2);
            context.Add(text3);

            await context.SaveChangesAsync();

            KinaUnaTextService kinaUnaTextService = new KinaUnaTextService(context);

            KinaUnaText resultKinaUnaText = await kinaUnaTextService.GetTextByTextId(2, 1);

            Assert.Null(resultKinaUnaText);
        }

        [Fact]
        public async Task GetTextByTextId_Should_Return_Null_When_LanguageId_Is_Invalid()
        {
            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>().UseInMemoryDatabase("GetTextByTextId_Should_Return_Null_When_LanguageId_Is_Invalid").Options;
            await using ProgenyDbContext context = new ProgenyDbContext(dbOptions);

            KinaUnaLanguage language1 = new KinaUnaLanguage { Name = "Language1", Code = "Code1", Icon = "Icon1", IconLink = "IconLink1" };
            KinaUnaLanguage language2 = new KinaUnaLanguage { Name = "Language2", Code = "Code2", Icon = "Icon2", IconLink = "IconLink2" };
            KinaUnaLanguage language3 = new KinaUnaLanguage { Name = "Language3", Code = "Code3", Icon = "Icon3", IconLink = "IconLink3" };

            context.Add(language1);
            context.Add(language2);
            context.Add(language3);

            KinaUnaText text1 = new KinaUnaText { LanguageId = 1, Title = "Title1", Page = "Page1", Text = "Text1.1", Created = DateTime.UtcNow, TextId = 1, Updated = DateTime.UtcNow };
            KinaUnaText text2 = new KinaUnaText { LanguageId = 2, Title = "Title1", Page = "Page1", Text = "Text1.2", Created = DateTime.UtcNow, TextId = 1, Updated = DateTime.UtcNow };
            KinaUnaText text3 = new KinaUnaText { LanguageId = 3, Title = "Title1", Page = "Page1", Text = "Text1.3", Created = DateTime.UtcNow, TextId = 1, Updated = DateTime.UtcNow };

            context.Add(text1);
            context.Add(text2);
            context.Add(text3);

            await context.SaveChangesAsync();

            KinaUnaTextService kinaUnaTextService = new KinaUnaTextService(context);

            KinaUnaText resultKinaUnaText = await kinaUnaTextService.GetTextByTextId(1, 4);

            Assert.Null(resultKinaUnaText);
        }

        [Fact]
        public async Task GetPageTextsList_Should_Return_List_Of_KinaUnaText_When_Page_Is_Valid()
        {
            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>().UseInMemoryDatabase("GetPageTextsList_Should_Return_List_Of_KinaUnaText_When_TextId_Is_Valid").Options;
            await using ProgenyDbContext context = new ProgenyDbContext(dbOptions);

            KinaUnaLanguage language1 = new KinaUnaLanguage { Name = "Language1", Code = "Code1", Icon = "Icon1", IconLink = "IconLink1" };
            KinaUnaLanguage language2 = new KinaUnaLanguage { Name = "Language2", Code = "Code2", Icon = "Icon2", IconLink = "IconLink2" };
            KinaUnaLanguage language3 = new KinaUnaLanguage { Name = "Language3", Code = "Code3", Icon = "Icon3", IconLink = "IconLink3" };

            context.Add(language1);
            context.Add(language2);
            context.Add(language3);

            KinaUnaTextNumber textNumber1 = new KinaUnaTextNumber { DefaultLanguage = 1 };
            KinaUnaTextNumber textNumber2 = new KinaUnaTextNumber { DefaultLanguage = 1 };
            KinaUnaTextNumber textNumber3 = new KinaUnaTextNumber { DefaultLanguage = 1 };
            context.Add(textNumber1);
            context.Add(textNumber2);
            context.Add(textNumber3);

            KinaUnaText text1 = new KinaUnaText { LanguageId = 1, Title = "Title1", Page = "Page1", Text = "Text1.1", Created = DateTime.UtcNow, TextId = 1, Updated = DateTime.UtcNow, };
            KinaUnaText text2 = new KinaUnaText { LanguageId = 2, Title = "Title1", Page = "Page1", Text = "Text1.2", Created = DateTime.UtcNow, TextId = 1, Updated = DateTime.UtcNow };
            KinaUnaText text3 = new KinaUnaText { LanguageId = 3, Title = "Title1", Page = "Page1", Text = "Text1.3", Created = DateTime.UtcNow, TextId = 1, Updated = DateTime.UtcNow };

            KinaUnaText text4 = new KinaUnaText { LanguageId = 1, Title = "Title2", Page = "Page1", Text = "Text2.1", Created = DateTime.UtcNow, TextId = 2, Updated = DateTime.UtcNow, };
            KinaUnaText text5 = new KinaUnaText { LanguageId = 2, Title = "Title2", Page = "Page1", Text = "Text2.2", Created = DateTime.UtcNow, TextId = 2, Updated = DateTime.UtcNow };
            KinaUnaText text6 = new KinaUnaText { LanguageId = 3, Title = "Title2", Page = "Page1", Text = "Text2.3", Created = DateTime.UtcNow, TextId = 2, Updated = DateTime.UtcNow };

            KinaUnaText text7 = new KinaUnaText { LanguageId = 1, Title = "Title3", Page = "Page1", Text = "Text3.1", Created = DateTime.UtcNow, TextId = 3, Updated = DateTime.UtcNow, };
            KinaUnaText text8 = new KinaUnaText { LanguageId = 2, Title = "Title3", Page = "Page1", Text = "Text3.2", Created = DateTime.UtcNow, TextId = 3, Updated = DateTime.UtcNow };
            KinaUnaText text9 = new KinaUnaText { LanguageId = 3, Title = "Title3", Page = "Page1", Text = "Text3.3", Created = DateTime.UtcNow, TextId = 3, Updated = DateTime.UtcNow };

            context.Add(text1);
            context.Add(text2);
            context.Add(text3);

            context.Add(text4);
            context.Add(text5);
            context.Add(text6);

            context.Add(text7);
            context.Add(text8);
            context.Add(text9);

            await context.SaveChangesAsync();

            KinaUnaTextService kinaUnaTextService = new KinaUnaTextService(context);

            List<KinaUnaText> resultKinaUnaTextsList = await kinaUnaTextService.GetPageTextsList("Page1", 1);

            Assert.NotNull(resultKinaUnaTextsList);
            Assert.IsType<List<KinaUnaText>>(resultKinaUnaTextsList);
            Assert.NotEmpty(resultKinaUnaTextsList);
            Assert.Equal(3, resultKinaUnaTextsList.Count);
        }

        [Fact]
        public async Task GetPageTextsList_Should_Return_Empty_List_Of_KinaUnaText_When_Page_Is_Invalid()
        {
            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>().UseInMemoryDatabase("GetPageTextsList_Should_Return_List_Of_KinaUnaText_When_TextId_Is_Valid").Options;
            await using ProgenyDbContext context = new ProgenyDbContext(dbOptions);

            KinaUnaLanguage language1 = new KinaUnaLanguage { Name = "Language1", Code = "Code1", Icon = "Icon1", IconLink = "IconLink1" };
            KinaUnaLanguage language2 = new KinaUnaLanguage { Name = "Language2", Code = "Code2", Icon = "Icon2", IconLink = "IconLink2" };
            KinaUnaLanguage language3 = new KinaUnaLanguage { Name = "Language3", Code = "Code3", Icon = "Icon3", IconLink = "IconLink3" };

            context.Add(language1);
            context.Add(language2);
            context.Add(language3);

            KinaUnaTextNumber textNumber1 = new KinaUnaTextNumber { DefaultLanguage = 1 };
            KinaUnaTextNumber textNumber2 = new KinaUnaTextNumber { DefaultLanguage = 1 };
            KinaUnaTextNumber textNumber3 = new KinaUnaTextNumber { DefaultLanguage = 1 };
            context.Add(textNumber1);
            context.Add(textNumber2);
            context.Add(textNumber3);

            KinaUnaText text1 = new KinaUnaText { LanguageId = 1, Title = "Title1", Page = "Page1", Text = "Text1.1", Created = DateTime.UtcNow, TextId = 1, Updated = DateTime.UtcNow, };
            KinaUnaText text2 = new KinaUnaText { LanguageId = 2, Title = "Title1", Page = "Page1", Text = "Text1.2", Created = DateTime.UtcNow, TextId = 1, Updated = DateTime.UtcNow };
            KinaUnaText text3 = new KinaUnaText { LanguageId = 3, Title = "Title1", Page = "Page1", Text = "Text1.3", Created = DateTime.UtcNow, TextId = 1, Updated = DateTime.UtcNow };

            KinaUnaText text4 = new KinaUnaText { LanguageId = 1, Title = "Title2", Page = "Page1", Text = "Text2.1", Created = DateTime.UtcNow, TextId = 2, Updated = DateTime.UtcNow, };
            KinaUnaText text5 = new KinaUnaText { LanguageId = 2, Title = "Title2", Page = "Page1", Text = "Text2.2", Created = DateTime.UtcNow, TextId = 2, Updated = DateTime.UtcNow };
            KinaUnaText text6 = new KinaUnaText { LanguageId = 3, Title = "Title2", Page = "Page1", Text = "Text2.3", Created = DateTime.UtcNow, TextId = 2, Updated = DateTime.UtcNow };

            KinaUnaText text7 = new KinaUnaText { LanguageId = 1, Title = "Title3", Page = "Page1", Text = "Text3.1", Created = DateTime.UtcNow, TextId = 3, Updated = DateTime.UtcNow, };
            KinaUnaText text8 = new KinaUnaText { LanguageId = 2, Title = "Title3", Page = "Page1", Text = "Text3.2", Created = DateTime.UtcNow, TextId = 3, Updated = DateTime.UtcNow };
            KinaUnaText text9 = new KinaUnaText { LanguageId = 3, Title = "Title3", Page = "Page1", Text = "Text3.3", Created = DateTime.UtcNow, TextId = 3, Updated = DateTime.UtcNow };

            context.Add(text1);
            context.Add(text2);
            context.Add(text3);

            context.Add(text4);
            context.Add(text5);
            context.Add(text6);

            context.Add(text7);
            context.Add(text8);
            context.Add(text9);

            await context.SaveChangesAsync();

            KinaUnaTextService kinaUnaTextService = new KinaUnaTextService(context);

            List<KinaUnaText> resultKinaUnaTextsList = await kinaUnaTextService.GetPageTextsList("Page2", 1);

            Assert.NotNull(resultKinaUnaTextsList);
            Assert.IsType<List<KinaUnaText>>(resultKinaUnaTextsList);
            Assert.Empty(resultKinaUnaTextsList);
        }

        [Fact]
        public async Task GetPageTextsList_Should_Return_Empty_List_Of_KinaUnaText_When_LanguageId_Is_Invalid()
        {
            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>().UseInMemoryDatabase("GetPageTextsList_Should_Return_Empty_List_Of_KinaUnaText_When_LanguageId_Is_Invalid").Options;
            await using ProgenyDbContext context = new ProgenyDbContext(dbOptions);

            KinaUnaLanguage language1 = new KinaUnaLanguage { Name = "Language1", Code = "Code1", Icon = "Icon1", IconLink = "IconLink1" };
            KinaUnaLanguage language2 = new KinaUnaLanguage { Name = "Language2", Code = "Code2", Icon = "Icon2", IconLink = "IconLink2" };
            KinaUnaLanguage language3 = new KinaUnaLanguage { Name = "Language3", Code = "Code3", Icon = "Icon3", IconLink = "IconLink3" };

            context.Add(language1);
            context.Add(language2);
            context.Add(language3);

            KinaUnaTextNumber textNumber1 = new KinaUnaTextNumber { DefaultLanguage = 1 };
            KinaUnaTextNumber textNumber2 = new KinaUnaTextNumber { DefaultLanguage = 1 };
            KinaUnaTextNumber textNumber3 = new KinaUnaTextNumber { DefaultLanguage = 1 };
            context.Add(textNumber1);
            context.Add(textNumber2);
            context.Add(textNumber3);

            KinaUnaText text1 = new KinaUnaText { LanguageId = 1, Title = "Title1", Page = "Page1", Text = "Text1.1", Created = DateTime.UtcNow, TextId = 1, Updated = DateTime.UtcNow, };
            KinaUnaText text2 = new KinaUnaText { LanguageId = 2, Title = "Title1", Page = "Page1", Text = "Text1.2", Created = DateTime.UtcNow, TextId = 1, Updated = DateTime.UtcNow };
            KinaUnaText text3 = new KinaUnaText { LanguageId = 3, Title = "Title1", Page = "Page1", Text = "Text1.3", Created = DateTime.UtcNow, TextId = 1, Updated = DateTime.UtcNow };

            KinaUnaText text4 = new KinaUnaText { LanguageId = 1, Title = "Title2", Page = "Page1", Text = "Text2.1", Created = DateTime.UtcNow, TextId = 2, Updated = DateTime.UtcNow, };
            KinaUnaText text5 = new KinaUnaText { LanguageId = 2, Title = "Title2", Page = "Page1", Text = "Text2.2", Created = DateTime.UtcNow, TextId = 2, Updated = DateTime.UtcNow };
            KinaUnaText text6 = new KinaUnaText { LanguageId = 3, Title = "Title2", Page = "Page1", Text = "Text2.3", Created = DateTime.UtcNow, TextId = 2, Updated = DateTime.UtcNow };

            KinaUnaText text7 = new KinaUnaText { LanguageId = 1, Title = "Title3", Page = "Page1", Text = "Text3.1", Created = DateTime.UtcNow, TextId = 3, Updated = DateTime.UtcNow, };
            KinaUnaText text8 = new KinaUnaText { LanguageId = 2, Title = "Title3", Page = "Page1", Text = "Text3.2", Created = DateTime.UtcNow, TextId = 3, Updated = DateTime.UtcNow };
            KinaUnaText text9 = new KinaUnaText { LanguageId = 3, Title = "Title3", Page = "Page1", Text = "Text3.3", Created = DateTime.UtcNow, TextId = 3, Updated = DateTime.UtcNow };

            context.Add(text1);
            context.Add(text2);
            context.Add(text3);

            context.Add(text4);
            context.Add(text5);
            context.Add(text6);

            context.Add(text7);
            context.Add(text8);
            context.Add(text9);

            await context.SaveChangesAsync();

            KinaUnaTextService kinaUnaTextService = new KinaUnaTextService(context);

            List<KinaUnaText> resultKinaUnaTextsList = await kinaUnaTextService.GetPageTextsList("Page1", 4);

            Assert.NotNull(resultKinaUnaTextsList);
            Assert.IsType<List<KinaUnaText>>(resultKinaUnaTextsList);
            Assert.Empty(resultKinaUnaTextsList);
        }

        [Fact]
        public async Task GetAllPageTextsList_Should_Return_List_Of_All_KinaUnaTexts_For_A_Language_When_LanguageId_Is_Valid()
        {
            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>().UseInMemoryDatabase("GetAllPageTextsList_Should_Return_List_Of_All_KinaUnaTexts_For_A_Language_When_LanguageId_Is_Valid").Options;
            await using ProgenyDbContext context = new ProgenyDbContext(dbOptions);

            KinaUnaLanguage language1 = new KinaUnaLanguage { Name = "Language1", Code = "Code1", Icon = "Icon1", IconLink = "IconLink1" };
            KinaUnaLanguage language2 = new KinaUnaLanguage { Name = "Language2", Code = "Code2", Icon = "Icon2", IconLink = "IconLink2" };
            KinaUnaLanguage language3 = new KinaUnaLanguage { Name = "Language3", Code = "Code3", Icon = "Icon3", IconLink = "IconLink3" };

            context.Add(language1);
            context.Add(language2);
            context.Add(language3);

            KinaUnaTextNumber textNumber1 = new KinaUnaTextNumber { DefaultLanguage = 1 };
            KinaUnaTextNumber textNumber2 = new KinaUnaTextNumber { DefaultLanguage = 1 };
            KinaUnaTextNumber textNumber3 = new KinaUnaTextNumber { DefaultLanguage = 1 };
            context.Add(textNumber1);
            context.Add(textNumber2);
            context.Add(textNumber3);

            KinaUnaText text1 = new KinaUnaText { LanguageId = 1, Title = "Title1", Page = "Page1", Text = "Text1.1", Created = DateTime.UtcNow, TextId = 1, Updated = DateTime.UtcNow, };
            KinaUnaText text2 = new KinaUnaText { LanguageId = 2, Title = "Title1", Page = "Page1", Text = "Text1.2", Created = DateTime.UtcNow, TextId = 1, Updated = DateTime.UtcNow };
            KinaUnaText text3 = new KinaUnaText { LanguageId = 3, Title = "Title1", Page = "Page1", Text = "Text1.3", Created = DateTime.UtcNow, TextId = 1, Updated = DateTime.UtcNow };

            KinaUnaText text4 = new KinaUnaText { LanguageId = 1, Title = "Title2", Page = "Page1", Text = "Text2.1", Created = DateTime.UtcNow, TextId = 2, Updated = DateTime.UtcNow, };
            KinaUnaText text5 = new KinaUnaText { LanguageId = 2, Title = "Title2", Page = "Page1", Text = "Text2.2", Created = DateTime.UtcNow, TextId = 2, Updated = DateTime.UtcNow };
            KinaUnaText text6 = new KinaUnaText { LanguageId = 3, Title = "Title2", Page = "Page1", Text = "Text2.3", Created = DateTime.UtcNow, TextId = 2, Updated = DateTime.UtcNow };

            KinaUnaText text7 = new KinaUnaText { LanguageId = 1, Title = "Title3", Page = "Page1", Text = "Text3.1", Created = DateTime.UtcNow, TextId = 3, Updated = DateTime.UtcNow, };
            KinaUnaText text8 = new KinaUnaText { LanguageId = 2, Title = "Title3", Page = "Page1", Text = "Text3.2", Created = DateTime.UtcNow, TextId = 3, Updated = DateTime.UtcNow };
            KinaUnaText text9 = new KinaUnaText { LanguageId = 3, Title = "Title3", Page = "Page1", Text = "Text3.3", Created = DateTime.UtcNow, TextId = 3, Updated = DateTime.UtcNow };

            context.Add(text1);
            context.Add(text2);
            context.Add(text3);

            context.Add(text4);
            context.Add(text5);
            context.Add(text6);

            context.Add(text7);
            context.Add(text8);
            context.Add(text9);

            await context.SaveChangesAsync();

            KinaUnaTextService kinaUnaTextService = new KinaUnaTextService(context);

            List<KinaUnaText> resultKinaUnaTextsList = await kinaUnaTextService.GetAllPageTextsList(1);

            Assert.NotNull(resultKinaUnaTextsList);
            Assert.IsType<List<KinaUnaText>>(resultKinaUnaTextsList);
            Assert.NotEmpty(resultKinaUnaTextsList);
            Assert.Equal(3, resultKinaUnaTextsList.Count);
        }

        [Fact]
        public async Task GetAllPageTextsList_Should_Return_Empty_List_Of_KinaUnaTexts_When_LanguageId_Is_Invalid()
        {
            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>().UseInMemoryDatabase("GetAllPageTextsList_Should_Return_Empty_List_Of_KinaUnaTexts_When_LanguageId_Is_Invalid").Options;
            await using ProgenyDbContext context = new ProgenyDbContext(dbOptions);

            KinaUnaLanguage language1 = new KinaUnaLanguage { Name = "Language1", Code = "Code1", Icon = "Icon1", IconLink = "IconLink1" };
            KinaUnaLanguage language2 = new KinaUnaLanguage { Name = "Language2", Code = "Code2", Icon = "Icon2", IconLink = "IconLink2" };
            KinaUnaLanguage language3 = new KinaUnaLanguage { Name = "Language3", Code = "Code3", Icon = "Icon3", IconLink = "IconLink3" };

            context.Add(language1);
            context.Add(language2);
            context.Add(language3);

            KinaUnaTextNumber textNumber1 = new KinaUnaTextNumber { DefaultLanguage = 1 };
            KinaUnaTextNumber textNumber2 = new KinaUnaTextNumber { DefaultLanguage = 1 };
            KinaUnaTextNumber textNumber3 = new KinaUnaTextNumber { DefaultLanguage = 1 };
            context.Add(textNumber1);
            context.Add(textNumber2);
            context.Add(textNumber3);

            KinaUnaText text1 = new KinaUnaText { LanguageId = 1, Title = "Title1", Page = "Page1", Text = "Text1.1", Created = DateTime.UtcNow, TextId = 1, Updated = DateTime.UtcNow, };
            KinaUnaText text2 = new KinaUnaText { LanguageId = 2, Title = "Title1", Page = "Page1", Text = "Text1.2", Created = DateTime.UtcNow, TextId = 1, Updated = DateTime.UtcNow };
            KinaUnaText text3 = new KinaUnaText { LanguageId = 3, Title = "Title1", Page = "Page1", Text = "Text1.3", Created = DateTime.UtcNow, TextId = 1, Updated = DateTime.UtcNow };

            KinaUnaText text4 = new KinaUnaText { LanguageId = 1, Title = "Title2", Page = "Page1", Text = "Text2.1", Created = DateTime.UtcNow, TextId = 2, Updated = DateTime.UtcNow, };
            KinaUnaText text5 = new KinaUnaText { LanguageId = 2, Title = "Title2", Page = "Page1", Text = "Text2.2", Created = DateTime.UtcNow, TextId = 2, Updated = DateTime.UtcNow };
            KinaUnaText text6 = new KinaUnaText { LanguageId = 3, Title = "Title2", Page = "Page1", Text = "Text2.3", Created = DateTime.UtcNow, TextId = 2, Updated = DateTime.UtcNow };

            KinaUnaText text7 = new KinaUnaText { LanguageId = 1, Title = "Title3", Page = "Page1", Text = "Text3.1", Created = DateTime.UtcNow, TextId = 3, Updated = DateTime.UtcNow, };
            KinaUnaText text8 = new KinaUnaText { LanguageId = 2, Title = "Title3", Page = "Page1", Text = "Text3.2", Created = DateTime.UtcNow, TextId = 3, Updated = DateTime.UtcNow };
            KinaUnaText text9 = new KinaUnaText { LanguageId = 3, Title = "Title3", Page = "Page1", Text = "Text3.3", Created = DateTime.UtcNow, TextId = 3, Updated = DateTime.UtcNow };

            context.Add(text1);
            context.Add(text2);
            context.Add(text3);

            context.Add(text4);
            context.Add(text5);
            context.Add(text6);

            context.Add(text7);
            context.Add(text8);
            context.Add(text9);

            await context.SaveChangesAsync();

            KinaUnaTextService kinaUnaTextService = new KinaUnaTextService(context);

            List<KinaUnaText> resultKinaUnaTextsList = await kinaUnaTextService.GetAllPageTextsList(4);

            Assert.NotNull(resultKinaUnaTextsList);
            Assert.IsType<List<KinaUnaText>>(resultKinaUnaTextsList);
            Assert.Empty(resultKinaUnaTextsList);
        }

        [Fact]
        public async Task CheckLanguages_Should_Add_Any_Missing_KinaUnaText_Language_Version()
        {
            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>().UseInMemoryDatabase("CheckLanguages_Should_Add_Any_Missing_KinaUnaText_Language_Version").Options;
            await using ProgenyDbContext context = new ProgenyDbContext(dbOptions);

            KinaUnaLanguage language1 = new KinaUnaLanguage { Name = "Language1", Code = "Code1", Icon = "Icon1", IconLink = "IconLink1" };
            KinaUnaLanguage language2 = new KinaUnaLanguage { Name = "Language2", Code = "Code2", Icon = "Icon2", IconLink = "IconLink2" };
            KinaUnaLanguage language3 = new KinaUnaLanguage { Name = "Language3", Code = "Code3", Icon = "Icon3", IconLink = "IconLink3" };

            context.Add(language1);
            context.Add(language2);
            context.Add(language3);

            KinaUnaTextNumber textNumber1 = new KinaUnaTextNumber { DefaultLanguage = 1 };
            KinaUnaTextNumber textNumber2 = new KinaUnaTextNumber { DefaultLanguage = 1 };
            KinaUnaTextNumber textNumber3 = new KinaUnaTextNumber { DefaultLanguage = 1 };
            context.Add(textNumber1);
            context.Add(textNumber2);
            context.Add(textNumber3);

            KinaUnaText text1 = new KinaUnaText { LanguageId = 1, Title = "Title1", Page = "Page1", Text = "Text1.1", Created = DateTime.UtcNow, TextId = 1, Updated = DateTime.UtcNow, };
            KinaUnaText text2 = new KinaUnaText { LanguageId = 2, Title = "Title1", Page = "Page1", Text = "Text1.2", Created = DateTime.UtcNow, TextId = 1, Updated = DateTime.UtcNow };
            KinaUnaText text3 = new KinaUnaText { LanguageId = 3, Title = "Title1", Page = "Page1", Text = "Text1.3", Created = DateTime.UtcNow, TextId = 1, Updated = DateTime.UtcNow };

            KinaUnaText text4 = new KinaUnaText { LanguageId = 1, Title = "Title2", Page = "Page1", Text = "Text2.1", Created = DateTime.UtcNow, TextId = 2, Updated = DateTime.UtcNow, };
            KinaUnaText text5 = new KinaUnaText { LanguageId = 2, Title = "Title2", Page = "Page1", Text = "Text2.2", Created = DateTime.UtcNow, TextId = 2, Updated = DateTime.UtcNow };
            //KinaUnaText text6 = new KinaUnaText { LanguageId = 3, Title = "Title2", Page = "Page1", Text = "Text2.3", Created = DateTime.UtcNow, TextId = 2, Updated = DateTime.UtcNow };

            KinaUnaText text7 = new KinaUnaText { LanguageId = 1, Title = "Title3", Page = "Page1", Text = "Text3.1", Created = DateTime.UtcNow, TextId = 3, Updated = DateTime.UtcNow, };
            KinaUnaText text8 = new KinaUnaText { LanguageId = 2, Title = "Title3", Page = "Page1", Text = "Text3.2", Created = DateTime.UtcNow, TextId = 3, Updated = DateTime.UtcNow };
            //KinaUnaText text9 = new KinaUnaText { LanguageId = 3, Title = "Title3", Page = "Page1", Text = "Text3.3", Created = DateTime.UtcNow, TextId = 3, Updated = DateTime.UtcNow };

            context.Add(text1);
            context.Add(text2);
            context.Add(text3);

            context.Add(text4);
            context.Add(text5);
            //context.Add(text6);

            context.Add(text7);
            context.Add(text8);
            //context.Add(text9);

            await context.SaveChangesAsync();

            KinaUnaTextService kinaUnaTextService = new KinaUnaTextService(context);
            int kinaUnaTextsCountBefore = context.KinaUnaTexts.Count();
            await kinaUnaTextService.CheckLanguages();
            int kinaUnaTextsCountAfter = context.KinaUnaTexts.Count();
            List<KinaUnaText> kinUnaTextsWithTextId2 = await context.KinaUnaTexts.Where(kt => kt.TextId == 2).ToListAsync();
            List<KinaUnaText> kinUnaTextsWithTextId3 = await context.KinaUnaTexts.Where(kt => kt.TextId == 3).ToListAsync();

            Assert.NotEqual(kinaUnaTextsCountBefore, kinaUnaTextsCountAfter);
            Assert.Equal(7, kinaUnaTextsCountBefore);
            Assert.Equal(9, kinaUnaTextsCountAfter);
            Assert.NotEmpty(kinUnaTextsWithTextId2);
            Assert.Equal(3, kinUnaTextsWithTextId2.Count);
            Assert.NotEmpty(kinUnaTextsWithTextId3);
            Assert.Equal(3, kinUnaTextsWithTextId3.Count);
        }

        [Fact]
        public async Task CheckLanguages_Should_Make_No_Changes_When_There_Are_No_Missing_KinaUnaText_Language_Version()
        {
            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>().UseInMemoryDatabase("CheckLanguages_Should_Make_No_Changes_When_There_Are_No_Missing_KinaUnaText_Language_Version").Options;
            await using ProgenyDbContext context = new ProgenyDbContext(dbOptions);

            KinaUnaLanguage language1 = new KinaUnaLanguage { Name = "Language1", Code = "Code1", Icon = "Icon1", IconLink = "IconLink1" };
            KinaUnaLanguage language2 = new KinaUnaLanguage { Name = "Language2", Code = "Code2", Icon = "Icon2", IconLink = "IconLink2" };
            KinaUnaLanguage language3 = new KinaUnaLanguage { Name = "Language3", Code = "Code3", Icon = "Icon3", IconLink = "IconLink3" };

            context.Add(language1);
            context.Add(language2);
            context.Add(language3);

            KinaUnaTextNumber textNumber1 = new KinaUnaTextNumber { DefaultLanguage = 1 };
            KinaUnaTextNumber textNumber2 = new KinaUnaTextNumber { DefaultLanguage = 1 };
            KinaUnaTextNumber textNumber3 = new KinaUnaTextNumber { DefaultLanguage = 1 };
            context.Add(textNumber1);
            context.Add(textNumber2);
            context.Add(textNumber3);

            KinaUnaText text1 = new KinaUnaText { LanguageId = 1, Title = "Title1", Page = "Page1", Text = "Text1.1", Created = DateTime.UtcNow, TextId = 1, Updated = DateTime.UtcNow, };
            KinaUnaText text2 = new KinaUnaText { LanguageId = 2, Title = "Title1", Page = "Page1", Text = "Text1.2", Created = DateTime.UtcNow, TextId = 1, Updated = DateTime.UtcNow };
            KinaUnaText text3 = new KinaUnaText { LanguageId = 3, Title = "Title1", Page = "Page1", Text = "Text1.3", Created = DateTime.UtcNow, TextId = 1, Updated = DateTime.UtcNow };

            KinaUnaText text4 = new KinaUnaText { LanguageId = 1, Title = "Title2", Page = "Page1", Text = "Text2.1", Created = DateTime.UtcNow, TextId = 2, Updated = DateTime.UtcNow, };
            KinaUnaText text5 = new KinaUnaText { LanguageId = 2, Title = "Title2", Page = "Page1", Text = "Text2.2", Created = DateTime.UtcNow, TextId = 2, Updated = DateTime.UtcNow };
            KinaUnaText text6 = new KinaUnaText { LanguageId = 3, Title = "Title2", Page = "Page1", Text = "Text2.3", Created = DateTime.UtcNow, TextId = 2, Updated = DateTime.UtcNow };

            KinaUnaText text7 = new KinaUnaText { LanguageId = 1, Title = "Title3", Page = "Page1", Text = "Text3.1", Created = DateTime.UtcNow, TextId = 3, Updated = DateTime.UtcNow, };
            KinaUnaText text8 = new KinaUnaText { LanguageId = 2, Title = "Title3", Page = "Page1", Text = "Text3.2", Created = DateTime.UtcNow, TextId = 3, Updated = DateTime.UtcNow };
            KinaUnaText text9 = new KinaUnaText { LanguageId = 3, Title = "Title3", Page = "Page1", Text = "Text3.3", Created = DateTime.UtcNow, TextId = 3, Updated = DateTime.UtcNow };

            context.Add(text1);
            context.Add(text2);
            context.Add(text3);

            context.Add(text4);
            context.Add(text5);
            context.Add(text6);

            context.Add(text7);
            context.Add(text8);
            context.Add(text9);

            await context.SaveChangesAsync();

            KinaUnaTextService kinaUnaTextService = new KinaUnaTextService(context);
            int kinaUnaTextsCountBefore = context.KinaUnaTexts.Count();
            await kinaUnaTextService.CheckLanguages();
            int kinaUnaTextsCountAfter = context.KinaUnaTexts.Count();
            
            Assert.Equal(kinaUnaTextsCountBefore, kinaUnaTextsCountAfter);
            Assert.Equal(9, kinaUnaTextsCountBefore);
            Assert.Equal(9, kinaUnaTextsCountAfter);
        }

        [Fact]
        public async Task AddText_Should_Save_KinaUnaText()
        {
            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>().UseInMemoryDatabase("AddText_Should_Save_KinaUnaText").Options;
            await using ProgenyDbContext context = new ProgenyDbContext(dbOptions);

            KinaUnaLanguage language1 = new KinaUnaLanguage { Name = "Language1", Code = "Code1", Icon = "Icon1", IconLink = "IconLink1" };
            KinaUnaLanguage language2 = new KinaUnaLanguage { Name = "Language2", Code = "Code2", Icon = "Icon2", IconLink = "IconLink2" };
            KinaUnaLanguage language3 = new KinaUnaLanguage { Name = "Language3", Code = "Code3", Icon = "Icon3", IconLink = "IconLink3" };

            context.Add(language1);
            context.Add(language2);
            context.Add(language3);

            KinaUnaTextNumber kinaUnaTextNumber1 = new KinaUnaTextNumber { DefaultLanguage = 1 };
            
            context.Add(kinaUnaTextNumber1);

            KinaUnaText text1 = new KinaUnaText { LanguageId = 1, Title = "Title1", Page = "Page1", Text = "Text1.1", Created = DateTime.UtcNow, TextId = 1, Updated = DateTime.UtcNow };
            KinaUnaText text2 = new KinaUnaText { LanguageId = 2, Title = "Title1", Page = "Page1", Text = "Text1.2", Created = DateTime.UtcNow, TextId = 1, Updated = DateTime.UtcNow };
            KinaUnaText text3 = new KinaUnaText { LanguageId = 3, Title = "Title1", Page = "Page1", Text = "Text1.3", Created = DateTime.UtcNow, TextId = 1, Updated = DateTime.UtcNow };

            context.Add(text1);
            context.Add(text2);
            context.Add(text3);

            await context.SaveChangesAsync();

            KinaUnaTextService kinaUnaTextService = new KinaUnaTextService(context);

            KinaUnaText kinaUnaTextToAdd = new KinaUnaText { LanguageId = 1, Title="Title2", Page = "Page2", Text = "Text2.1", Created = DateTime.UtcNow, Updated = DateTime.UtcNow};

            KinaUnaText addedKinaUnaText = await kinaUnaTextService.AddText(kinaUnaTextToAdd);
            KinaUnaText? dbKinaUnaText = await context.KinaUnaTexts.AsNoTracking().SingleOrDefaultAsync(kt => kt.Id == addedKinaUnaText.Id);
            KinaUnaText savedKinaUnaText = await kinaUnaTextService.GetTextById(addedKinaUnaText.Id);

            Assert.NotNull(addedKinaUnaText);
            Assert.IsType<KinaUnaText>(addedKinaUnaText);
            Assert.Equal(kinaUnaTextToAdd.Title, addedKinaUnaText.Title);
            Assert.Equal(kinaUnaTextToAdd.Page, addedKinaUnaText.Page);
            Assert.Equal(kinaUnaTextToAdd.Text, addedKinaUnaText.Text);
            Assert.Equal(kinaUnaTextToAdd.LanguageId, addedKinaUnaText.LanguageId);

            if (dbKinaUnaText != null)
            {
                Assert.IsType<KinaUnaText>(dbKinaUnaText);
                Assert.Equal(kinaUnaTextToAdd.Title, dbKinaUnaText.Title);
                Assert.Equal(kinaUnaTextToAdd.Page, dbKinaUnaText.Page);
                Assert.Equal(kinaUnaTextToAdd.Text, dbKinaUnaText.Text);
                Assert.Equal(kinaUnaTextToAdd.LanguageId, dbKinaUnaText.LanguageId);
            }
            Assert.NotNull(savedKinaUnaText);
            Assert.IsType<KinaUnaText>(savedKinaUnaText);
            Assert.Equal(kinaUnaTextToAdd.Title, savedKinaUnaText.Title);
            Assert.Equal(kinaUnaTextToAdd.Page, savedKinaUnaText.Page);
            Assert.Equal(kinaUnaTextToAdd.Text, savedKinaUnaText.Text);
            Assert.Equal(kinaUnaTextToAdd.LanguageId, savedKinaUnaText.LanguageId);
        }

        [Fact]
        public async Task AddText_Should_Add_KinaUnaText_For_All_Languages()
        {
            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>().UseInMemoryDatabase("AddText_Should_Add_KinaUnaText_For_All_Languages").Options;
            await using ProgenyDbContext context = new ProgenyDbContext(dbOptions);

            KinaUnaLanguage language1 = new KinaUnaLanguage { Name = "Language1", Code = "Code1", Icon = "Icon1", IconLink = "IconLink1" };
            KinaUnaLanguage language2 = new KinaUnaLanguage { Name = "Language2", Code = "Code2", Icon = "Icon2", IconLink = "IconLink2" };
            KinaUnaLanguage language3 = new KinaUnaLanguage { Name = "Language3", Code = "Code3", Icon = "Icon3", IconLink = "IconLink3" };

            context.Add(language1);
            context.Add(language2);
            context.Add(language3);

            KinaUnaTextNumber kinaUnaTextNumber1 = new KinaUnaTextNumber { DefaultLanguage = 1 };

            context.Add(kinaUnaTextNumber1);

            KinaUnaText text1 = new KinaUnaText { LanguageId = 1, Title = "Title1", Page = "Page1", Text = "Text1.1", Created = DateTime.UtcNow, TextId = 1, Updated = DateTime.UtcNow };
            KinaUnaText text2 = new KinaUnaText { LanguageId = 2, Title = "Title1", Page = "Page1", Text = "Text1.2", Created = DateTime.UtcNow, TextId = 1, Updated = DateTime.UtcNow };
            KinaUnaText text3 = new KinaUnaText { LanguageId = 3, Title = "Title1", Page = "Page1", Text = "Text1.3", Created = DateTime.UtcNow, TextId = 1, Updated = DateTime.UtcNow };

            context.Add(text1);
            context.Add(text2);
            context.Add(text3);

            await context.SaveChangesAsync();

            KinaUnaTextService kinaUnaTextService = new KinaUnaTextService(context);

            KinaUnaText kinaUnaTextToAdd = new KinaUnaText { LanguageId = 1, Title = "Title2", Page = "Page2", Text = "Text2.1", Created = DateTime.UtcNow, Updated = DateTime.UtcNow };

            KinaUnaText addedTextTranslation = await kinaUnaTextService.AddText(kinaUnaTextToAdd);
            List<KinaUnaText> allLanguageVersionsOfAddedKinaUnaText = await context.KinaUnaTexts.Where(kt => kt.Page == kinaUnaTextToAdd.Page && kt.Title == kinaUnaTextToAdd.Title).ToListAsync();

            Assert.NotEmpty(allLanguageVersionsOfAddedKinaUnaText);
            Assert.Equal(3, allLanguageVersionsOfAddedKinaUnaText.Count);
        }

        [Fact]
        public async Task UpdateText_Should_Save_KinaUnaText()
        {
            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>().UseInMemoryDatabase("UpdateText_Should_Save_KinaUnaText").Options;
            await using ProgenyDbContext context = new ProgenyDbContext(dbOptions);

            KinaUnaLanguage language1 = new KinaUnaLanguage { Name = "Language1", Code = "Code1", Icon = "Icon1", IconLink = "IconLink1" };
            KinaUnaLanguage language2 = new KinaUnaLanguage { Name = "Language2", Code = "Code2", Icon = "Icon2", IconLink = "IconLink2" };
            KinaUnaLanguage language3 = new KinaUnaLanguage { Name = "Language3", Code = "Code3", Icon = "Icon3", IconLink = "IconLink3" };

            context.Add(language1);
            context.Add(language2);
            context.Add(language3);

            KinaUnaTextNumber kinaUnaTextNumber1 = new KinaUnaTextNumber { DefaultLanguage = 1 };

            context.Add(kinaUnaTextNumber1);

            KinaUnaText text1 = new KinaUnaText { LanguageId = 1, Title = "Title1", Page = "Page1", Text = "Text1.1", Created = DateTime.UtcNow, TextId = 1, Updated = DateTime.UtcNow };
            KinaUnaText text2 = new KinaUnaText { LanguageId = 2, Title = "Title1", Page = "Page1", Text = "Text1.2", Created = DateTime.UtcNow, TextId = 1, Updated = DateTime.UtcNow };
            KinaUnaText text3 = new KinaUnaText { LanguageId = 3, Title = "Title1", Page = "Page1", Text = "Text1.3", Created = DateTime.UtcNow, TextId = 1, Updated = DateTime.UtcNow };

            context.Add(text1);
            context.Add(text2);
            context.Add(text3);

            await context.SaveChangesAsync();

            KinaUnaTextService kinaUnaTextService = new KinaUnaTextService(context);

            KinaUnaText kinaUnaTextToUpdate = await kinaUnaTextService.GetTextById(1);
            kinaUnaTextToUpdate.Text = "Text1.1 Updated";
            KinaUnaText updatedKinaUnaText = await kinaUnaTextService.UpdateText(1, kinaUnaTextToUpdate);
            KinaUnaText? dbKinaUnaText = await context.KinaUnaTexts.AsNoTracking().SingleOrDefaultAsync(kt => kt.Id == 1);
            KinaUnaText savedKinaUnaText = await kinaUnaTextService.GetTextById(1);

            Assert.NotNull(updatedKinaUnaText);
            Assert.IsType<KinaUnaText>(updatedKinaUnaText);
            Assert.NotEqual(0, updatedKinaUnaText.Id);
            Assert.Equal("Text1.1 Updated", updatedKinaUnaText.Text);
            Assert.Equal("Title1", updatedKinaUnaText.Title);
            Assert.Equal("Page1", updatedKinaUnaText.Page);

            if (dbKinaUnaText != null)
            {
                Assert.IsType<KinaUnaText>(dbKinaUnaText);
                Assert.NotEqual(0, dbKinaUnaText.Id);
                Assert.Equal("Text1.1 Updated", dbKinaUnaText.Text);
                Assert.Equal("Title1", dbKinaUnaText.Title);
                Assert.Equal("Page1", dbKinaUnaText.Page);
            }

            Assert.NotNull(savedKinaUnaText);
            Assert.IsType<KinaUnaText>(savedKinaUnaText);
            Assert.NotEqual(0, savedKinaUnaText.Id);
            Assert.Equal("Text1.1 Updated", savedKinaUnaText.Text);
            Assert.Equal("Title1", savedKinaUnaText.Title);
            Assert.Equal("Page1", savedKinaUnaText.Page);
        }

        [Fact]
        public async Task DeleteText_Should_Remove_All_Language_Versions_Of_The_KinaUnaText()
        {
            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>().UseInMemoryDatabase("DeleteText_Should_Remove_All_Language_Versions_Of_The_KinaUnaText").Options;
            await using ProgenyDbContext context = new ProgenyDbContext(dbOptions);

            KinaUnaLanguage language1 = new KinaUnaLanguage { Name = "Language1", Code = "Code1", Icon = "Icon1", IconLink = "IconLink1" };
            KinaUnaLanguage language2 = new KinaUnaLanguage { Name = "Language2", Code = "Code2", Icon = "Icon2", IconLink = "IconLink2" };
            KinaUnaLanguage language3 = new KinaUnaLanguage { Name = "Language3", Code = "Code3", Icon = "Icon3", IconLink = "IconLink3" };

            context.Add(language1);
            context.Add(language2);
            context.Add(language3);

            KinaUnaTextNumber textNumber1 = new KinaUnaTextNumber { DefaultLanguage = 1 };
            KinaUnaTextNumber textNumber2 = new KinaUnaTextNumber { DefaultLanguage = 1 };
            KinaUnaTextNumber textNumber3 = new KinaUnaTextNumber { DefaultLanguage = 1 };
            context.Add(textNumber1);
            context.Add(textNumber2);
            context.Add(textNumber3);

            KinaUnaText text1 = new KinaUnaText { LanguageId = 1, Title = "Title1", Page = "Page1", Text = "Text1.1", Created = DateTime.UtcNow, TextId = 1, Updated = DateTime.UtcNow, };
            KinaUnaText text2 = new KinaUnaText { LanguageId = 2, Title = "Title1", Page = "Page1", Text = "Text1.2", Created = DateTime.UtcNow, TextId = 1, Updated = DateTime.UtcNow };
            KinaUnaText text3 = new KinaUnaText { LanguageId = 3, Title = "Title1", Page = "Page1", Text = "Text1.3", Created = DateTime.UtcNow, TextId = 1, Updated = DateTime.UtcNow };

            KinaUnaText text4 = new KinaUnaText { LanguageId = 1, Title = "Title2", Page = "Page1", Text = "Text2.1", Created = DateTime.UtcNow, TextId = 2, Updated = DateTime.UtcNow, };
            KinaUnaText text5 = new KinaUnaText { LanguageId = 2, Title = "Title2", Page = "Page1", Text = "Text2.2", Created = DateTime.UtcNow, TextId = 2, Updated = DateTime.UtcNow };
            KinaUnaText text6 = new KinaUnaText { LanguageId = 3, Title = "Title2", Page = "Page1", Text = "Text2.3", Created = DateTime.UtcNow, TextId = 2, Updated = DateTime.UtcNow };

            KinaUnaText text7 = new KinaUnaText { LanguageId = 1, Title = "Title3", Page = "Page1", Text = "Text3.1", Created = DateTime.UtcNow, TextId = 3, Updated = DateTime.UtcNow, };
            KinaUnaText text8 = new KinaUnaText { LanguageId = 2, Title = "Title3", Page = "Page1", Text = "Text3.2", Created = DateTime.UtcNow, TextId = 3, Updated = DateTime.UtcNow };
            KinaUnaText text9 = new KinaUnaText { LanguageId = 3, Title = "Title3", Page = "Page1", Text = "Text3.3", Created = DateTime.UtcNow, TextId = 3, Updated = DateTime.UtcNow };

            context.Add(text1);
            context.Add(text2);
            context.Add(text3);

            context.Add(text4);
            context.Add(text5);
            context.Add(text6);

            context.Add(text7);
            context.Add(text8);
            context.Add(text9);

            await context.SaveChangesAsync();

            KinaUnaTextService kinaUnaTextService = new KinaUnaTextService(context);

            KinaUnaText kinaUnaTextToDelete = await kinaUnaTextService.GetTextById(1);
            List<KinaUnaText> kinaUnaTextsListBeforeDelete = await context.KinaUnaTexts.Where(kt => kt.Page == kinaUnaTextToDelete.Page && kt.Title == kinaUnaTextToDelete.Title).ToListAsync();
            int allKinaUnaTextsCountBeforeDelete = context.KinaUnaTexts.Count();
            await kinaUnaTextService.DeleteText(1);
            List<KinaUnaText> textTranslationListAfterDelete = await context.KinaUnaTexts.Where(kt => kt.Page == kinaUnaTextToDelete.Page && kt.Title == kinaUnaTextToDelete.Title).ToListAsync();
            int allKinaUnaTextsCountAfterDelete = context.KinaUnaTexts.Count();
            KinaUnaText? deletedKinaUnaText = await context.KinaUnaTexts.SingleOrDefaultAsync(kt => kt.Id == 1);

            Assert.Null(deletedKinaUnaText);
            Assert.Equal(3, kinaUnaTextsListBeforeDelete.Count);
            Assert.Empty(textTranslationListAfterDelete);
            Assert.Equal(9, allKinaUnaTextsCountBeforeDelete);
            Assert.Equal(6, allKinaUnaTextsCountAfterDelete);
        }

        [Fact]
        public async Task DeleteSingleText_Should_Remove_Only_1_Language_Version_Of_The_KinaUnaText()
        {
            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>().UseInMemoryDatabase("DeleteSingleText_Should_Remove_Only_1_Language_Version_Of_The_KinaUnaText").Options;
            await using ProgenyDbContext context = new ProgenyDbContext(dbOptions);

            KinaUnaLanguage language1 = new KinaUnaLanguage { Name = "Language1", Code = "Code1", Icon = "Icon1", IconLink = "IconLink1" };
            KinaUnaLanguage language2 = new KinaUnaLanguage { Name = "Language2", Code = "Code2", Icon = "Icon2", IconLink = "IconLink2" };
            KinaUnaLanguage language3 = new KinaUnaLanguage { Name = "Language3", Code = "Code3", Icon = "Icon3", IconLink = "IconLink3" };

            context.Add(language1);
            context.Add(language2);
            context.Add(language3);

            KinaUnaTextNumber textNumber1 = new KinaUnaTextNumber { DefaultLanguage = 1 };
            KinaUnaTextNumber textNumber2 = new KinaUnaTextNumber { DefaultLanguage = 1 };
            KinaUnaTextNumber textNumber3 = new KinaUnaTextNumber { DefaultLanguage = 1 };
            context.Add(textNumber1);
            context.Add(textNumber2);
            context.Add(textNumber3);

            KinaUnaText text1 = new KinaUnaText { LanguageId = 1, Title = "Title1", Page = "Page1", Text = "Text1.1", Created = DateTime.UtcNow, TextId = 1, Updated = DateTime.UtcNow, };
            KinaUnaText text2 = new KinaUnaText { LanguageId = 2, Title = "Title1", Page = "Page1", Text = "Text1.2", Created = DateTime.UtcNow, TextId = 1, Updated = DateTime.UtcNow };
            KinaUnaText text3 = new KinaUnaText { LanguageId = 3, Title = "Title1", Page = "Page1", Text = "Text1.3", Created = DateTime.UtcNow, TextId = 1, Updated = DateTime.UtcNow };

            KinaUnaText text4 = new KinaUnaText { LanguageId = 1, Title = "Title2", Page = "Page1", Text = "Text2.1", Created = DateTime.UtcNow, TextId = 2, Updated = DateTime.UtcNow, };
            KinaUnaText text5 = new KinaUnaText { LanguageId = 2, Title = "Title2", Page = "Page1", Text = "Text2.2", Created = DateTime.UtcNow, TextId = 2, Updated = DateTime.UtcNow };
            KinaUnaText text6 = new KinaUnaText { LanguageId = 3, Title = "Title2", Page = "Page1", Text = "Text2.3", Created = DateTime.UtcNow, TextId = 2, Updated = DateTime.UtcNow };

            KinaUnaText text7 = new KinaUnaText { LanguageId = 1, Title = "Title3", Page = "Page1", Text = "Text3.1", Created = DateTime.UtcNow, TextId = 3, Updated = DateTime.UtcNow, };
            KinaUnaText text8 = new KinaUnaText { LanguageId = 2, Title = "Title3", Page = "Page1", Text = "Text3.2", Created = DateTime.UtcNow, TextId = 3, Updated = DateTime.UtcNow };
            KinaUnaText text9 = new KinaUnaText { LanguageId = 3, Title = "Title3", Page = "Page1", Text = "Text3.3", Created = DateTime.UtcNow, TextId = 3, Updated = DateTime.UtcNow };

            context.Add(text1);
            context.Add(text2);
            context.Add(text3);

            context.Add(text4);
            context.Add(text5);
            context.Add(text6);

            context.Add(text7);
            context.Add(text8);
            context.Add(text9);

            await context.SaveChangesAsync();

            KinaUnaTextService kinaUnaTextService = new KinaUnaTextService(context);

            KinaUnaText kinaUnaTextToDelete = await kinaUnaTextService.GetTextById(1);
            List<KinaUnaText> kinaUnaTextsListBeforeDelete = await context.KinaUnaTexts.Where(kt => kt.Page == kinaUnaTextToDelete.Page && kt.Title == kinaUnaTextToDelete.Title).ToListAsync();
            int allKinaUnaTextsCountBeforeDelete = context.KinaUnaTexts.Count();
            await kinaUnaTextService.DeleteSingleText(1);
            List<KinaUnaText> kinaUnaTextsListAfterDelete = await context.KinaUnaTexts.Where(kt => kt.Page == kinaUnaTextToDelete.Page && kt.Title == kinaUnaTextToDelete.Title).ToListAsync();
            int allKinaUnaTextsCountAfterDelete = context.KinaUnaTexts.Count();
            KinaUnaText? deletedKinaUnaText = await context.KinaUnaTexts.SingleOrDefaultAsync(tt => tt.Id == 1);

            Assert.Null(deletedKinaUnaText);
            Assert.Equal(3, kinaUnaTextsListBeforeDelete.Count);
            Assert.NotEmpty(kinaUnaTextsListAfterDelete);
            Assert.Equal(9, allKinaUnaTextsCountBeforeDelete);
            Assert.Equal(8, allKinaUnaTextsCountAfterDelete);
        }
    }
}
