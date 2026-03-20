using KinaUna.Data.Contexts;
using KinaUna.Data.Models;
using KinaUna.Data.Models.AccessManagement;
using KinaUna.Data.Models.Search;
using KinaUnaProgenyApi.Services;
using KinaUnaProgenyApi.Services.AccessManagementService;
using KinaUnaProgenyApi.Services.Search;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace KinaUnaProgenyApi.Tests.Services.Search
{
    public class SearchServiceSearchVocabularyTests
    {
        private readonly Mock<IAccessManagementService> _mockAccessManagementService;
        private readonly Mock<ITimelineService> _mockTimelineService;

        public SearchServiceSearchVocabularyTests()
        {
            _mockAccessManagementService = new Mock<IAccessManagementService>();
            _mockTimelineService = new Mock<ITimelineService>();
        }

        private static ProgenyDbContext GetInMemoryProgenyDbContext(string dbName)
        {
            DbContextOptions<ProgenyDbContext> options = new DbContextOptionsBuilder<ProgenyDbContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .Options;
            return new ProgenyDbContext(options);
        }

        private static MediaDbContext GetInMemoryMediaDbContext(string dbName)
        {
            DbContextOptions<MediaDbContext> options = new DbContextOptionsBuilder<MediaDbContext>()
                .UseInMemoryDatabase(databaseName: dbName + "_media")
                .Options;
            return new MediaDbContext(options);
        }

        private static UserInfo CreateTestUserInfo(string userId = "testuser@test.com")
        {
            return new UserInfo
            {
                UserId = userId,
                UserEmail = userId
            };
        }

        #region SearchVocabularyItems Tests

        [Fact]
        public async Task SearchVocabularyItems_Should_Return_Empty_Response_When_UserInfo_Is_Null()
        {
            // Arrange
            await using ProgenyDbContext progenyContext = GetInMemoryProgenyDbContext("SearchVocabularyItems_NullUser");
            await using MediaDbContext mediaContext = GetInMemoryMediaDbContext("SearchVocabularyItems_NullUser");

            SearchService service = new(progenyContext, mediaContext, _mockAccessManagementService.Object, _mockTimelineService.Object);

            SearchRequest request = new()
            {
                ProgenyIds = [1],
                Query = "test"
            };

            // Act
            SearchResponse<VocabularyItem> result = await service.SearchVocabularyItems(request, null!);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result.Results);
            Assert.Equal(request, result.SearchRequest);
        }

        [Fact]
        public async Task SearchVocabularyItems_Should_Return_Empty_Response_When_Query_Is_Empty()
        {
            // Arrange
            await using ProgenyDbContext progenyContext = GetInMemoryProgenyDbContext("SearchVocabularyItems_EmptyQuery");
            await using MediaDbContext mediaContext = GetInMemoryMediaDbContext("SearchVocabularyItems_EmptyQuery");

            SearchService service = new(progenyContext, mediaContext, _mockAccessManagementService.Object, _mockTimelineService.Object);
            UserInfo userInfo = CreateTestUserInfo();

            SearchRequest request = new()
            {
                ProgenyIds = [1],
                Query = ""
            };

            // Act
            SearchResponse<VocabularyItem> result = await service.SearchVocabularyItems(request, userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result.Results);
            Assert.Equal(request, result.SearchRequest);
        }

        [Fact]
        public async Task SearchVocabularyItems_Should_Return_Empty_Response_When_Query_Is_Whitespace()
        {
            // Arrange
            await using ProgenyDbContext progenyContext = GetInMemoryProgenyDbContext("SearchVocabularyItems_WhitespaceQuery");
            await using MediaDbContext mediaContext = GetInMemoryMediaDbContext("SearchVocabularyItems_WhitespaceQuery");

            SearchService service = new(progenyContext, mediaContext, _mockAccessManagementService.Object, _mockTimelineService.Object);
            UserInfo userInfo = CreateTestUserInfo();

            SearchRequest request = new()
            {
                ProgenyIds = [1],
                Query = "   "
            };

            // Act
            SearchResponse<VocabularyItem> result = await service.SearchVocabularyItems(request, userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result.Results);
        }

        [Fact]
        public async Task SearchVocabularyItems_Should_Return_Matching_Items_By_Word()
        {
            // Arrange
            await using ProgenyDbContext progenyContext = GetInMemoryProgenyDbContext("SearchVocabularyItems_MatchByWord");
            await using MediaDbContext mediaContext = GetInMemoryMediaDbContext("SearchVocabularyItems_MatchByWord");

            VocabularyItem vocabularyItem = new()
            {
                WordId = 1,
                ProgenyId = 1,
                Word = "Hello",
                Description = "A greeting",
                Language = "English",
                SoundsLike = "heh-loh",
                Date = DateTime.UtcNow.AddDays(-5),
                DateAdded = DateTime.UtcNow.AddDays(-5)
            };

            progenyContext.VocabularyDb.Add(vocabularyItem);
            await progenyContext.SaveChangesAsync(TestContext.Current.CancellationToken);

            UserInfo userInfo = CreateTestUserInfo();
            TimelineItemPermission permission = new() { PermissionLevel = PermissionLevel.View };

            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(1, userInfo, PermissionLevel.View))
                .ReturnsAsync(true);

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Vocabulary, 1, userInfo, PermissionLevel.View))
                .ReturnsAsync(true);

            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Vocabulary, 1, 1, 0, userInfo, null))
                .ReturnsAsync(permission);

            SearchService service = new(progenyContext, mediaContext, _mockAccessManagementService.Object, _mockTimelineService.Object);

            SearchRequest request = new()
            {
                ProgenyIds = [1],
                Query = "hello"
            };

            // Act
            SearchResponse<VocabularyItem> result = await service.SearchVocabularyItems(request, userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result.Results);
            Assert.Equal("Hello", result.Results[0].Word);
        }

        [Fact]
        public async Task SearchVocabularyItems_Should_Return_Matching_Items_By_Description()
        {
            // Arrange
            await using ProgenyDbContext progenyContext = GetInMemoryProgenyDbContext("SearchVocabularyItems_MatchByDescription");
            await using MediaDbContext mediaContext = GetInMemoryMediaDbContext("SearchVocabularyItems_MatchByDescription");

            VocabularyItem vocabularyItem = new()
            {
                WordId = 1,
                ProgenyId = 1,
                Word = "Mama",
                Description = "Mother or parent",
                Language = "English",
                SoundsLike = "mah-mah",
                Date = DateTime.UtcNow,
                DateAdded = DateTime.UtcNow
            };

            progenyContext.VocabularyDb.Add(vocabularyItem);
            await progenyContext.SaveChangesAsync(TestContext.Current.CancellationToken);

            UserInfo userInfo = CreateTestUserInfo();
            TimelineItemPermission permission = new() { PermissionLevel = PermissionLevel.View };

            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(1, userInfo, PermissionLevel.View))
                .ReturnsAsync(true);

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Vocabulary, 1, userInfo, PermissionLevel.View))
                .ReturnsAsync(true);

            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Vocabulary, 1, 1, 0, userInfo, null))
                .ReturnsAsync(permission);

            SearchService service = new(progenyContext, mediaContext, _mockAccessManagementService.Object, _mockTimelineService.Object);

            SearchRequest request = new()
            {
                ProgenyIds = [1],
                Query = "parent"
            };

            // Act
            SearchResponse<VocabularyItem> result = await service.SearchVocabularyItems(request, userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result.Results);
            Assert.Equal("Mama", result.Results[0].Word);
        }

        [Fact]
        public async Task SearchVocabularyItems_Should_Return_Matching_Items_By_Language()
        {
            // Arrange
            await using ProgenyDbContext progenyContext = GetInMemoryProgenyDbContext("SearchVocabularyItems_MatchByLanguage");
            await using MediaDbContext mediaContext = GetInMemoryMediaDbContext("SearchVocabularyItems_MatchByLanguage");

            VocabularyItem vocabularyItem = new()
            {
                WordId = 1,
                ProgenyId = 1,
                Word = "Hola",
                Description = "Hello",
                Language = "Spanish",
                SoundsLike = "oh-lah",
                Date = DateTime.UtcNow,
                DateAdded = DateTime.UtcNow
            };

            progenyContext.VocabularyDb.Add(vocabularyItem);
            await progenyContext.SaveChangesAsync(TestContext.Current.CancellationToken);

            UserInfo userInfo = CreateTestUserInfo();
            TimelineItemPermission permission = new() { PermissionLevel = PermissionLevel.View };

            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(1, userInfo, PermissionLevel.View))
                .ReturnsAsync(true);

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Vocabulary, 1, userInfo, PermissionLevel.View))
                .ReturnsAsync(true);

            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Vocabulary, 1, 1, 0, userInfo, null))
                .ReturnsAsync(permission);

            SearchService service = new(progenyContext, mediaContext, _mockAccessManagementService.Object, _mockTimelineService.Object);

            SearchRequest request = new()
            {
                ProgenyIds = [1],
                Query = "spanish"
            };

            // Act
            SearchResponse<VocabularyItem> result = await service.SearchVocabularyItems(request, userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result.Results);
            Assert.Equal("Hola", result.Results[0].Word);
        }

        [Fact]
        public async Task SearchVocabularyItems_Should_Return_Matching_Items_By_SoundsLike()
        {
            // Arrange
            await using ProgenyDbContext progenyContext = GetInMemoryProgenyDbContext("SearchVocabularyItems_MatchBySoundsLike");
            await using MediaDbContext mediaContext = GetInMemoryMediaDbContext("SearchVocabularyItems_MatchBySoundsLike");

            VocabularyItem vocabularyItem = new()
            {
                WordId = 1,
                ProgenyId = 1,
                Word = "Dog",
                Description = "A pet animal",
                Language = "English",
                SoundsLike = "dawg",
                Date = DateTime.UtcNow,
                DateAdded = DateTime.UtcNow
            };

            progenyContext.VocabularyDb.Add(vocabularyItem);
            await progenyContext.SaveChangesAsync(TestContext.Current.CancellationToken);

            UserInfo userInfo = CreateTestUserInfo();
            TimelineItemPermission permission = new() { PermissionLevel = PermissionLevel.View };

            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(1, userInfo, PermissionLevel.View))
                .ReturnsAsync(true);

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Vocabulary, 1, userInfo, PermissionLevel.View))
                .ReturnsAsync(true);

            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Vocabulary, 1, 1, 0, userInfo, null))
                .ReturnsAsync(permission);

            SearchService service = new(progenyContext, mediaContext, _mockAccessManagementService.Object, _mockTimelineService.Object);

            SearchRequest request = new()
            {
                ProgenyIds = [1],
                Query = "dawg"
            };

            // Act
            SearchResponse<VocabularyItem> result = await service.SearchVocabularyItems(request, userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result.Results);
            Assert.Equal("Dog", result.Results[0].Word);
        }

        [Fact]
        public async Task SearchVocabularyItems_Should_Be_Case_Insensitive()
        {
            // Arrange
            await using ProgenyDbContext progenyContext = GetInMemoryProgenyDbContext("SearchVocabularyItems_CaseInsensitive");
            await using MediaDbContext mediaContext = GetInMemoryMediaDbContext("SearchVocabularyItems_CaseInsensitive");

            VocabularyItem vocabularyItem = new()
            {
                WordId = 1,
                ProgenyId = 1,
                Word = "HELLO",
                Description = "A GREETING",
                Language = "ENGLISH",
                SoundsLike = "HEH-LOH",
                Date = DateTime.UtcNow,
                DateAdded = DateTime.UtcNow
            };

            progenyContext.VocabularyDb.Add(vocabularyItem);
            await progenyContext.SaveChangesAsync(TestContext.Current.CancellationToken);

            UserInfo userInfo = CreateTestUserInfo();
            TimelineItemPermission permission = new() { PermissionLevel = PermissionLevel.View };

            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(1, userInfo, PermissionLevel.View))
                .ReturnsAsync(true);

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Vocabulary, 1, userInfo, PermissionLevel.View))
                .ReturnsAsync(true);

            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Vocabulary, 1, 1, 0, userInfo, null))
                .ReturnsAsync(permission);

            SearchService service = new(progenyContext, mediaContext, _mockAccessManagementService.Object, _mockTimelineService.Object);

            SearchRequest request = new()
            {
                ProgenyIds = [1],
                Query = "hello"
            };

            // Act
            SearchResponse<VocabularyItem> result = await service.SearchVocabularyItems(request, userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result.Results);
        }

        [Fact]
        public async Task SearchVocabularyItems_Should_Skip_Progeny_Without_Permission()
        {
            // Arrange
            await using ProgenyDbContext progenyContext = GetInMemoryProgenyDbContext("SearchVocabularyItems_NoProgenyPermission");
            await using MediaDbContext mediaContext = GetInMemoryMediaDbContext("SearchVocabularyItems_NoProgenyPermission");

            VocabularyItem vocabularyItem = new()
            {
                WordId = 1,
                ProgenyId = 1,
                Word = "Hello",
                Description = "A greeting",
                Language = "English",
                SoundsLike = "heh-loh",
                Date = DateTime.UtcNow,
                DateAdded = DateTime.UtcNow
            };

            progenyContext.VocabularyDb.Add(vocabularyItem);
            await progenyContext.SaveChangesAsync(TestContext.Current.CancellationToken);

            UserInfo userInfo = CreateTestUserInfo();

            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(1, userInfo, PermissionLevel.View))
                .ReturnsAsync(false);

            SearchService service = new(progenyContext, mediaContext, _mockAccessManagementService.Object, _mockTimelineService.Object);

            SearchRequest request = new()
            {
                ProgenyIds = [1],
                Query = "hello"
            };

            // Act
            SearchResponse<VocabularyItem> result = await service.SearchVocabularyItems(request, userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result.Results);
        }

        [Fact]
        public async Task SearchVocabularyItems_Should_Skip_Items_Without_Item_Permission()
        {
            // Arrange
            await using ProgenyDbContext progenyContext = GetInMemoryProgenyDbContext("SearchVocabularyItems_NoItemPermission");
            await using MediaDbContext mediaContext = GetInMemoryMediaDbContext("SearchVocabularyItems_NoItemPermission");

            VocabularyItem vocabularyItem = new()
            {
                WordId = 1,
                ProgenyId = 1,
                Word = "Hello",
                Description = "A greeting",
                Language = "English",
                SoundsLike = "heh-loh",
                Date = DateTime.UtcNow,
                DateAdded = DateTime.UtcNow
            };

            progenyContext.VocabularyDb.Add(vocabularyItem);
            await progenyContext.SaveChangesAsync(TestContext.Current.CancellationToken);

            UserInfo userInfo = CreateTestUserInfo();

            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(1, userInfo, PermissionLevel.View))
                .ReturnsAsync(true);

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Vocabulary, 1, userInfo, PermissionLevel.View))
                .ReturnsAsync(false);

            SearchService service = new(progenyContext, mediaContext, _mockAccessManagementService.Object, _mockTimelineService.Object);

            SearchRequest request = new()
            {
                ProgenyIds = [1],
                Query = "hello"
            };

            // Act
            SearchResponse<VocabularyItem> result = await service.SearchVocabularyItems(request, userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result.Results);
        }

        [Fact]
        public async Task SearchVocabularyItems_Should_Include_ItemPermission_In_Results()
        {
            // Arrange
            await using ProgenyDbContext progenyContext = GetInMemoryProgenyDbContext("SearchVocabularyItems_IncludePermission");
            await using MediaDbContext mediaContext = GetInMemoryMediaDbContext("SearchVocabularyItems_IncludePermission");

            VocabularyItem vocabularyItem = new()
            {
                WordId = 1,
                ProgenyId = 1,
                Word = "Hello",
                Description = "A greeting",
                Language = "English",
                SoundsLike = "heh-loh",
                Date = DateTime.UtcNow,
                DateAdded = DateTime.UtcNow
            };

            progenyContext.VocabularyDb.Add(vocabularyItem);
            await progenyContext.SaveChangesAsync(TestContext.Current.CancellationToken);

            UserInfo userInfo = CreateTestUserInfo();
            TimelineItemPermission permission = new() { PermissionLevel = PermissionLevel.Edit };

            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(1, userInfo, PermissionLevel.View))
                .ReturnsAsync(true);

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Vocabulary, 1, userInfo, PermissionLevel.View))
                .ReturnsAsync(true);

            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Vocabulary, 1, 1, 0, userInfo, null))
                .ReturnsAsync(permission);

            SearchService service = new(progenyContext, mediaContext, _mockAccessManagementService.Object, _mockTimelineService.Object);

            SearchRequest request = new()
            {
                ProgenyIds = [1],
                Query = "hello"
            };

            // Act
            SearchResponse<VocabularyItem> result = await service.SearchVocabularyItems(request, userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result.Results);
            Assert.NotNull(result.Results[0].ItemPermission);
            Assert.Equal(PermissionLevel.Edit, result.Results[0].ItemPermission.PermissionLevel);
        }

        [Fact]
        public async Task SearchVocabularyItems_Should_Search_Across_Multiple_Progenies()
        {
            // Arrange
            await using ProgenyDbContext progenyContext = GetInMemoryProgenyDbContext("SearchVocabularyItems_MultipleProgenies");
            await using MediaDbContext mediaContext = GetInMemoryMediaDbContext("SearchVocabularyItems_MultipleProgenies");

            VocabularyItem vocabularyItem1 = new()
            {
                WordId = 1,
                ProgenyId = 1,
                Word = "Hello",
                Description = "A greeting",
                Language = "English",
                SoundsLike = "heh-loh",
                Date = DateTime.UtcNow,
                DateAdded = DateTime.UtcNow
            };

            VocabularyItem vocabularyItem2 = new()
            {
                WordId = 2,
                ProgenyId = 2,
                Word = "Hello World",
                Description = "Programming",
                Language = "English",
                SoundsLike = "heh-loh world",
                Date = DateTime.UtcNow,
                DateAdded = DateTime.UtcNow
            };

            progenyContext.VocabularyDb.AddRange(vocabularyItem1, vocabularyItem2);
            await progenyContext.SaveChangesAsync(TestContext.Current.CancellationToken);

            UserInfo userInfo = CreateTestUserInfo();
            TimelineItemPermission permission = new() { PermissionLevel = PermissionLevel.View };

            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(It.IsAny<int>(), userInfo, PermissionLevel.View))
                .ReturnsAsync(true);

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Vocabulary, It.IsAny<int>(), userInfo, PermissionLevel.View))
                .ReturnsAsync(true);

            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Vocabulary, It.IsAny<int>(), It.IsAny<int>(), 0, userInfo, null))
                .ReturnsAsync(permission);

            SearchService service = new(progenyContext, mediaContext, _mockAccessManagementService.Object, _mockTimelineService.Object);

            SearchRequest request = new()
            {
                ProgenyIds = [1, 2],
                Query = "hello"
            };

            // Act
            SearchResponse<VocabularyItem> result = await service.SearchVocabularyItems(request, userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Results.Count);
        }

        [Fact]
        public async Task SearchVocabularyItems_Should_Respect_Pagination_Skip()
        {
            // Arrange
            await using ProgenyDbContext progenyContext = GetInMemoryProgenyDbContext("SearchVocabularyItems_PaginationSkip");
            await using MediaDbContext mediaContext = GetInMemoryMediaDbContext("SearchVocabularyItems_PaginationSkip");

            for (int i = 1; i <= 5; i++)
            {
                progenyContext.VocabularyDb.Add(new VocabularyItem
                {
                    WordId = i,
                    ProgenyId = 1,
                    Word = $"Word{i}",
                    Description = "Test description",
                    Language = "English",
                    SoundsLike = $"word{i}",
                    Date = DateTime.UtcNow.AddDays(-i),
                    DateAdded = DateTime.UtcNow.AddDays(-i)
                });
            }
            await progenyContext.SaveChangesAsync(TestContext.Current.CancellationToken);

            UserInfo userInfo = CreateTestUserInfo();
            TimelineItemPermission permission = new() { PermissionLevel = PermissionLevel.View };

            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(1, userInfo, PermissionLevel.View))
                .ReturnsAsync(true);

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Vocabulary, It.IsAny<int>(), userInfo, PermissionLevel.View))
                .ReturnsAsync(true);

            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Vocabulary, It.IsAny<int>(), 1, 0, userInfo, null))
                .ReturnsAsync(permission);

            SearchService service = new(progenyContext, mediaContext, _mockAccessManagementService.Object, _mockTimelineService.Object);

            SearchRequest request = new()
            {
                ProgenyIds = [1],
                Query = "word",
                Skip = 2,
                NumberOfItems = 10
            };

            // Act
            SearchResponse<VocabularyItem> result = await service.SearchVocabularyItems(request, userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(3, result.Results.Count);
            Assert.Equal(5, result.TotalCount);
        }

        [Fact]
        public async Task SearchVocabularyItems_Should_Respect_Pagination_NumberOfItems()
        {
            // Arrange
            await using ProgenyDbContext progenyContext = GetInMemoryProgenyDbContext("SearchVocabularyItems_PaginationLimit");
            await using MediaDbContext mediaContext = GetInMemoryMediaDbContext("SearchVocabularyItems_PaginationLimit");

            for (int i = 1; i <= 5; i++)
            {
                progenyContext.VocabularyDb.Add(new VocabularyItem
                {
                    WordId = i,
                    ProgenyId = 1,
                    Word = $"Word{i}",
                    Description = "Test description",
                    Language = "English",
                    SoundsLike = $"word{i}",
                    Date = DateTime.UtcNow.AddDays(-i),
                    DateAdded = DateTime.UtcNow.AddDays(-i)
                });
            }
            await progenyContext.SaveChangesAsync(TestContext.Current.CancellationToken);

            UserInfo userInfo = CreateTestUserInfo();
            TimelineItemPermission permission = new() { PermissionLevel = PermissionLevel.View };

            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(1, userInfo, PermissionLevel.View))
                .ReturnsAsync(true);

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Vocabulary, It.IsAny<int>(), userInfo, PermissionLevel.View))
                .ReturnsAsync(true);

            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Vocabulary, It.IsAny<int>(), 1, 0, userInfo, null))
                .ReturnsAsync(permission);

            SearchService service = new(progenyContext, mediaContext, _mockAccessManagementService.Object, _mockTimelineService.Object);

            SearchRequest request = new()
            {
                ProgenyIds = [1],
                Query = "word",
                Skip = 0,
                NumberOfItems = 2
            };

            // Act
            SearchResponse<VocabularyItem> result = await service.SearchVocabularyItems(request, userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Results.Count);
            Assert.Equal(5, result.TotalCount);
            Assert.Equal(3, result.RemainingItems);
        }

        [Fact]
        public async Task SearchVocabularyItems_Should_Sort_By_Date_Descending_When_Sort_Is_0()
        {
            // Arrange
            await using ProgenyDbContext progenyContext = GetInMemoryProgenyDbContext("SearchVocabularyItems_SortDescending");
            await using MediaDbContext mediaContext = GetInMemoryMediaDbContext("SearchVocabularyItems_SortDescending");

            VocabularyItem oldItem = new()
            {
                WordId = 1,
                ProgenyId = 1,
                Word = "OldWord",
                Description = "Test",
                Language = "English",
                SoundsLike = "old",
                Date = DateTime.UtcNow.AddDays(-10),
                DateAdded = DateTime.UtcNow.AddDays(-10)
            };

            VocabularyItem newItem = new()
            {
                WordId = 2,
                ProgenyId = 1,
                Word = "NewWord",
                Description = "Test",
                Language = "English",
                SoundsLike = "new",
                Date = DateTime.UtcNow,
                DateAdded = DateTime.UtcNow
            };

            progenyContext.VocabularyDb.AddRange(oldItem, newItem);
            await progenyContext.SaveChangesAsync(TestContext.Current.CancellationToken);

            UserInfo userInfo = CreateTestUserInfo();
            TimelineItemPermission permission = new() { PermissionLevel = PermissionLevel.View };

            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(1, userInfo, PermissionLevel.View))
                .ReturnsAsync(true);

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Vocabulary, It.IsAny<int>(), userInfo, PermissionLevel.View))
                .ReturnsAsync(true);

            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Vocabulary, It.IsAny<int>(), 1, 0, userInfo, null))
                .ReturnsAsync(permission);

            SearchService service = new(progenyContext, mediaContext, _mockAccessManagementService.Object, _mockTimelineService.Object);

            SearchRequest request = new()
            {
                ProgenyIds = [1],
                Query = "word",
                Sort = 0
            };

            // Act
            SearchResponse<VocabularyItem> result = await service.SearchVocabularyItems(request, userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Results.Count);
            Assert.Equal("NewWord", result.Results[0].Word);
            Assert.Equal("OldWord", result.Results[1].Word);
        }

        [Fact]
        public async Task SearchVocabularyItems_Should_Sort_By_Date_Ascending_When_Sort_Is_1()
        {
            // Arrange
            await using ProgenyDbContext progenyContext = GetInMemoryProgenyDbContext("SearchVocabularyItems_SortAscending");
            await using MediaDbContext mediaContext = GetInMemoryMediaDbContext("SearchVocabularyItems_SortAscending");

            VocabularyItem oldItem = new()
            {
                WordId = 1,
                ProgenyId = 1,
                Word = "OldWord",
                Description = "Test",
                Language = "English",
                SoundsLike = "old",
                Date = DateTime.UtcNow.AddDays(-10),
                DateAdded = DateTime.UtcNow.AddDays(-10)
            };

            VocabularyItem newItem = new()
            {
                WordId = 2,
                ProgenyId = 1,
                Word = "NewWord",
                Description = "Test",
                Language = "English",
                SoundsLike = "new",
                Date = DateTime.UtcNow,
                DateAdded = DateTime.UtcNow
            };

            progenyContext.VocabularyDb.AddRange(oldItem, newItem);
            await progenyContext.SaveChangesAsync(TestContext.Current.CancellationToken);

            UserInfo userInfo = CreateTestUserInfo();
            TimelineItemPermission permission = new() { PermissionLevel = PermissionLevel.View };

            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(1, userInfo, PermissionLevel.View))
                .ReturnsAsync(true);

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Vocabulary, It.IsAny<int>(), userInfo, PermissionLevel.View))
                .ReturnsAsync(true);

            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Vocabulary, It.IsAny<int>(), 1, 0, userInfo, null))
                .ReturnsAsync(permission);

            SearchService service = new(progenyContext, mediaContext, _mockAccessManagementService.Object, _mockTimelineService.Object);

            SearchRequest request = new()
            {
                ProgenyIds = [1],
                Query = "word",
                Sort = 1
            };

            // Act
            SearchResponse<VocabularyItem> result = await service.SearchVocabularyItems(request, userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Results.Count);
            Assert.Equal("OldWord", result.Results[0].Word);
            Assert.Equal("NewWord", result.Results[1].Word);
        }

        [Fact]
        public async Task SearchVocabularyItems_Should_Trim_Query()
        {
            // Arrange
            await using ProgenyDbContext progenyContext = GetInMemoryProgenyDbContext("SearchVocabularyItems_TrimQuery");
            await using MediaDbContext mediaContext = GetInMemoryMediaDbContext("SearchVocabularyItems_TrimQuery");

            VocabularyItem vocabularyItem = new()
            {
                WordId = 1,
                ProgenyId = 1,
                Word = "Hello",
                Description = "A greeting",
                Language = "English",
                SoundsLike = "heh-loh",
                Date = DateTime.UtcNow,
                DateAdded = DateTime.UtcNow
            };

            progenyContext.VocabularyDb.Add(vocabularyItem);
            await progenyContext.SaveChangesAsync(TestContext.Current.CancellationToken);

            UserInfo userInfo = CreateTestUserInfo();
            TimelineItemPermission permission = new() { PermissionLevel = PermissionLevel.View };

            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(1, userInfo, PermissionLevel.View))
                .ReturnsAsync(true);

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Vocabulary, 1, userInfo, PermissionLevel.View))
                .ReturnsAsync(true);

            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Vocabulary, 1, 1, 0, userInfo, null))
                .ReturnsAsync(permission);

            SearchService service = new(progenyContext, mediaContext, _mockAccessManagementService.Object, _mockTimelineService.Object);

            SearchRequest request = new()
            {
                ProgenyIds = [1],
                Query = "   hello   "
            };

            // Act
            SearchResponse<VocabularyItem> result = await service.SearchVocabularyItems(request, userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result.Results);
        }

        [Fact]
        public async Task SearchVocabularyItems_Should_Use_DateAdded_When_Date_Is_Null()
        {
            // Arrange
            await using ProgenyDbContext progenyContext = GetInMemoryProgenyDbContext("SearchVocabularyItems_NullDate");
            await using MediaDbContext mediaContext = GetInMemoryMediaDbContext("SearchVocabularyItems_NullDate");

            VocabularyItem vocabularyItem = new()
            {
                WordId = 1,
                ProgenyId = 1,
                Word = "Hello",
                Description = "A greeting",
                Language = "English",
                SoundsLike = "heh-loh",
                Date = null,
                DateAdded = DateTime.UtcNow.AddDays(-5)
            };

            progenyContext.VocabularyDb.Add(vocabularyItem);
            await progenyContext.SaveChangesAsync(TestContext.Current.CancellationToken);

            UserInfo userInfo = CreateTestUserInfo();
            TimelineItemPermission permission = new() { PermissionLevel = PermissionLevel.View };

            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(1, userInfo, PermissionLevel.View))
                .ReturnsAsync(true);

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Vocabulary, 1, userInfo, PermissionLevel.View))
                .ReturnsAsync(true);

            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Vocabulary, 1, 1, 0, userInfo, null))
                .ReturnsAsync(permission);

            SearchService service = new(progenyContext, mediaContext, _mockAccessManagementService.Object, _mockTimelineService.Object);

            SearchRequest request = new()
            {
                ProgenyIds = [1],
                Query = "hello"
            };

            // Act
            SearchResponse<VocabularyItem> result = await service.SearchVocabularyItems(request, userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result.Results);
        }

        [Fact]
        public async Task SearchVocabularyItems_Should_Return_Empty_When_No_Match()
        {
            // Arrange
            await using ProgenyDbContext progenyContext = GetInMemoryProgenyDbContext("SearchVocabularyItems_NoMatch");
            await using MediaDbContext mediaContext = GetInMemoryMediaDbContext("SearchVocabularyItems_NoMatch");

            VocabularyItem vocabularyItem = new()
            {
                WordId = 1,
                ProgenyId = 1,
                Word = "Hello",
                Description = "A greeting",
                Language = "English",
                SoundsLike = "heh-loh",
                Date = DateTime.UtcNow,
                DateAdded = DateTime.UtcNow
            };

            progenyContext.VocabularyDb.Add(vocabularyItem);
            await progenyContext.SaveChangesAsync(TestContext.Current.CancellationToken);

            UserInfo userInfo = CreateTestUserInfo();

            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(1, userInfo, PermissionLevel.View))
                .ReturnsAsync(true);

            SearchService service = new(progenyContext, mediaContext, _mockAccessManagementService.Object, _mockTimelineService.Object);

            SearchRequest request = new()
            {
                ProgenyIds = [1],
                Query = "xyz123"
            };

            // Act
            SearchResponse<VocabularyItem> result = await service.SearchVocabularyItems(request, userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result.Results);
            Assert.Equal(0, result.TotalCount);
        }

        [Fact]
        public async Task SearchVocabularyItems_Should_Return_Correct_TotalCount_And_RemainingItems()
        {
            // Arrange
            await using ProgenyDbContext progenyContext = GetInMemoryProgenyDbContext("SearchVocabularyItems_Counts");
            await using MediaDbContext mediaContext = GetInMemoryMediaDbContext("SearchVocabularyItems_Counts");

            for (int i = 1; i <= 10; i++)
            {
                progenyContext.VocabularyDb.Add(new VocabularyItem
                {
                    WordId = i,
                    ProgenyId = 1,
                    Word = $"TestWord{i}",
                    Description = "Test description",
                    Language = "English",
                    SoundsLike = $"test{i}",
                    Date = DateTime.UtcNow.AddDays(-i),
                    DateAdded = DateTime.UtcNow.AddDays(-i)
                });
            }
            await progenyContext.SaveChangesAsync(TestContext.Current.CancellationToken);

            UserInfo userInfo = CreateTestUserInfo();
            TimelineItemPermission permission = new() { PermissionLevel = PermissionLevel.View };

            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(1, userInfo, PermissionLevel.View))
                .ReturnsAsync(true);

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Vocabulary, It.IsAny<int>(), userInfo, PermissionLevel.View))
                .ReturnsAsync(true);

            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Vocabulary, It.IsAny<int>(), 1, 0, userInfo, null))
                .ReturnsAsync(permission);

            SearchService service = new(progenyContext, mediaContext, _mockAccessManagementService.Object, _mockTimelineService.Object);

            SearchRequest request = new()
            {
                ProgenyIds = [1],
                Query = "test",
                Skip = 2,
                NumberOfItems = 3
            };

            // Act
            SearchResponse<VocabularyItem> result = await service.SearchVocabularyItems(request, userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(3, result.Results.Count);
            Assert.Equal(10, result.TotalCount);
            Assert.Equal(5, result.RemainingItems);
            Assert.Equal(1, result.PageNumber);
        }

        [Fact]
        public async Task SearchVocabularyItems_Should_Calculate_PageNumber_Correctly()
        {
            // Arrange
            await using ProgenyDbContext progenyContext = GetInMemoryProgenyDbContext("SearchVocabularyItems_PageNumber");
            await using MediaDbContext mediaContext = GetInMemoryMediaDbContext("SearchVocabularyItems_PageNumber");

            for (int i = 1; i <= 10; i++)
            {
                progenyContext.VocabularyDb.Add(new VocabularyItem
                {
                    WordId = i,
                    ProgenyId = 1,
                    Word = $"TestWord{i}",
                    Description = "Test description",
                    Language = "English",
                    SoundsLike = $"test{i}",
                    Date = DateTime.UtcNow.AddDays(-i),
                    DateAdded = DateTime.UtcNow.AddDays(-i)
                });
            }
            await progenyContext.SaveChangesAsync(TestContext.Current.CancellationToken);

            UserInfo userInfo = CreateTestUserInfo();
            TimelineItemPermission permission = new() { PermissionLevel = PermissionLevel.View };

            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(1, userInfo, PermissionLevel.View))
                .ReturnsAsync(true);

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Vocabulary, It.IsAny<int>(), userInfo, PermissionLevel.View))
                .ReturnsAsync(true);

            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Vocabulary, It.IsAny<int>(), 1, 0, userInfo, null))
                .ReturnsAsync(permission);

            SearchService service = new(progenyContext, mediaContext, _mockAccessManagementService.Object, _mockTimelineService.Object);

            SearchRequest request = new()
            {
                ProgenyIds = [1],
                Query = "test",
                Skip = 6,
                NumberOfItems = 3
            };

            // Act
            SearchResponse<VocabularyItem> result = await service.SearchVocabularyItems(request, userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(3, result.PageNumber); // Skip 6 / Take 3 = Page 3
        }

        #endregion
    }
}