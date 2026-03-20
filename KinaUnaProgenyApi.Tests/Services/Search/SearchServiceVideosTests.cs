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
    public class SearchServiceVideosTests
    {
        private readonly Mock<IAccessManagementService> _mockAccessManagementService;
        private readonly Mock<ITimelineService> _mockTimelineService;

        public SearchServiceVideosTests()
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

        private static Video CreateTestVideo(
            int videoId,
            int progenyId,
            string tags = "",
            string location = "",
            DateTime? videoTime = null)
        {
            return new Video
            {
                VideoId = videoId,
                ProgenyId = progenyId,
                Tags = tags,
                Location = location,
                VideoTime = videoTime ?? DateTime.UtcNow.AddDays(-videoId),
                CreatedTime = DateTime.UtcNow.AddDays(-videoId),
                VideoLink = $"https://example.com/video{videoId}.mp4",
                ThumbLink = $"https://example.com/thumb{videoId}.jpg"
            };
        }

        private static SearchRequest CreateTestSearchRequest(
            string query,
            List<int>? progenyIds = null,
            int skip = 0,
            int numberOfItems = 10,
            int sort = 0)
        {
            return new SearchRequest
            {
                Query = query,
                ProgenyIds = progenyIds ?? [1],
                FamilyIds = [],
                Skip = skip,
                NumberOfItems = numberOfItems,
                Sort = sort
            };
        }

        #region Basic Search Tests

        [Fact]
        public async Task SearchVideos_Should_Return_Empty_Response_When_UserInfo_Is_Null()
        {
            // Arrange
            await using ProgenyDbContext progenyContext = GetInMemoryProgenyDbContext("SearchVideos_NullUser");
            await using MediaDbContext mediaContext = GetInMemoryMediaDbContext("SearchVideos_NullUser");

            SearchService service = new(
                progenyContext,
                mediaContext,
                _mockAccessManagementService.Object,
                _mockTimelineService.Object);

            SearchRequest request = CreateTestSearchRequest("test");

            // Act
            SearchResponse<Video> result = await service.SearchVideos(request, null);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.SearchRequest);
            Assert.NotNull(result.Results);
            Assert.Empty(result.Results);
        }

        [Fact]
        public async Task SearchVideos_Should_Return_Empty_Response_When_Query_Is_Empty()
        {
            // Arrange
            await using ProgenyDbContext progenyContext = GetInMemoryProgenyDbContext("SearchVideos_EmptyQuery");
            await using MediaDbContext mediaContext = GetInMemoryMediaDbContext("SearchVideos_EmptyQuery");

            UserInfo userInfo = CreateTestUserInfo();
            SearchService service = new(
                progenyContext,
                mediaContext,
                _mockAccessManagementService.Object,
                _mockTimelineService.Object);

            SearchRequest request = CreateTestSearchRequest("");

            // Act
            SearchResponse<Video> result = await service.SearchVideos(request, userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.SearchRequest);
            Assert.NotNull(result.Results);
            Assert.Empty(result.Results);
        }

        [Fact]
        public async Task SearchVideos_Should_Return_Empty_Response_When_Query_Is_Whitespace()
        {
            // Arrange
            await using ProgenyDbContext progenyContext = GetInMemoryProgenyDbContext("SearchVideos_WhitespaceQuery");
            await using MediaDbContext mediaContext = GetInMemoryMediaDbContext("SearchVideos_WhitespaceQuery");

            UserInfo userInfo = CreateTestUserInfo();
            SearchService service = new(
                progenyContext,
                mediaContext,
                _mockAccessManagementService.Object,
                _mockTimelineService.Object);

            SearchRequest request = CreateTestSearchRequest("   ");

            // Act
            SearchResponse<Video> result = await service.SearchVideos(request, userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.SearchRequest);
            Assert.NotNull(result.Results);
            Assert.Empty(result.Results);
        }

        #endregion

        #region Permission Tests

        [Fact]
        public async Task SearchVideos_Should_Return_Empty_When_User_Has_No_Progeny_Permission()
        {
            // Arrange
            await using ProgenyDbContext progenyContext = GetInMemoryProgenyDbContext("SearchVideos_NoProgenyPermission");
            await using MediaDbContext mediaContext = GetInMemoryMediaDbContext("SearchVideos_NoProgenyPermission");

            Video video = CreateTestVideo(1, 1, "birthday party", "Home");
            mediaContext.VideoDb.Add(video);
            await mediaContext.SaveChangesAsync();

            UserInfo userInfo = CreateTestUserInfo();

            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(1, userInfo, PermissionLevel.View))
                .ReturnsAsync(false);

            SearchService service = new(
                progenyContext,
                mediaContext,
                _mockAccessManagementService.Object,
                _mockTimelineService.Object);

            SearchRequest request = CreateTestSearchRequest("birthday");

            // Act
            SearchResponse<Video> result = await service.SearchVideos(request, userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Results);
            Assert.Empty(result.Results);
        }

        [Fact]
        public async Task SearchVideos_Should_Return_Empty_When_User_Has_No_Item_Permission()
        {
            // Arrange
            await using ProgenyDbContext progenyContext = GetInMemoryProgenyDbContext("SearchVideos_NoItemPermission");
            await using MediaDbContext mediaContext = GetInMemoryMediaDbContext("SearchVideos_NoItemPermission");

            Video video = CreateTestVideo(1, 1, "birthday party", "Home");
            mediaContext.VideoDb.Add(video);
            await mediaContext.SaveChangesAsync(TestContext.Current.CancellationToken);

            UserInfo userInfo = CreateTestUserInfo();

            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(1, userInfo, PermissionLevel.View))
                .ReturnsAsync(true);

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Video, 1, userInfo, PermissionLevel.View))
                .ReturnsAsync(false);

            SearchService service = new(
                progenyContext,
                mediaContext,
                _mockAccessManagementService.Object,
                _mockTimelineService.Object);

            SearchRequest request = CreateTestSearchRequest("birthday");

            // Act
            SearchResponse<Video> result = await service.SearchVideos(request, userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Results);
            Assert.Empty(result.Results);
        }

        #endregion

        #region Search Field Tests

        [Fact]
        public async Task SearchVideos_Should_Find_Match_In_Tags()
        {
            // Arrange
            await using ProgenyDbContext progenyContext = GetInMemoryProgenyDbContext("SearchVideos_MatchTags");
            await using MediaDbContext mediaContext = GetInMemoryMediaDbContext("SearchVideos_MatchTags");

            Video video = CreateTestVideo(1, 1, "birthday party celebration", "Home");
            mediaContext.VideoDb.Add(video);
            await mediaContext.SaveChangesAsync();

            UserInfo userInfo = CreateTestUserInfo();

            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(1, userInfo, PermissionLevel.View))
                .ReturnsAsync(true);

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Video, 1, userInfo, PermissionLevel.View))
                .ReturnsAsync(true);

            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Video, 1, 1, 0, userInfo))
                .ReturnsAsync(new TimelineItemPermission { PermissionLevel = PermissionLevel.View });

            SearchService service = new(
                progenyContext,
                mediaContext,
                _mockAccessManagementService.Object,
                _mockTimelineService.Object);

            SearchRequest request = CreateTestSearchRequest("birthday");

            // Act
            SearchResponse<Video> result = await service.SearchVideos(request, userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Results);
            Assert.Single(result.Results);
            Assert.Contains("birthday", result.Results[0].Tags);
        }

        [Fact]
        public async Task SearchVideos_Should_Find_Match_In_Location()
        {
            // Arrange
            await using ProgenyDbContext progenyContext = GetInMemoryProgenyDbContext("SearchVideos_MatchLocation");
            await using MediaDbContext mediaContext = GetInMemoryMediaDbContext("SearchVideos_MatchLocation");

            Video video = CreateTestVideo(1, 1, "family", "Central Park");
            mediaContext.VideoDb.Add(video);
            await mediaContext.SaveChangesAsync();

            UserInfo userInfo = CreateTestUserInfo();

            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(1, userInfo, PermissionLevel.View))
                .ReturnsAsync(true);

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Video, 1, userInfo, PermissionLevel.View))
                .ReturnsAsync(true);

            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Video, 1, 1, 0, userInfo))
                .ReturnsAsync(new TimelineItemPermission { PermissionLevel = PermissionLevel.View });

            SearchService service = new(
                progenyContext,
                mediaContext,
                _mockAccessManagementService.Object,
                _mockTimelineService.Object);

            SearchRequest request = CreateTestSearchRequest("central park");

            // Act
            SearchResponse<Video> result = await service.SearchVideos(request, userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Results);
            Assert.Single(result.Results);
            Assert.Contains("Central Park", result.Results[0].Location);
        }

        #endregion

        #region Null Field Handling Tests

        [Fact]
        public async Task SearchVideos_Should_Handle_Null_Tags()
        {
            // Arrange
            await using ProgenyDbContext progenyContext = GetInMemoryProgenyDbContext("SearchVideos_NullTags");
            await using MediaDbContext mediaContext = GetInMemoryMediaDbContext("SearchVideos_NullTags");

            Video video = new()
            {
                VideoId = 1,
                ProgenyId = 1,
                Tags = null,
                Location = "Home",
                VideoTime = DateTime.UtcNow,
                CreatedTime = DateTime.UtcNow
            };
            mediaContext.VideoDb.Add(video);
            await mediaContext.SaveChangesAsync();

            UserInfo userInfo = CreateTestUserInfo();

            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(1, userInfo, PermissionLevel.View))
                .ReturnsAsync(true);

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Video, 1, userInfo, PermissionLevel.View))
                .ReturnsAsync(true);

            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Video, 1, 1, 0, userInfo))
                .ReturnsAsync(new TimelineItemPermission { PermissionLevel = PermissionLevel.View });

            SearchService service = new(
                progenyContext,
                mediaContext,
                _mockAccessManagementService.Object,
                _mockTimelineService.Object);

            SearchRequest request = CreateTestSearchRequest("home");

            // Act
            SearchResponse<Video> result = await service.SearchVideos(request, userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Results);
            Assert.Single(result.Results);
        }

        [Fact]
        public async Task SearchVideos_Should_Handle_Null_Location()
        {
            // Arrange
            await using ProgenyDbContext progenyContext = GetInMemoryProgenyDbContext("SearchVideos_NullLocation");
            await using MediaDbContext mediaContext = GetInMemoryMediaDbContext("SearchVideos_NullLocation");

            Video video = new()
            {
                VideoId = 1,
                ProgenyId = 1,
                Tags = "birthday",
                Location = null,
                VideoTime = DateTime.UtcNow,
                CreatedTime = DateTime.UtcNow
            };
            mediaContext.VideoDb.Add(video);
            await mediaContext.SaveChangesAsync();

            UserInfo userInfo = CreateTestUserInfo();

            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(1, userInfo, PermissionLevel.View))
                .ReturnsAsync(true);

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Video, 1, userInfo, PermissionLevel.View))
                .ReturnsAsync(true);

            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Video, 1, 1, 0, userInfo))
                .ReturnsAsync(new TimelineItemPermission { PermissionLevel = PermissionLevel.View });

            SearchService service = new(
                progenyContext,
                mediaContext,
                _mockAccessManagementService.Object,
                _mockTimelineService.Object);

            SearchRequest request = CreateTestSearchRequest("birthday");

            // Act
            SearchResponse<Video> result = await service.SearchVideos(request, userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Results);
            Assert.Single(result.Results);
        }

        [Fact]
        public async Task SearchVideos_Should_Handle_Both_Null_Tags_And_Location()
        {
            // Arrange
            await using ProgenyDbContext progenyContext = GetInMemoryProgenyDbContext("SearchVideos_BothNull");
            await using MediaDbContext mediaContext = GetInMemoryMediaDbContext("SearchVideos_BothNull");

            Video video = new()
            {
                VideoId = 1,
                ProgenyId = 1,
                Tags = null,
                Location = null,
                VideoTime = DateTime.UtcNow,
                CreatedTime = DateTime.UtcNow
            };
            mediaContext.VideoDb.Add(video);
            await mediaContext.SaveChangesAsync();

            UserInfo userInfo = CreateTestUserInfo();

            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(1, userInfo, PermissionLevel.View))
                .ReturnsAsync(true);

            SearchService service = new(
                progenyContext,
                mediaContext,
                _mockAccessManagementService.Object,
                _mockTimelineService.Object);

            SearchRequest request = CreateTestSearchRequest("test");

            // Act
            SearchResponse<Video> result = await service.SearchVideos(request, userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Results);
            Assert.Empty(result.Results);
        }

        #endregion

        #region Case Insensitivity Tests

        [Fact]
        public async Task SearchVideos_Should_Be_Case_Insensitive()
        {
            // Arrange
            await using ProgenyDbContext progenyContext = GetInMemoryProgenyDbContext("SearchVideos_CaseInsensitive");
            await using MediaDbContext mediaContext = GetInMemoryMediaDbContext("SearchVideos_CaseInsensitive");

            Video video = CreateTestVideo(1, 1, "Birthday Party", "Central Park");
            mediaContext.VideoDb.Add(video);
            await mediaContext.SaveChangesAsync();

            UserInfo userInfo = CreateTestUserInfo();

            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(1, userInfo, PermissionLevel.View))
                .ReturnsAsync(true);

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Video, 1, userInfo, PermissionLevel.View))
                .ReturnsAsync(true);

            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Video, 1, 1, 0, userInfo))
                .ReturnsAsync(new TimelineItemPermission { PermissionLevel = PermissionLevel.View });

            SearchService service = new(
                progenyContext,
                mediaContext,
                _mockAccessManagementService.Object,
                _mockTimelineService.Object);

            SearchRequest request = CreateTestSearchRequest("birthday party");

            // Act
            SearchResponse<Video> result = await service.SearchVideos(request, userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Results);
            Assert.Single(result.Results);
        }

        [Fact]
        public async Task SearchVideos_Should_Be_Case_Insensitive_For_Location()
        {
            // Arrange
            await using ProgenyDbContext progenyContext = GetInMemoryProgenyDbContext("SearchVideos_CaseInsensitiveLocation");
            await using MediaDbContext mediaContext = GetInMemoryMediaDbContext("SearchVideos_CaseInsensitiveLocation");

            Video video = CreateTestVideo(1, 1, "family", "CENTRAL PARK");
            mediaContext.VideoDb.Add(video);
            await mediaContext.SaveChangesAsync();

            UserInfo userInfo = CreateTestUserInfo();

            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(1, userInfo, PermissionLevel.View))
                .ReturnsAsync(true);

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Video, 1, userInfo, PermissionLevel.View))
                .ReturnsAsync(true);

            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Video, 1, 1, 0, userInfo))
                .ReturnsAsync(new TimelineItemPermission { PermissionLevel = PermissionLevel.View });

            SearchService service = new(
                progenyContext,
                mediaContext,
                _mockAccessManagementService.Object,
                _mockTimelineService.Object);

            SearchRequest request = CreateTestSearchRequest("central park");

            // Act
            SearchResponse<Video> result = await service.SearchVideos(request, userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Results);
            Assert.Single(result.Results);
        }

        #endregion

        #region Multiple Results Tests

        [Fact]
        public async Task SearchVideos_Should_Return_Multiple_Matching_Items()
        {
            // Arrange
            await using ProgenyDbContext progenyContext = GetInMemoryProgenyDbContext("SearchVideos_MultipleResults");
            await using MediaDbContext mediaContext = GetInMemoryMediaDbContext("SearchVideos_MultipleResults");

            List<Video> videos =
            [
                CreateTestVideo(1, 1, "birthday party", "Home"),
                CreateTestVideo(2, 1, "birthday celebration", "Park"),
                CreateTestVideo(3, 1, "vacation", "Beach")
            ];

            mediaContext.VideoDb.AddRange(videos);
            await mediaContext.SaveChangesAsync();

            UserInfo userInfo = CreateTestUserInfo();

            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(1, userInfo, PermissionLevel.View))
                .ReturnsAsync(true);

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Video, It.IsAny<int>(), userInfo, PermissionLevel.View))
                .ReturnsAsync(true);

            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Video, It.IsAny<int>(), 1, 0, userInfo))
                .ReturnsAsync(new TimelineItemPermission { PermissionLevel = PermissionLevel.View });

            SearchService service = new(
                progenyContext,
                mediaContext,
                _mockAccessManagementService.Object,
                _mockTimelineService.Object);

            SearchRequest request = CreateTestSearchRequest("birthday");

            // Act
            SearchResponse<Video> result = await service.SearchVideos(request, userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Results);
            Assert.Equal(2, result.Results.Count);
            Assert.All(result.Results, v => Assert.Contains("birthday", v.Tags));
        }

        #endregion

        #region Multiple Progenies Tests

        [Fact]
        public async Task SearchVideos_Should_Search_Across_Multiple_Progenies()
        {
            // Arrange
            await using ProgenyDbContext progenyContext = GetInMemoryProgenyDbContext("SearchVideos_MultipleProgenies");
            await using MediaDbContext mediaContext = GetInMemoryMediaDbContext("SearchVideos_MultipleProgenies");

            List<Video> videos =
            [
                CreateTestVideo(1, 1, "birthday party", "Home"),
                CreateTestVideo(2, 2, "birthday celebration", "Park")
            ];

            mediaContext.VideoDb.AddRange(videos);
            await mediaContext.SaveChangesAsync();

            UserInfo userInfo = CreateTestUserInfo();

            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(It.IsAny<int>(), userInfo, PermissionLevel.View))
                .ReturnsAsync(true);

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Video, It.IsAny<int>(), userInfo, PermissionLevel.View))
                .ReturnsAsync(true);

            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Video, It.IsAny<int>(), It.IsAny<int>(), 0, userInfo))
                .ReturnsAsync(new TimelineItemPermission { PermissionLevel = PermissionLevel.View });

            SearchService service = new(
                progenyContext,
                mediaContext,
                _mockAccessManagementService.Object,
                _mockTimelineService.Object);

            SearchRequest request = CreateTestSearchRequest("birthday", [1, 2]);

            // Act
            SearchResponse<Video> result = await service.SearchVideos(request, userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Results);
            Assert.Equal(2, result.Results.Count);
        }

        [Fact]
        public async Task SearchVideos_Should_Skip_Progenies_Without_Permission()
        {
            // Arrange
            await using ProgenyDbContext progenyContext = GetInMemoryProgenyDbContext("SearchVideos_SkipNoPermission");
            await using MediaDbContext mediaContext = GetInMemoryMediaDbContext("SearchVideos_SkipNoPermission");

            List<Video> videos =
            [
                CreateTestVideo(1, 1, "birthday party", "Home"),
                CreateTestVideo(2, 2, "birthday celebration", "Park")
            ];

            mediaContext.VideoDb.AddRange(videos);
            await mediaContext.SaveChangesAsync();

            UserInfo userInfo = CreateTestUserInfo();

            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(1, userInfo, PermissionLevel.View))
                .ReturnsAsync(true);

            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(2, userInfo, PermissionLevel.View))
                .ReturnsAsync(false);

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Video, 1, userInfo, PermissionLevel.View))
                .ReturnsAsync(true);

            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Video, 1, 1, 0, userInfo))
                .ReturnsAsync(new TimelineItemPermission { PermissionLevel = PermissionLevel.View });

            SearchService service = new(
                progenyContext,
                mediaContext,
                _mockAccessManagementService.Object,
                _mockTimelineService.Object);

            SearchRequest request = CreateTestSearchRequest("birthday", [1, 2]);

            // Act
            SearchResponse<Video> result = await service.SearchVideos(request, userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Results);
            Assert.Single(result.Results);
            Assert.Equal(1, result.Results[0].ProgenyId);
        }

        #endregion

        #region Pagination and Sorting Tests

        [Fact]
        public async Task SearchVideos_Should_Sort_By_Date_Descending_When_Sort_Is_0()
        {
            // Arrange
            await using ProgenyDbContext progenyContext = GetInMemoryProgenyDbContext("SearchVideos_SortDescending");
            await using MediaDbContext mediaContext = GetInMemoryMediaDbContext("SearchVideos_SortDescending");

            List<Video> videos =
            [
                new() { VideoId = 1, ProgenyId = 1, Tags = "family", VideoTime = DateTime.UtcNow.AddDays(-10), CreatedTime = DateTime.UtcNow.AddDays(-10) },
                new() { VideoId = 2, ProgenyId = 1, Tags = "family", VideoTime = DateTime.UtcNow.AddDays(-5), CreatedTime = DateTime.UtcNow.AddDays(-5) },
                new() { VideoId = 3, ProgenyId = 1, Tags = "family", VideoTime = DateTime.UtcNow.AddDays(-1), CreatedTime = DateTime.UtcNow.AddDays(-1) }
            ];

            mediaContext.VideoDb.AddRange(videos);
            await mediaContext.SaveChangesAsync();

            UserInfo userInfo = CreateTestUserInfo();

            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(1, userInfo, PermissionLevel.View))
                .ReturnsAsync(true);

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Video, It.IsAny<int>(), userInfo, PermissionLevel.View))
                .ReturnsAsync(true);

            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Video, It.IsAny<int>(), 1, 0, userInfo))
                .ReturnsAsync(new TimelineItemPermission { PermissionLevel = PermissionLevel.View });

            SearchService service = new(
                progenyContext,
                mediaContext,
                _mockAccessManagementService.Object,
                _mockTimelineService.Object);

            SearchRequest request = CreateTestSearchRequest("family", [1]);

            // Act
            SearchResponse<Video> result = await service.SearchVideos(request, userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Results);
            Assert.Equal(3, result.Results.Count);
            Assert.Equal(3, result.Results[0].VideoId);
            Assert.Equal(1, result.Results[2].VideoId);
        }

        [Fact]
        public async Task SearchVideos_Should_Sort_By_Date_Ascending_When_Sort_Is_1()
        {
            // Arrange
            await using ProgenyDbContext progenyContext = GetInMemoryProgenyDbContext("SearchVideos_SortAscending");
            await using MediaDbContext mediaContext = GetInMemoryMediaDbContext("SearchVideos_SortAscending");

            List<Video> videos =
            [
                new() { VideoId = 1, ProgenyId = 1, Tags = "family", VideoTime = DateTime.UtcNow.AddDays(-10), CreatedTime = DateTime.UtcNow.AddDays(-10) },
                new() { VideoId = 2, ProgenyId = 1, Tags = "family", VideoTime = DateTime.UtcNow.AddDays(-5), CreatedTime = DateTime.UtcNow.AddDays(-5) },
                new() { VideoId = 3, ProgenyId = 1, Tags = "family", VideoTime = DateTime.UtcNow.AddDays(-1), CreatedTime = DateTime.UtcNow.AddDays(-1) }
            ];

            mediaContext.VideoDb.AddRange(videos);
            await mediaContext.SaveChangesAsync();

            UserInfo userInfo = CreateTestUserInfo();

            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(1, userInfo, PermissionLevel.View))
                .ReturnsAsync(true);

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Video, It.IsAny<int>(), userInfo, PermissionLevel.View))
                .ReturnsAsync(true);

            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Video, It.IsAny<int>(), 1, 0, userInfo))
                .ReturnsAsync(new TimelineItemPermission { PermissionLevel = PermissionLevel.View });

            SearchService service = new(
                progenyContext,
                mediaContext,
                _mockAccessManagementService.Object,
                _mockTimelineService.Object);

            SearchRequest request = CreateTestSearchRequest("family", [1], 0, 10, 1);

            // Act
            SearchResponse<Video> result = await service.SearchVideos(request, userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Results);
            Assert.Equal(3, result.Results.Count);
            Assert.Equal(1, result.Results[0].VideoId);
            Assert.Equal(3, result.Results[2].VideoId);
        }

        [Fact]
        public async Task SearchVideos_Should_Use_CreatedTime_When_VideoTime_Is_Null()
        {
            // Arrange
            await using ProgenyDbContext progenyContext = GetInMemoryProgenyDbContext("SearchVideos_NullVideoTime");
            await using MediaDbContext mediaContext = GetInMemoryMediaDbContext("SearchVideos_NullVideoTime");

            List<Video> videos =
            [
                new() { VideoId = 1, ProgenyId = 1, Tags = "family", VideoTime = null, CreatedTime = DateTime.UtcNow.AddDays(-10) },
                new() { VideoId = 2, ProgenyId = 1, Tags = "family", VideoTime = DateTime.UtcNow.AddDays(-5), CreatedTime = DateTime.UtcNow.AddDays(-5) },
                new() { VideoId = 3, ProgenyId = 1, Tags = "family", VideoTime = null, CreatedTime = DateTime.UtcNow.AddDays(-1) }
            ];

            mediaContext.VideoDb.AddRange(videos);
            await mediaContext.SaveChangesAsync();

            UserInfo userInfo = CreateTestUserInfo();

            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(1, userInfo, PermissionLevel.View))
                .ReturnsAsync(true);

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Video, It.IsAny<int>(), userInfo, PermissionLevel.View))
                .ReturnsAsync(true);

            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Video, It.IsAny<int>(), 1, 0, userInfo))
                .ReturnsAsync(new TimelineItemPermission { PermissionLevel = PermissionLevel.View });

            SearchService service = new(
                progenyContext,
                mediaContext,
                _mockAccessManagementService.Object,
                _mockTimelineService.Object);

            SearchRequest request = CreateTestSearchRequest("family", [1]);

            // Act
            SearchResponse<Video> result = await service.SearchVideos(request, userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Results);
            Assert.Equal(3, result.Results.Count);
            Assert.Equal(3, result.Results[0].VideoId);
            Assert.Equal(1, result.Results[2].VideoId);
        }

        [Fact]
        public async Task SearchVideos_Should_Apply_Skip_And_Take()
        {
            // Arrange
            await using ProgenyDbContext progenyContext = GetInMemoryProgenyDbContext("SearchVideos_Pagination");
            await using MediaDbContext mediaContext = GetInMemoryMediaDbContext("SearchVideos_Pagination");

            List<Video> videos = [];
            for (int i = 1; i <= 10; i++)
            {
                videos.Add(new Video
                {
                    VideoId = i,
                    ProgenyId = 1,
                    Tags = "family",
                    VideoTime = DateTime.UtcNow.AddDays(-i),
                    CreatedTime = DateTime.UtcNow.AddDays(-i)
                });
            }

            mediaContext.VideoDb.AddRange(videos);
            await mediaContext.SaveChangesAsync();

            UserInfo userInfo = CreateTestUserInfo();

            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(1, userInfo, PermissionLevel.View))
                .ReturnsAsync(true);

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Video, It.IsAny<int>(), userInfo, PermissionLevel.View))
                .ReturnsAsync(true);

            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Video, It.IsAny<int>(), 1, 0, userInfo))
                .ReturnsAsync(new TimelineItemPermission { PermissionLevel = PermissionLevel.View });

            SearchService service = new(
                progenyContext,
                mediaContext,
                _mockAccessManagementService.Object,
                _mockTimelineService.Object);

            SearchRequest request = CreateTestSearchRequest("family", [1], 2, 3);

            // Act
            SearchResponse<Video> result = await service.SearchVideos(request, userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Results);
            Assert.Equal(3, result.Results.Count);
            Assert.Equal(10, result.TotalCount);
        }

        #endregion

        #region Response Properties Tests

        [Fact]
        public async Task SearchVideos_Should_Set_ItemPermission_For_Each_Result()
        {
            // Arrange
            await using ProgenyDbContext progenyContext = GetInMemoryProgenyDbContext("SearchVideos_ItemPermission");
            await using MediaDbContext mediaContext = GetInMemoryMediaDbContext("SearchVideos_ItemPermission");

            Video video = CreateTestVideo(1, 1, "birthday", "Home");
            mediaContext.VideoDb.Add(video);
            await mediaContext.SaveChangesAsync();

            UserInfo userInfo = CreateTestUserInfo();
            TimelineItemPermission permission = new() { PermissionLevel = PermissionLevel.Edit };

            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(1, userInfo, PermissionLevel.View))
                .ReturnsAsync(true);

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Video, 1, userInfo, PermissionLevel.View))
                .ReturnsAsync(true);

            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Video, 1, 1, 0, userInfo))
                .ReturnsAsync(permission);

            SearchService service = new(
                progenyContext,
                mediaContext,
                _mockAccessManagementService.Object,
                _mockTimelineService.Object);

            SearchRequest request = CreateTestSearchRequest("birthday");

            // Act
            SearchResponse<Video> result = await service.SearchVideos(request, userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Results);
            Assert.Single(result.Results);
            Assert.NotNull(result.Results[0].ItemPermission);
            Assert.Equal(PermissionLevel.Edit, result.Results[0].ItemPermission.PermissionLevel);
        }

        [Fact]
        public async Task SearchVideos_Should_Set_TotalCount_Correctly()
        {
            // Arrange
            await using ProgenyDbContext progenyContext = GetInMemoryProgenyDbContext("SearchVideos_TotalCount");
            await using MediaDbContext mediaContext = GetInMemoryMediaDbContext("SearchVideos_TotalCount");

            List<Video> videos =
            [
                CreateTestVideo(1, 1, "family A"),
                CreateTestVideo(2, 1, "family B"),
                CreateTestVideo(3, 1, "family C")
            ];

            mediaContext.VideoDb.AddRange(videos);
            await mediaContext.SaveChangesAsync();

            UserInfo userInfo = CreateTestUserInfo();

            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(1, userInfo, PermissionLevel.View))
                .ReturnsAsync(true);

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Video, It.IsAny<int>(), userInfo, PermissionLevel.View))
                .ReturnsAsync(true);

            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Video, It.IsAny<int>(), 1, 0, userInfo))
                .ReturnsAsync(new TimelineItemPermission { PermissionLevel = PermissionLevel.View });

            SearchService service = new(
                progenyContext,
                mediaContext,
                _mockAccessManagementService.Object,
                _mockTimelineService.Object);

            SearchRequest request = CreateTestSearchRequest("family", [1], 0, 2);

            // Act
            SearchResponse<Video> result = await service.SearchVideos(request, userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(3, result.TotalCount);
            Assert.Equal(2, result.Results.Count);
        }

        [Fact]
        public async Task SearchVideos_Should_Set_PageNumber_Correctly()
        {
            // Arrange
            await using ProgenyDbContext progenyContext = GetInMemoryProgenyDbContext("SearchVideos_PageNumber");
            await using MediaDbContext mediaContext = GetInMemoryMediaDbContext("SearchVideos_PageNumber");

            List<Video> videos = [];
            for (int i = 1; i <= 10; i++)
            {
                videos.Add(CreateTestVideo(i, 1, $"family {i}"));
            }

            mediaContext.VideoDb.AddRange(videos);
            await mediaContext.SaveChangesAsync();

            UserInfo userInfo = CreateTestUserInfo();

            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(1, userInfo, PermissionLevel.View))
                .ReturnsAsync(true);

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Video, It.IsAny<int>(), userInfo, PermissionLevel.View))
                .ReturnsAsync(true);

            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Video, It.IsAny<int>(), 1, 0, userInfo))
                .ReturnsAsync(new TimelineItemPermission { PermissionLevel = PermissionLevel.View });

            SearchService service = new(
                progenyContext,
                mediaContext,
                _mockAccessManagementService.Object,
                _mockTimelineService.Object);

            SearchRequest request = CreateTestSearchRequest("family", [1], 4, 2);

            // Act
            SearchResponse<Video> result = await service.SearchVideos(request, userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(3, result.PageNumber);
        }

        [Fact]
        public async Task SearchVideos_Should_Set_RemainingItems_Correctly()
        {
            // Arrange
            await using ProgenyDbContext progenyContext = GetInMemoryProgenyDbContext("SearchVideos_RemainingItems");
            await using MediaDbContext mediaContext = GetInMemoryMediaDbContext("SearchVideos_RemainingItems");

            List<Video> videos = [];
            for (int i = 1; i <= 10; i++)
            {
                videos.Add(CreateTestVideo(i, 1, $"family {i}"));
            }

            mediaContext.VideoDb.AddRange(videos);
            await mediaContext.SaveChangesAsync();

            UserInfo userInfo = CreateTestUserInfo();

            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(1, userInfo, PermissionLevel.View))
                .ReturnsAsync(true);

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Video, It.IsAny<int>(), userInfo, PermissionLevel.View))
                .ReturnsAsync(true);

            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Video, It.IsAny<int>(), 1, 0, userInfo))
                .ReturnsAsync(new TimelineItemPermission { PermissionLevel = PermissionLevel.View });

            SearchService service = new(
                progenyContext,
                mediaContext,
                _mockAccessManagementService.Object,
                _mockTimelineService.Object);

            SearchRequest request = CreateTestSearchRequest("family", [1], 2, 3);

            // Act
            SearchResponse<Video> result = await service.SearchVideos(request, userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(5, result.RemainingItems);
        }

        #endregion

        #region Edge Cases Tests

        [Fact]
        public async Task SearchVideos_Should_Handle_Empty_ProgenyIds_List()
        {
            // Arrange
            await using ProgenyDbContext progenyContext = GetInMemoryProgenyDbContext("SearchVideos_EmptyProgenyIds");
            await using MediaDbContext mediaContext = GetInMemoryMediaDbContext("SearchVideos_EmptyProgenyIds");

            Video video = CreateTestVideo(1, 1, "birthday", "Home");
            mediaContext.VideoDb.Add(video);
            await mediaContext.SaveChangesAsync();

            UserInfo userInfo = CreateTestUserInfo();

            SearchService service = new(
                progenyContext,
                mediaContext,
                _mockAccessManagementService.Object,
                _mockTimelineService.Object);

            SearchRequest request = CreateTestSearchRequest("birthday", []);

            // Act
            SearchResponse<Video> result = await service.SearchVideos(request, userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Results);
            Assert.Empty(result.Results);
        }

        [Fact]
        public async Task SearchVideos_Should_Trim_Query_Whitespace()
        {
            // Arrange
            await using ProgenyDbContext progenyContext = GetInMemoryProgenyDbContext("SearchVideos_TrimQuery");
            await using MediaDbContext mediaContext = GetInMemoryMediaDbContext("SearchVideos_TrimQuery");

            Video video = CreateTestVideo(1, 1, "birthday", "Home");
            mediaContext.VideoDb.Add(video);
            await mediaContext.SaveChangesAsync();

            UserInfo userInfo = CreateTestUserInfo();

            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(1, userInfo, PermissionLevel.View))
                .ReturnsAsync(true);

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Video, 1, userInfo, PermissionLevel.View))
                .ReturnsAsync(true);

            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Video, 1, 1, 0, userInfo))
                .ReturnsAsync(new TimelineItemPermission { PermissionLevel = PermissionLevel.View });

            SearchService service = new(
                progenyContext,
                mediaContext,
                _mockAccessManagementService.Object,
                _mockTimelineService.Object);

            SearchRequest request = CreateTestSearchRequest("  birthday  ");

            // Act
            SearchResponse<Video> result = await service.SearchVideos(request, userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Results);
            Assert.Single(result.Results);
        }

        [Fact]
        public async Task SearchVideos_Should_Return_No_Results_When_No_Match_Found()
        {
            // Arrange
            await using ProgenyDbContext progenyContext = GetInMemoryProgenyDbContext("SearchVideos_NoMatch");
            await using MediaDbContext mediaContext = GetInMemoryMediaDbContext("SearchVideos_NoMatch");

            Video video = CreateTestVideo(1, 1, "birthday", "Home");
            mediaContext.VideoDb.Add(video);
            await mediaContext.SaveChangesAsync();

            UserInfo userInfo = CreateTestUserInfo();

            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(1, userInfo, PermissionLevel.View))
                .ReturnsAsync(true);

            SearchService service = new(
                progenyContext,
                mediaContext,
                _mockAccessManagementService.Object,
                _mockTimelineService.Object);

            SearchRequest request = CreateTestSearchRequest("vacation");

            // Act
            SearchResponse<Video> result = await service.SearchVideos(request, userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Results);
            Assert.Empty(result.Results);
        }

        [Fact]
        public async Task SearchVideos_Should_Return_All_Items_When_NumberOfItems_Is_Zero()
        {
            // Arrange
            await using ProgenyDbContext progenyContext = GetInMemoryProgenyDbContext("SearchVideos_NoLimit");
            await using MediaDbContext mediaContext = GetInMemoryMediaDbContext("SearchVideos_NoLimit");

            List<Video> videos = [];
            for (int i = 1; i <= 15; i++)
            {
                videos.Add(CreateTestVideo(i, 1, $"family {i}"));
            }

            mediaContext.VideoDb.AddRange(videos);
            await mediaContext.SaveChangesAsync();

            UserInfo userInfo = CreateTestUserInfo();

            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(1, userInfo, PermissionLevel.View))
                .ReturnsAsync(true);

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Video, It.IsAny<int>(), userInfo, PermissionLevel.View))
                .ReturnsAsync(true);

            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Video, It.IsAny<int>(), 1, 0, userInfo))
                .ReturnsAsync(new TimelineItemPermission { PermissionLevel = PermissionLevel.View });

            SearchService service = new(
                progenyContext,
                mediaContext,
                _mockAccessManagementService.Object,
                _mockTimelineService.Object);

            SearchRequest request = CreateTestSearchRequest("family", [1], 0, 0);

            // Act
            SearchResponse<Video> result = await service.SearchVideos(request, userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Results);
            Assert.Equal(15, result.Results.Count);
        }

        #endregion
    }
}