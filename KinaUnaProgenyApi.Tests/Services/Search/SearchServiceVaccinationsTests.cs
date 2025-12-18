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
    public class SearchServiceVaccinationsTests
    {
        private readonly Mock<IAccessManagementService> _mockAccessManagementService;
        private readonly Mock<ITimelineService> _mockTimelineService;

        public SearchServiceVaccinationsTests()
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

        private static Vaccination CreateTestVaccination(
            int vaccinationId,
            int progenyId,
            string vaccinationName,
            string vaccinationDescription = "",
            string notes = "")
        {
            return new Vaccination
            {
                VaccinationId = vaccinationId,
                ProgenyId = progenyId,
                VaccinationName = vaccinationName,
                VaccinationDescription = vaccinationDescription,
                Notes = notes,
                VaccinationDate = DateTime.UtcNow.AddDays(-vaccinationId)
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
        public async Task SearchVaccinations_Should_Return_Empty_Response_When_UserInfo_Is_Null()
        {
            // Arrange
            await using ProgenyDbContext progenyContext = GetInMemoryProgenyDbContext("SearchVaccinations_NullUser");
            await using MediaDbContext mediaContext = GetInMemoryMediaDbContext("SearchVaccinations_NullUser");

            SearchService service = new(
                progenyContext,
                mediaContext,
                _mockAccessManagementService.Object,
                _mockTimelineService.Object);

            SearchRequest request = CreateTestSearchRequest("test");

            // Act
            SearchResponse<Vaccination> result = await service.SearchVaccinations(request, null);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.SearchRequest);
            Assert.NotNull(result.Results);
            Assert.Empty(result.Results);
        }

        [Fact]
        public async Task SearchVaccinations_Should_Return_Empty_Response_When_Query_Is_Empty()
        {
            // Arrange
            await using ProgenyDbContext progenyContext = GetInMemoryProgenyDbContext("SearchVaccinations_EmptyQuery");
            await using MediaDbContext mediaContext = GetInMemoryMediaDbContext("SearchVaccinations_EmptyQuery");

            UserInfo userInfo = CreateTestUserInfo();
            SearchService service = new(
                progenyContext,
                mediaContext,
                _mockAccessManagementService.Object,
                _mockTimelineService.Object);

            SearchRequest request = CreateTestSearchRequest("");

            // Act
            SearchResponse<Vaccination> result = await service.SearchVaccinations(request, userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.SearchRequest);
            Assert.NotNull(result.Results);
            Assert.Empty(result.Results);
        }

        [Fact]
        public async Task SearchVaccinations_Should_Return_Empty_Response_When_Query_Is_Whitespace()
        {
            // Arrange
            await using ProgenyDbContext progenyContext = GetInMemoryProgenyDbContext("SearchVaccinations_WhitespaceQuery");
            await using MediaDbContext mediaContext = GetInMemoryMediaDbContext("SearchVaccinations_WhitespaceQuery");

            UserInfo userInfo = CreateTestUserInfo();
            SearchService service = new(
                progenyContext,
                mediaContext,
                _mockAccessManagementService.Object,
                _mockTimelineService.Object);

            SearchRequest request = CreateTestSearchRequest("   ");

            // Act
            SearchResponse<Vaccination> result = await service.SearchVaccinations(request, userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.SearchRequest);
            Assert.NotNull(result.Results);
            Assert.Empty(result.Results);
        }

        #endregion

        #region Permission Tests

        [Fact]
        public async Task SearchVaccinations_Should_Return_Empty_When_User_Has_No_Progeny_Permission()
        {
            // Arrange
            await using ProgenyDbContext progenyContext = GetInMemoryProgenyDbContext("SearchVaccinations_NoProgenyPermission");
            await using MediaDbContext mediaContext = GetInMemoryMediaDbContext("SearchVaccinations_NoProgenyPermission");

            Vaccination vaccination = CreateTestVaccination(1, 1, "MMR Vaccine");
            progenyContext.VaccinationsDb.Add(vaccination);
            await progenyContext.SaveChangesAsync();

            UserInfo userInfo = CreateTestUserInfo();

            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(1, userInfo, PermissionLevel.View))
                .ReturnsAsync(false);

            SearchService service = new(
                progenyContext,
                mediaContext,
                _mockAccessManagementService.Object,
                _mockTimelineService.Object);

            SearchRequest request = CreateTestSearchRequest("MMR");

            // Act
            SearchResponse<Vaccination> result = await service.SearchVaccinations(request, userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Results);
            Assert.Empty(result.Results);
        }

        [Fact]
        public async Task SearchVaccinations_Should_Return_Empty_When_User_Has_No_Item_Permission()
        {
            // Arrange
            await using ProgenyDbContext progenyContext = GetInMemoryProgenyDbContext("SearchVaccinations_NoItemPermission");
            await using MediaDbContext mediaContext = GetInMemoryMediaDbContext("SearchVaccinations_NoItemPermission");

            Vaccination vaccination = CreateTestVaccination(1, 1, "MMR Vaccine");
            progenyContext.VaccinationsDb.Add(vaccination);
            await progenyContext.SaveChangesAsync();

            UserInfo userInfo = CreateTestUserInfo();

            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(1, userInfo, PermissionLevel.View))
                .ReturnsAsync(true);

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Vaccination, 1, userInfo, PermissionLevel.View))
                .ReturnsAsync(false);

            SearchService service = new(
                progenyContext,
                mediaContext,
                _mockAccessManagementService.Object,
                _mockTimelineService.Object);

            SearchRequest request = CreateTestSearchRequest("MMR");

            // Act
            SearchResponse<Vaccination> result = await service.SearchVaccinations(request, userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Results);
            Assert.Empty(result.Results);
        }

        #endregion

        #region Search Field Tests

        [Fact]
        public async Task SearchVaccinations_Should_Find_Match_In_VaccinationName()
        {
            // Arrange
            await using ProgenyDbContext progenyContext = GetInMemoryProgenyDbContext("SearchVaccinations_MatchName");
            await using MediaDbContext mediaContext = GetInMemoryMediaDbContext("SearchVaccinations_MatchName");

            Vaccination vaccination = CreateTestVaccination(1, 1, "MMR Vaccine", "Description", "Notes");
            progenyContext.VaccinationsDb.Add(vaccination);
            await progenyContext.SaveChangesAsync();

            UserInfo userInfo = CreateTestUserInfo();

            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(1, userInfo, PermissionLevel.View))
                .ReturnsAsync(true);

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Vaccination, 1, userInfo, PermissionLevel.View))
                .ReturnsAsync(true);

            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Vaccination, 1, 1, 0, userInfo))
                .ReturnsAsync(new TimelineItemPermission { PermissionLevel = PermissionLevel.View });

            SearchService service = new(
                progenyContext,
                mediaContext,
                _mockAccessManagementService.Object,
                _mockTimelineService.Object);

            SearchRequest request = CreateTestSearchRequest("MMR");

            // Act
            SearchResponse<Vaccination> result = await service.SearchVaccinations(request, userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Results);
            Assert.Single(result.Results);
            Assert.Equal("MMR Vaccine", result.Results[0].VaccinationName);
        }

        [Fact]
        public async Task SearchVaccinations_Should_Find_Match_In_VaccinationDescription()
        {
            // Arrange
            await using ProgenyDbContext progenyContext = GetInMemoryProgenyDbContext("SearchVaccinations_MatchDescription");
            await using MediaDbContext mediaContext = GetInMemoryMediaDbContext("SearchVaccinations_MatchDescription");

            Vaccination vaccination = CreateTestVaccination(1, 1, "Vaccine", "Measles Mumps Rubella", "Notes");
            progenyContext.VaccinationsDb.Add(vaccination);
            await progenyContext.SaveChangesAsync();

            UserInfo userInfo = CreateTestUserInfo();

            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(1, userInfo, PermissionLevel.View))
                .ReturnsAsync(true);

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Vaccination, 1, userInfo, PermissionLevel.View))
                .ReturnsAsync(true);

            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Vaccination, 1, 1, 0, userInfo))
                .ReturnsAsync(new TimelineItemPermission { PermissionLevel = PermissionLevel.View });

            SearchService service = new(
                progenyContext,
                mediaContext,
                _mockAccessManagementService.Object,
                _mockTimelineService.Object);

            SearchRequest request = CreateTestSearchRequest("measles");

            // Act
            SearchResponse<Vaccination> result = await service.SearchVaccinations(request, userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Results);
            Assert.Single(result.Results);
            Assert.Contains("Measles", result.Results[0].VaccinationDescription);
        }

        [Fact]
        public async Task SearchVaccinations_Should_Find_Match_In_Notes()
        {
            // Arrange
            await using ProgenyDbContext progenyContext = GetInMemoryProgenyDbContext("SearchVaccinations_MatchNotes");
            await using MediaDbContext mediaContext = GetInMemoryMediaDbContext("SearchVaccinations_MatchNotes");

            Vaccination vaccination = CreateTestVaccination(1, 1, "Vaccine", "Description", "Given at pediatrician office");
            progenyContext.VaccinationsDb.Add(vaccination);
            await progenyContext.SaveChangesAsync();

            UserInfo userInfo = CreateTestUserInfo();

            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(1, userInfo, PermissionLevel.View))
                .ReturnsAsync(true);

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Vaccination, 1, userInfo, PermissionLevel.View))
                .ReturnsAsync(true);

            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Vaccination, 1, 1, 0, userInfo))
                .ReturnsAsync(new TimelineItemPermission { PermissionLevel = PermissionLevel.View });

            SearchService service = new(
                progenyContext,
                mediaContext,
                _mockAccessManagementService.Object,
                _mockTimelineService.Object);

            SearchRequest request = CreateTestSearchRequest("pediatrician");

            // Act
            SearchResponse<Vaccination> result = await service.SearchVaccinations(request, userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Results);
            Assert.Single(result.Results);
            Assert.Contains("pediatrician", result.Results[0].Notes);
        }

        #endregion

        #region Case Insensitivity Tests

        [Fact]
        public async Task SearchVaccinations_Should_Be_Case_Insensitive()
        {
            // Arrange
            await using ProgenyDbContext progenyContext = GetInMemoryProgenyDbContext("SearchVaccinations_CaseInsensitive");
            await using MediaDbContext mediaContext = GetInMemoryMediaDbContext("SearchVaccinations_CaseInsensitive");

            Vaccination vaccination = CreateTestVaccination(1, 1, "MMR Vaccine", "Description", "Notes");
            progenyContext.VaccinationsDb.Add(vaccination);
            await progenyContext.SaveChangesAsync();

            UserInfo userInfo = CreateTestUserInfo();

            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(1, userInfo, PermissionLevel.View))
                .ReturnsAsync(true);

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Vaccination, 1, userInfo, PermissionLevel.View))
                .ReturnsAsync(true);

            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Vaccination, 1, 1, 0, userInfo))
                .ReturnsAsync(new TimelineItemPermission { PermissionLevel = PermissionLevel.View });

            SearchService service = new(
                progenyContext,
                mediaContext,
                _mockAccessManagementService.Object,
                _mockTimelineService.Object);

            SearchRequest request = CreateTestSearchRequest("mmr vaccine");

            // Act
            SearchResponse<Vaccination> result = await service.SearchVaccinations(request, userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Results);
            Assert.Single(result.Results);
        }

        #endregion

        #region Multiple Results Tests

        [Fact]
        public async Task SearchVaccinations_Should_Return_Multiple_Matching_Items()
        {
            // Arrange
            await using ProgenyDbContext progenyContext = GetInMemoryProgenyDbContext("SearchVaccinations_MultipleResults");
            await using MediaDbContext mediaContext = GetInMemoryMediaDbContext("SearchVaccinations_MultipleResults");

            List<Vaccination> vaccinations =
            [
                CreateTestVaccination(1, 1, "MMR Vaccine 1"),
                CreateTestVaccination(2, 1, "MMR Vaccine 2"),
                CreateTestVaccination(3, 1, "Polio Vaccine")
            ];

            progenyContext.VaccinationsDb.AddRange(vaccinations);
            await progenyContext.SaveChangesAsync();

            UserInfo userInfo = CreateTestUserInfo();

            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(1, userInfo, PermissionLevel.View))
                .ReturnsAsync(true);

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Vaccination, It.IsAny<int>(), userInfo, PermissionLevel.View))
                .ReturnsAsync(true);

            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Vaccination, It.IsAny<int>(), 1, 0, userInfo))
                .ReturnsAsync(new TimelineItemPermission { PermissionLevel = PermissionLevel.View });

            SearchService service = new(
                progenyContext,
                mediaContext,
                _mockAccessManagementService.Object,
                _mockTimelineService.Object);

            SearchRequest request = CreateTestSearchRequest("MMR");

            // Act
            SearchResponse<Vaccination> result = await service.SearchVaccinations(request, userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Results);
            Assert.Equal(2, result.Results.Count);
            Assert.All(result.Results, v => Assert.Contains("MMR", v.VaccinationName));
        }

        #endregion

        #region Multiple Progenies Tests

        [Fact]
        public async Task SearchVaccinations_Should_Search_Across_Multiple_Progenies()
        {
            // Arrange
            await using ProgenyDbContext progenyContext = GetInMemoryProgenyDbContext("SearchVaccinations_MultipleProgenies");
            await using MediaDbContext mediaContext = GetInMemoryMediaDbContext("SearchVaccinations_MultipleProgenies");

            List<Vaccination> vaccinations =
            [
                CreateTestVaccination(1, 1, "MMR Vaccine"),
                CreateTestVaccination(2, 2, "MMR Booster")
            ];

            progenyContext.VaccinationsDb.AddRange(vaccinations);
            await progenyContext.SaveChangesAsync();

            UserInfo userInfo = CreateTestUserInfo();

            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(It.IsAny<int>(), userInfo, PermissionLevel.View))
                .ReturnsAsync(true);

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Vaccination, It.IsAny<int>(), userInfo, PermissionLevel.View))
                .ReturnsAsync(true);

            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Vaccination, It.IsAny<int>(), It.IsAny<int>(), 0, userInfo))
                .ReturnsAsync(new TimelineItemPermission { PermissionLevel = PermissionLevel.View });

            SearchService service = new(
                progenyContext,
                mediaContext,
                _mockAccessManagementService.Object,
                _mockTimelineService.Object);

            SearchRequest request = CreateTestSearchRequest("MMR", [1, 2]);

            // Act
            SearchResponse<Vaccination> result = await service.SearchVaccinations(request, userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Results);
            Assert.Equal(2, result.Results.Count);
        }

        [Fact]
        public async Task SearchVaccinations_Should_Skip_Progenies_Without_Permission()
        {
            // Arrange
            await using ProgenyDbContext progenyContext = GetInMemoryProgenyDbContext("SearchVaccinations_SkipNoPermission");
            await using MediaDbContext mediaContext = GetInMemoryMediaDbContext("SearchVaccinations_SkipNoPermission");

            List<Vaccination> vaccinations =
            [
                CreateTestVaccination(1, 1, "MMR Vaccine"),
                CreateTestVaccination(2, 2, "MMR Booster")
            ];

            progenyContext.VaccinationsDb.AddRange(vaccinations);
            await progenyContext.SaveChangesAsync();

            UserInfo userInfo = CreateTestUserInfo();

            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(1, userInfo, PermissionLevel.View))
                .ReturnsAsync(true);

            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(2, userInfo, PermissionLevel.View))
                .ReturnsAsync(false);

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Vaccination, 1, userInfo, PermissionLevel.View))
                .ReturnsAsync(true);

            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Vaccination, 1, 1, 0, userInfo))
                .ReturnsAsync(new TimelineItemPermission { PermissionLevel = PermissionLevel.View });

            SearchService service = new(
                progenyContext,
                mediaContext,
                _mockAccessManagementService.Object,
                _mockTimelineService.Object);

            SearchRequest request = CreateTestSearchRequest("MMR", [1, 2]);

            // Act
            SearchResponse<Vaccination> result = await service.SearchVaccinations(request, userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Results);
            Assert.Single(result.Results);
            Assert.Equal(1, result.Results[0].ProgenyId);
        }

        #endregion

        #region Pagination and Sorting Tests

        [Fact]
        public async Task SearchVaccinations_Should_Sort_By_Date_Descending_When_Sort_Is_0()
        {
            // Arrange
            await using ProgenyDbContext progenyContext = GetInMemoryProgenyDbContext("SearchVaccinations_SortDescending");
            await using MediaDbContext mediaContext = GetInMemoryMediaDbContext("SearchVaccinations_SortDescending");

            List<Vaccination> vaccinations =
            [
                new() { VaccinationId = 1, ProgenyId = 1, VaccinationName = "Vaccine A", VaccinationDate = DateTime.UtcNow.AddDays(-10) },
                new() { VaccinationId = 2, ProgenyId = 1, VaccinationName = "Vaccine B", VaccinationDate = DateTime.UtcNow.AddDays(-5) },
                new() { VaccinationId = 3, ProgenyId = 1, VaccinationName = "Vaccine C", VaccinationDate = DateTime.UtcNow.AddDays(-1) }
            ];

            progenyContext.VaccinationsDb.AddRange(vaccinations);
            await progenyContext.SaveChangesAsync();

            UserInfo userInfo = CreateTestUserInfo();

            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(1, userInfo, PermissionLevel.View))
                .ReturnsAsync(true);

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Vaccination, It.IsAny<int>(), userInfo, PermissionLevel.View))
                .ReturnsAsync(true);

            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Vaccination, It.IsAny<int>(), 1, 0, userInfo))
                .ReturnsAsync(new TimelineItemPermission { PermissionLevel = PermissionLevel.View });

            SearchService service = new(
                progenyContext,
                mediaContext,
                _mockAccessManagementService.Object,
                _mockTimelineService.Object);

            SearchRequest request = CreateTestSearchRequest("Vaccine", [1]);

            // Act
            SearchResponse<Vaccination> result = await service.SearchVaccinations(request, userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Results);
            Assert.Equal(3, result.Results.Count);
            Assert.Equal("Vaccine C", result.Results[0].VaccinationName);
            Assert.Equal("Vaccine A", result.Results[2].VaccinationName);
        }

        [Fact]
        public async Task SearchVaccinations_Should_Sort_By_Date_Ascending_When_Sort_Is_1()
        {
            // Arrange
            await using ProgenyDbContext progenyContext = GetInMemoryProgenyDbContext("SearchVaccinations_SortAscending");
            await using MediaDbContext mediaContext = GetInMemoryMediaDbContext("SearchVaccinations_SortAscending");

            List<Vaccination> vaccinations =
            [
                new() { VaccinationId = 1, ProgenyId = 1, VaccinationName = "Vaccine A", VaccinationDate = DateTime.UtcNow.AddDays(-10) },
                new() { VaccinationId = 2, ProgenyId = 1, VaccinationName = "Vaccine B", VaccinationDate = DateTime.UtcNow.AddDays(-5) },
                new() { VaccinationId = 3, ProgenyId = 1, VaccinationName = "Vaccine C", VaccinationDate = DateTime.UtcNow.AddDays(-1) }
            ];

            progenyContext.VaccinationsDb.AddRange(vaccinations);
            await progenyContext.SaveChangesAsync();

            UserInfo userInfo = CreateTestUserInfo();

            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(1, userInfo, PermissionLevel.View))
                .ReturnsAsync(true);

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Vaccination, It.IsAny<int>(), userInfo, PermissionLevel.View))
                .ReturnsAsync(true);

            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Vaccination, It.IsAny<int>(), 1, 0, userInfo))
                .ReturnsAsync(new TimelineItemPermission { PermissionLevel = PermissionLevel.View });

            SearchService service = new(
                progenyContext,
                mediaContext,
                _mockAccessManagementService.Object,
                _mockTimelineService.Object);

            SearchRequest request = CreateTestSearchRequest("Vaccine", [1], 0, 10, 1);

            // Act
            SearchResponse<Vaccination> result = await service.SearchVaccinations(request, userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Results);
            Assert.Equal(3, result.Results.Count);
            Assert.Equal("Vaccine A", result.Results[0].VaccinationName);
            Assert.Equal("Vaccine C", result.Results[2].VaccinationName);
        }

        [Fact]
        public async Task SearchVaccinations_Should_Apply_Skip_And_Take()
        {
            // Arrange
            await using ProgenyDbContext progenyContext = GetInMemoryProgenyDbContext("SearchVaccinations_Pagination");
            await using MediaDbContext mediaContext = GetInMemoryMediaDbContext("SearchVaccinations_Pagination");

            List<Vaccination> vaccinations = [];
            for (int i = 1; i <= 10; i++)
            {
                vaccinations.Add(new Vaccination
                {
                    VaccinationId = i,
                    ProgenyId = 1,
                    VaccinationName = $"Vaccine {i}",
                    VaccinationDate = DateTime.UtcNow.AddDays(-i)
                });
            }

            progenyContext.VaccinationsDb.AddRange(vaccinations);
            await progenyContext.SaveChangesAsync();

            UserInfo userInfo = CreateTestUserInfo();

            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(1, userInfo, PermissionLevel.View))
                .ReturnsAsync(true);

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Vaccination, It.IsAny<int>(), userInfo, PermissionLevel.View))
                .ReturnsAsync(true);

            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Vaccination, It.IsAny<int>(), 1, 0, userInfo))
                .ReturnsAsync(new TimelineItemPermission { PermissionLevel = PermissionLevel.View });

            SearchService service = new(
                progenyContext,
                mediaContext,
                _mockAccessManagementService.Object,
                _mockTimelineService.Object);

            SearchRequest request = CreateTestSearchRequest("Vaccine", [1], 2, 3);

            // Act
            SearchResponse<Vaccination> result = await service.SearchVaccinations(request, userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Results);
            Assert.Equal(3, result.Results.Count);
            Assert.Equal(10, result.TotalCount);
        }

        #endregion

        #region Response Properties Tests

        [Fact]
        public async Task SearchVaccinations_Should_Set_ItemPermission_For_Each_Result()
        {
            // Arrange
            await using ProgenyDbContext progenyContext = GetInMemoryProgenyDbContext("SearchVaccinations_ItemPermission");
            await using MediaDbContext mediaContext = GetInMemoryMediaDbContext("SearchVaccinations_ItemPermission");

            Vaccination vaccination = CreateTestVaccination(1, 1, "MMR Vaccine");
            progenyContext.VaccinationsDb.Add(vaccination);
            await progenyContext.SaveChangesAsync();

            UserInfo userInfo = CreateTestUserInfo();
            TimelineItemPermission permission = new() { PermissionLevel = PermissionLevel.Edit };

            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(1, userInfo, PermissionLevel.View))
                .ReturnsAsync(true);

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Vaccination, 1, userInfo, PermissionLevel.View))
                .ReturnsAsync(true);

            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Vaccination, 1, 1, 0, userInfo))
                .ReturnsAsync(permission);

            SearchService service = new(
                progenyContext,
                mediaContext,
                _mockAccessManagementService.Object,
                _mockTimelineService.Object);

            SearchRequest request = CreateTestSearchRequest("MMR");

            // Act
            SearchResponse<Vaccination> result = await service.SearchVaccinations(request, userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Results);
            Assert.Single(result.Results);
            Assert.NotNull(result.Results[0].ItemPermission);
            Assert.Equal(PermissionLevel.Edit, result.Results[0].ItemPermission.PermissionLevel);
        }

        [Fact]
        public async Task SearchVaccinations_Should_Set_TotalCount_Correctly()
        {
            // Arrange
            await using ProgenyDbContext progenyContext = GetInMemoryProgenyDbContext("SearchVaccinations_TotalCount");
            await using MediaDbContext mediaContext = GetInMemoryMediaDbContext("SearchVaccinations_TotalCount");

            List<Vaccination> vaccinations =
            [
                CreateTestVaccination(1, 1, "Vaccine A"),
                CreateTestVaccination(2, 1, "Vaccine B"),
                CreateTestVaccination(3, 1, "Vaccine C")
            ];

            progenyContext.VaccinationsDb.AddRange(vaccinations);
            await progenyContext.SaveChangesAsync();

            UserInfo userInfo = CreateTestUserInfo();

            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(1, userInfo, PermissionLevel.View))
                .ReturnsAsync(true);

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Vaccination, It.IsAny<int>(), userInfo, PermissionLevel.View))
                .ReturnsAsync(true);

            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Vaccination, It.IsAny<int>(), 1, 0, userInfo))
                .ReturnsAsync(new TimelineItemPermission { PermissionLevel = PermissionLevel.View });

            SearchService service = new(
                progenyContext,
                mediaContext,
                _mockAccessManagementService.Object,
                _mockTimelineService.Object);

            SearchRequest request = CreateTestSearchRequest("Vaccine", [1], 0, 2);

            // Act
            SearchResponse<Vaccination> result = await service.SearchVaccinations(request, userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(3, result.TotalCount);
            Assert.Equal(2, result.Results.Count);
        }

        [Fact]
        public async Task SearchVaccinations_Should_Set_PageNumber_Correctly()
        {
            // Arrange
            await using ProgenyDbContext progenyContext = GetInMemoryProgenyDbContext("SearchVaccinations_PageNumber");
            await using MediaDbContext mediaContext = GetInMemoryMediaDbContext("SearchVaccinations_PageNumber");

            List<Vaccination> vaccinations = [];
            for (int i = 1; i <= 10; i++)
            {
                vaccinations.Add(CreateTestVaccination(i, 1, $"Vaccine {i}"));
            }

            progenyContext.VaccinationsDb.AddRange(vaccinations);
            await progenyContext.SaveChangesAsync();

            UserInfo userInfo = CreateTestUserInfo();

            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(1, userInfo, PermissionLevel.View))
                .ReturnsAsync(true);

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Vaccination, It.IsAny<int>(), userInfo, PermissionLevel.View))
                .ReturnsAsync(true);

            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Vaccination, It.IsAny<int>(), 1, 0, userInfo))
                .ReturnsAsync(new TimelineItemPermission { PermissionLevel = PermissionLevel.View });

            SearchService service = new(
                progenyContext,
                mediaContext,
                _mockAccessManagementService.Object,
                _mockTimelineService.Object);

            SearchRequest request = CreateTestSearchRequest("Vaccine", [1], 4, 2);

            // Act
            SearchResponse<Vaccination> result = await service.SearchVaccinations(request, userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(3, result.PageNumber);
        }

        [Fact]
        public async Task SearchVaccinations_Should_Set_RemainingItems_Correctly()
        {
            // Arrange
            await using ProgenyDbContext progenyContext = GetInMemoryProgenyDbContext("SearchVaccinations_RemainingItems");
            await using MediaDbContext mediaContext = GetInMemoryMediaDbContext("SearchVaccinations_RemainingItems");

            List<Vaccination> vaccinations = [];
            for (int i = 1; i <= 10; i++)
            {
                vaccinations.Add(CreateTestVaccination(i, 1, $"Vaccine {i}"));
            }

            progenyContext.VaccinationsDb.AddRange(vaccinations);
            await progenyContext.SaveChangesAsync();

            UserInfo userInfo = CreateTestUserInfo();

            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(1, userInfo, PermissionLevel.View))
                .ReturnsAsync(true);

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Vaccination, It.IsAny<int>(), userInfo, PermissionLevel.View))
                .ReturnsAsync(true);

            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Vaccination, It.IsAny<int>(), 1, 0, userInfo))
                .ReturnsAsync(new TimelineItemPermission { PermissionLevel = PermissionLevel.View });

            SearchService service = new(
                progenyContext,
                mediaContext,
                _mockAccessManagementService.Object,
                _mockTimelineService.Object);

            SearchRequest request = CreateTestSearchRequest("Vaccine", [1], 2, 3);

            // Act
            SearchResponse<Vaccination> result = await service.SearchVaccinations(request, userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(5, result.RemainingItems);
        }

        #endregion

        #region Edge Cases Tests

        [Fact]
        public async Task SearchVaccinations_Should_Handle_Empty_ProgenyIds_List()
        {
            // Arrange
            await using ProgenyDbContext progenyContext = GetInMemoryProgenyDbContext("SearchVaccinations_EmptyProgenyIds");
            await using MediaDbContext mediaContext = GetInMemoryMediaDbContext("SearchVaccinations_EmptyProgenyIds");

            Vaccination vaccination = CreateTestVaccination(1, 1, "MMR Vaccine");
            progenyContext.VaccinationsDb.Add(vaccination);
            await progenyContext.SaveChangesAsync();

            UserInfo userInfo = CreateTestUserInfo();

            SearchService service = new(
                progenyContext,
                mediaContext,
                _mockAccessManagementService.Object,
                _mockTimelineService.Object);

            SearchRequest request = CreateTestSearchRequest("MMR", []);

            // Act
            SearchResponse<Vaccination> result = await service.SearchVaccinations(request, userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Results);
            Assert.Empty(result.Results);
        }

        [Fact]
        public async Task SearchVaccinations_Should_Trim_Query_Whitespace()
        {
            // Arrange
            await using ProgenyDbContext progenyContext = GetInMemoryProgenyDbContext("SearchVaccinations_TrimQuery");
            await using MediaDbContext mediaContext = GetInMemoryMediaDbContext("SearchVaccinations_TrimQuery");

            Vaccination vaccination = CreateTestVaccination(1, 1, "MMR Vaccine");
            progenyContext.VaccinationsDb.Add(vaccination);
            await progenyContext.SaveChangesAsync();

            UserInfo userInfo = CreateTestUserInfo();

            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(1, userInfo, PermissionLevel.View))
                .ReturnsAsync(true);

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Vaccination, 1, userInfo, PermissionLevel.View))
                .ReturnsAsync(true);

            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Vaccination, 1, 1, 0, userInfo))
                .ReturnsAsync(new TimelineItemPermission { PermissionLevel = PermissionLevel.View });

            SearchService service = new(
                progenyContext,
                mediaContext,
                _mockAccessManagementService.Object,
                _mockTimelineService.Object);

            SearchRequest request = CreateTestSearchRequest("  MMR  ");

            // Act
            SearchResponse<Vaccination> result = await service.SearchVaccinations(request, userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Results);
            Assert.Single(result.Results);
        }

        [Fact]
        public async Task SearchVaccinations_Should_Return_No_Results_When_No_Match_Found()
        {
            // Arrange
            await using ProgenyDbContext progenyContext = GetInMemoryProgenyDbContext("SearchVaccinations_NoMatch");
            await using MediaDbContext mediaContext = GetInMemoryMediaDbContext("SearchVaccinations_NoMatch");

            Vaccination vaccination = CreateTestVaccination(1, 1, "MMR Vaccine");
            progenyContext.VaccinationsDb.Add(vaccination);
            await progenyContext.SaveChangesAsync();

            UserInfo userInfo = CreateTestUserInfo();

            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(1, userInfo, PermissionLevel.View))
                .ReturnsAsync(true);

            SearchService service = new(
                progenyContext,
                mediaContext,
                _mockAccessManagementService.Object,
                _mockTimelineService.Object);

            SearchRequest request = CreateTestSearchRequest("Polio");

            // Act
            SearchResponse<Vaccination> result = await service.SearchVaccinations(request, userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Results);
            Assert.Empty(result.Results);
        }

        #endregion
    }
}