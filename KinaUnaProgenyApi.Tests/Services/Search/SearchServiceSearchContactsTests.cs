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
    public class SearchServiceSearchContactsTests
    {
        private readonly Mock<IAccessManagementService> _mockAccessManagementService = new();
        private readonly Mock<ITimelineService> _mockTimelineService = new();
        private readonly UserInfo _testUser = new() { UserId = "user1", UserEmail = "user1@example.com" };

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

        #region SearchContacts Tests

        [Fact]
        public async Task SearchContacts_Should_Return_Empty_Response_When_CurrentUserInfo_Is_Null()
        {
            // Arrange
            await using ProgenyDbContext progenyContext = GetInMemoryProgenyDbContext("SearchContacts_NullUser");
            await using MediaDbContext mediaContext = GetInMemoryMediaDbContext("SearchContacts_NullUser");

            SearchService service = new(progenyContext, mediaContext, _mockAccessManagementService.Object, _mockTimelineService.Object);

            SearchRequest request = new()
            {
                Query = "test",
                ProgenyIds = [1]
            };

            // Act
            SearchResponse<Contact> result = await service.SearchContacts(request, null!);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result.Results);
            Assert.Equal(request, result.SearchRequest);
        }

        [Fact]
        public async Task SearchContacts_Should_Return_Empty_Response_When_Query_Is_Empty()
        {
            // Arrange
            await using ProgenyDbContext progenyContext = GetInMemoryProgenyDbContext("SearchContacts_EmptyQuery");
            await using MediaDbContext mediaContext = GetInMemoryMediaDbContext("SearchContacts_EmptyQuery");

            SearchService service = new(progenyContext, mediaContext, _mockAccessManagementService.Object, _mockTimelineService.Object);

            SearchRequest request = new()
            {
                Query = "",
                ProgenyIds = [1]
            };

            // Act
            SearchResponse<Contact> result = await service.SearchContacts(request, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result.Results);
            Assert.Equal(request, result.SearchRequest);
        }

        [Fact]
        public async Task SearchContacts_Should_Return_Empty_Response_When_Query_Is_Whitespace()
        {
            // Arrange
            await using ProgenyDbContext progenyContext = GetInMemoryProgenyDbContext("SearchContacts_WhitespaceQuery");
            await using MediaDbContext mediaContext = GetInMemoryMediaDbContext("SearchContacts_WhitespaceQuery");

            SearchService service = new(progenyContext, mediaContext, _mockAccessManagementService.Object, _mockTimelineService.Object);

            SearchRequest request = new()
            {
                Query = "   ",
                ProgenyIds = [1]
            };

            // Act
            SearchResponse<Contact> result = await service.SearchContacts(request, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result.Results);
        }

        [Fact]
        public async Task SearchContacts_Should_Return_Matching_Contacts_For_Progeny()
        {
            // Arrange
            await using ProgenyDbContext progenyContext = GetInMemoryProgenyDbContext("SearchContacts_MatchingProgeny");
            await using MediaDbContext mediaContext = GetInMemoryMediaDbContext("SearchContacts_MatchingProgeny");

            Contact contact1 = new()
            {
                ContactId = 1,
                ProgenyId = 1,
                FamilyId = 0,
                FirstName = "John",
                MiddleName = "",
                LastName = "Doe",
                DisplayName = "John Doe",
                Email1 = "john@example.com",
                Email2 = "",
                Context = "Work",
                Notes = "Test contact",
                Website = "",
                Tags = "friend",
                DateAdded = DateTime.UtcNow,
                CreatedTime = DateTime.UtcNow
            };

            progenyContext.ContactsDb.Add(contact1);
            await progenyContext.SaveChangesAsync(TestContext.Current.CancellationToken);

            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(1, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);

            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Contact, 1, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);

            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Contact, 1, 1, 0, _testUser, null))
                .ReturnsAsync(new TimelineItemPermission { PermissionLevel = PermissionLevel.View });

            SearchService service = new(progenyContext, mediaContext, _mockAccessManagementService.Object, _mockTimelineService.Object);

            SearchRequest request = new()
            {
                Query = "john",
                ProgenyIds = [1]
            };

            // Act
            SearchResponse<Contact> result = await service.SearchContacts(request, _testUser);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result.Results);
            Assert.Equal("John", result.Results[0].FirstName);
            Assert.NotNull(result.Results[0].ItemPerMission);
        }

        [Fact]
        public async Task SearchContacts_Should_Search_In_FirstName()
        {
            // Arrange
            await using ProgenyDbContext progenyContext = GetInMemoryProgenyDbContext("SearchContacts_FirstName");
            await using MediaDbContext mediaContext = GetInMemoryMediaDbContext("SearchContacts_FirstName");

            Contact contact = new()
            {
                ContactId = 1,
                ProgenyId = 1,
                FamilyId = 0,
                FirstName = "Alexander",
                MiddleName = "",
                LastName = "Smith",
                DisplayName = "",
                Email1 = "",
                Email2 = "",
                Context = "",
                Notes = "",
                Website = "",
                Tags = "",
                DateAdded = DateTime.UtcNow,
                CreatedTime = DateTime.UtcNow
            };

            progenyContext.ContactsDb.Add(contact);
            await progenyContext.SaveChangesAsync(TestContext.Current.CancellationToken);

            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(1, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Contact, 1, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Contact, 1, 1, 0, _testUser, null))
                .ReturnsAsync(new TimelineItemPermission { PermissionLevel = PermissionLevel.View });

            SearchService service = new(progenyContext, mediaContext, _mockAccessManagementService.Object, _mockTimelineService.Object);

            SearchRequest request = new()
            {
                Query = "alex",
                ProgenyIds = [1]
            };

            // Act
            SearchResponse<Contact> result = await service.SearchContacts(request, _testUser);

            // Assert
            Assert.Single(result.Results);
            Assert.Equal("Alexander", result.Results[0].FirstName);
        }

        [Fact]
        public async Task SearchContacts_Should_Search_In_MiddleName()
        {
            // Arrange
            await using ProgenyDbContext progenyContext = GetInMemoryProgenyDbContext("SearchContacts_MiddleName");
            await using MediaDbContext mediaContext = GetInMemoryMediaDbContext("SearchContacts_MiddleName");

            Contact contact = new()
            {
                ContactId = 1,
                ProgenyId = 1,
                FamilyId = 0,
                FirstName = "John",
                MiddleName = "William",
                LastName = "Smith",
                DisplayName = "",
                Email1 = "",
                Email2 = "",
                Context = "",
                Notes = "",
                Website = "",
                Tags = "",
                DateAdded = DateTime.UtcNow,
                CreatedTime = DateTime.UtcNow
            };

            progenyContext.ContactsDb.Add(contact);
            await progenyContext.SaveChangesAsync(TestContext.Current.CancellationToken);

            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(1, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Contact, 1, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Contact, 1, 1, 0, _testUser, null))
                .ReturnsAsync(new TimelineItemPermission { PermissionLevel = PermissionLevel.View });

            SearchService service = new(progenyContext, mediaContext, _mockAccessManagementService.Object, _mockTimelineService.Object);

            SearchRequest request = new()
            {
                Query = "william",
                ProgenyIds = [1]
            };

            // Act
            SearchResponse<Contact> result = await service.SearchContacts(request, _testUser);

            // Assert
            Assert.Single(result.Results);
            Assert.Equal("William", result.Results[0].MiddleName);
        }

        [Fact]
        public async Task SearchContacts_Should_Search_In_LastName()
        {
            // Arrange
            await using ProgenyDbContext progenyContext = GetInMemoryProgenyDbContext("SearchContacts_LastName");
            await using MediaDbContext mediaContext = GetInMemoryMediaDbContext("SearchContacts_LastName");

            Contact contact = new()
            {
                ContactId = 1,
                ProgenyId = 1,
                FamilyId = 0,
                FirstName = "John",
                MiddleName = "",
                LastName = "Peterson",
                DisplayName = "",
                Email1 = "",
                Email2 = "",
                Context = "",
                Notes = "",
                Website = "",
                Tags = "",
                DateAdded = DateTime.UtcNow,
                CreatedTime = DateTime.UtcNow
            };

            progenyContext.ContactsDb.Add(contact);
            await progenyContext.SaveChangesAsync(TestContext.Current.CancellationToken);

            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(1, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Contact, 1, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Contact, 1, 1, 0, _testUser, null))
                .ReturnsAsync(new TimelineItemPermission { PermissionLevel = PermissionLevel.View });

            SearchService service = new(progenyContext, mediaContext, _mockAccessManagementService.Object, _mockTimelineService.Object);

            SearchRequest request = new()
            {
                Query = "peter",
                ProgenyIds = [1]
            };

            // Act
            SearchResponse<Contact> result = await service.SearchContacts(request, _testUser);

            // Assert
            Assert.Single(result.Results);
            Assert.Equal("Peterson", result.Results[0].LastName);
        }

        [Fact]
        public async Task SearchContacts_Should_Search_In_Email1()
        {
            // Arrange
            await using ProgenyDbContext progenyContext = GetInMemoryProgenyDbContext("SearchContacts_Email1");
            await using MediaDbContext mediaContext = GetInMemoryMediaDbContext("SearchContacts_Email1");

            Contact contact = new()
            {
                ContactId = 1,
                ProgenyId = 1,
                FamilyId = 0,
                FirstName = "John",
                MiddleName = "",
                LastName = "Doe",
                DisplayName = "",
                Email1 = "johndoe@company.com",
                Email2 = "",
                Context = "",
                Notes = "",
                Website = "",
                Tags = "",
                DateAdded = DateTime.UtcNow,
                CreatedTime = DateTime.UtcNow
            };

            progenyContext.ContactsDb.Add(contact);
            await progenyContext.SaveChangesAsync(TestContext.Current.CancellationToken);

            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(1, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Contact, 1, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Contact, 1, 1, 0, _testUser, null))
                .ReturnsAsync(new TimelineItemPermission { PermissionLevel = PermissionLevel.View });

            SearchService service = new(progenyContext, mediaContext, _mockAccessManagementService.Object, _mockTimelineService.Object);

            SearchRequest request = new()
            {
                Query = "company",
                ProgenyIds = [1]
            };

            // Act
            SearchResponse<Contact> result = await service.SearchContacts(request, _testUser);

            // Assert
            Assert.Single(result.Results);
            Assert.Contains("company", result.Results[0].Email1);
        }

        [Fact]
        public async Task SearchContacts_Should_Search_In_Tags()
        {
            // Arrange
            await using ProgenyDbContext progenyContext = GetInMemoryProgenyDbContext("SearchContacts_Tags");
            await using MediaDbContext mediaContext = GetInMemoryMediaDbContext("SearchContacts_Tags");

            Contact contact = new()
            {
                ContactId = 1,
                ProgenyId = 1,
                FamilyId = 0,
                FirstName = "John",
                MiddleName = "",
                LastName = "Doe",
                DisplayName = "",
                Email1 = "",
                Email2 = "",
                Context = "",
                Notes = "",
                Website = "",
                Tags = "colleague, basketball",
                DateAdded = DateTime.UtcNow,
                CreatedTime = DateTime.UtcNow
            };

            progenyContext.ContactsDb.Add(contact);
            await progenyContext.SaveChangesAsync(TestContext.Current.CancellationToken);

            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(1, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Contact, 1, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Contact, 1, 1, 0, _testUser, null))
                .ReturnsAsync(new TimelineItemPermission { PermissionLevel = PermissionLevel.View });

            SearchService service = new(progenyContext, mediaContext, _mockAccessManagementService.Object, _mockTimelineService.Object);

            SearchRequest request = new()
            {
                Query = "basketball",
                ProgenyIds = [1]
            };

            // Act
            SearchResponse<Contact> result = await service.SearchContacts(request, _testUser);

            // Assert
            Assert.Single(result.Results);
            Assert.Contains("basketball", result.Results[0].Tags);
        }

        [Fact]
        public async Task SearchContacts_Should_Be_Case_Insensitive()
        {
            // Arrange
            await using ProgenyDbContext progenyContext = GetInMemoryProgenyDbContext("SearchContacts_CaseInsensitive");
            await using MediaDbContext mediaContext = GetInMemoryMediaDbContext("SearchContacts_CaseInsensitive");

            Contact contact = new()
            {
                ContactId = 1,
                ProgenyId = 1,
                FamilyId = 0,
                FirstName = "JOHN",
                MiddleName = "",
                LastName = "DOE",
                DisplayName = "",
                Email1 = "",
                Email2 = "",
                Context = "",
                Notes = "",
                Website = "",
                Tags = "",
                DateAdded = DateTime.UtcNow,
                CreatedTime = DateTime.UtcNow
            };

            progenyContext.ContactsDb.Add(contact);
            await progenyContext.SaveChangesAsync(TestContext.Current.CancellationToken);

            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(1, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Contact, 1, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Contact, 1, 1, 0, _testUser, null))
                .ReturnsAsync(new TimelineItemPermission { PermissionLevel = PermissionLevel.View });

            SearchService service = new(progenyContext, mediaContext, _mockAccessManagementService.Object, _mockTimelineService.Object);

            SearchRequest request = new()
            {
                Query = "john",
                ProgenyIds = [1]
            };

            // Act
            SearchResponse<Contact> result = await service.SearchContacts(request, _testUser);

            // Assert
            Assert.Single(result.Results);
        }

        [Fact]
        public async Task SearchContacts_Should_Filter_Out_Contacts_Without_Permission()
        {
            // Arrange
            await using ProgenyDbContext progenyContext = GetInMemoryProgenyDbContext("SearchContacts_NoPermission");
            await using MediaDbContext mediaContext = GetInMemoryMediaDbContext("SearchContacts_NoPermission");

            Contact contact1 = new()
            {
                ContactId = 1,
                ProgenyId = 1,
                FamilyId = 0,
                FirstName = "John",
                MiddleName = "",
                LastName = "Doe",
                DisplayName = "",
                Email1 = "",
                Email2 = "",
                Context = "",
                Notes = "",
                Website = "",
                Tags = "",
                DateAdded = DateTime.UtcNow,
                CreatedTime = DateTime.UtcNow
            };

            Contact contact2 = new()
            {
                ContactId = 2,
                ProgenyId = 1,
                FamilyId = 0,
                FirstName = "Jane",
                MiddleName = "",
                LastName = "Johnson",
                DisplayName = "",
                Email1 = "",
                Email2 = "",
                Context = "",
                Notes = "",
                Website = "",
                Tags = "",
                DateAdded = DateTime.UtcNow,
                CreatedTime = DateTime.UtcNow
            };

            progenyContext.ContactsDb.AddRange(contact1, contact2);
            await progenyContext.SaveChangesAsync(TestContext.Current.CancellationToken);

            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(1, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Contact, 1, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Contact, 2, _testUser, PermissionLevel.View))
                .ReturnsAsync(false);
            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Contact, 1, 1, 0, _testUser, null))
                .ReturnsAsync(new TimelineItemPermission { PermissionLevel = PermissionLevel.View });

            SearchService service = new(progenyContext, mediaContext, _mockAccessManagementService.Object, _mockTimelineService.Object);

            SearchRequest request = new()
            {
                Query = "j",
                ProgenyIds = [1]
            };

            // Act
            SearchResponse<Contact> result = await service.SearchContacts(request, _testUser);

            // Assert
            Assert.Single(result.Results);
            Assert.Equal("John", result.Results[0].FirstName);
        }

        [Fact]
        public async Task SearchContacts_Should_Skip_Progenies_Without_Permission()
        {
            // Arrange
            await using ProgenyDbContext progenyContext = GetInMemoryProgenyDbContext("SearchContacts_SkipProgeny");
            await using MediaDbContext mediaContext = GetInMemoryMediaDbContext("SearchContacts_SkipProgeny");

            Contact contact = new()
            {
                ContactId = 1,
                ProgenyId = 1,
                FamilyId = 0,
                FirstName = "John",
                MiddleName = "",
                LastName = "Doe",
                DisplayName = "",
                Email1 = "",
                Email2 = "",
                Context = "",
                Notes = "",
                Website = "",
                Tags = "",
                DateAdded = DateTime.UtcNow,
                CreatedTime = DateTime.UtcNow
            };

            progenyContext.ContactsDb.Add(contact);
            await progenyContext.SaveChangesAsync(TestContext.Current.CancellationToken);

            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(1, _testUser, PermissionLevel.View))
                .ReturnsAsync(false);

            SearchService service = new(progenyContext, mediaContext, _mockAccessManagementService.Object, _mockTimelineService.Object);

            SearchRequest request = new()
            {
                Query = "john",
                ProgenyIds = [1]
            };

            // Act
            SearchResponse<Contact> result = await service.SearchContacts(request, _testUser);

            // Assert
            Assert.Empty(result.Results);
        }

        [Fact]
        public async Task SearchContacts_Should_Return_Contacts_From_Multiple_Progenies()
        {
            // Arrange
            await using ProgenyDbContext progenyContext = GetInMemoryProgenyDbContext("SearchContacts_MultipleProgenies");
            await using MediaDbContext mediaContext = GetInMemoryMediaDbContext("SearchContacts_MultipleProgenies");

            Contact contact1 = new()
            {
                ContactId = 1,
                ProgenyId = 1,
                FamilyId = 0,
                FirstName = "John",
                MiddleName = "",
                LastName = "Doe",
                DisplayName = "",
                Email1 = "",
                Email2 = "",
                Context = "",
                Notes = "",
                Website = "",
                Tags = "",
                DateAdded = DateTime.UtcNow,
                CreatedTime = DateTime.UtcNow
            };

            Contact contact2 = new()
            {
                ContactId = 2,
                ProgenyId = 2,
                FamilyId = 0,
                FirstName = "Johnny",
                MiddleName = "",
                LastName = "Smith",
                DisplayName = "",
                Email1 = "",
                Email2 = "",
                Context = "",
                Notes = "",
                Website = "",
                Tags = "",
                DateAdded = DateTime.UtcNow,
                CreatedTime = DateTime.UtcNow
            };

            progenyContext.ContactsDb.AddRange(contact1, contact2);
            await progenyContext.SaveChangesAsync(TestContext.Current.CancellationToken);

            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(It.IsAny<int>(), _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Contact, It.IsAny<int>(), _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Contact, It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), _testUser, null))
                .ReturnsAsync(new TimelineItemPermission { PermissionLevel = PermissionLevel.View });

            SearchService service = new(progenyContext, mediaContext, _mockAccessManagementService.Object, _mockTimelineService.Object);

            SearchRequest request = new()
            {
                Query = "john",
                ProgenyIds = [1, 2]
            };

            // Act
            SearchResponse<Contact> result = await service.SearchContacts(request, _testUser);

            // Assert
            Assert.Equal(2, result.Results.Count);
        }

        [Fact]
        public async Task SearchContacts_Should_Return_Family_Contacts()
        {
            // Arrange
            await using ProgenyDbContext progenyContext = GetInMemoryProgenyDbContext("SearchContacts_Family");
            await using MediaDbContext mediaContext = GetInMemoryMediaDbContext("SearchContacts_Family");

            Contact contact = new()
            {
                ContactId = 1,
                ProgenyId = 0,
                FamilyId = 1,
                FirstName = "John",
                MiddleName = "",
                LastName = "Doe",
                DisplayName = "",
                Email1 = "",
                Email2 = "",
                Context = "",
                Notes = "",
                Website = "",
                Tags = "",
                DateAdded = DateTime.UtcNow,
                CreatedTime = DateTime.UtcNow
            };

            progenyContext.ContactsDb.Add(contact);
            await progenyContext.SaveChangesAsync(TestContext.Current.CancellationToken);

            _mockAccessManagementService
                .Setup(x => x.HasFamilyPermission(1, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Contact, 1, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Contact, 1, 0, 1, _testUser, null))
                .ReturnsAsync(new TimelineItemPermission { PermissionLevel = PermissionLevel.View });

            SearchService service = new(progenyContext, mediaContext, _mockAccessManagementService.Object, _mockTimelineService.Object);

            SearchRequest request = new()
            {
                Query = "john",
                FamilyIds = [1]
            };

            // Act
            SearchResponse<Contact> result = await service.SearchContacts(request, _testUser);

            // Assert
            Assert.Single(result.Results);
            Assert.Equal(1, result.Results[0].FamilyId);
            Assert.Equal(0, result.Results[0].ProgenyId);
        }

        [Fact]
        public async Task SearchContacts_Should_Apply_Pagination_Skip()
        {
            // Arrange
            await using ProgenyDbContext progenyContext = GetInMemoryProgenyDbContext("SearchContacts_Skip");
            await using MediaDbContext mediaContext = GetInMemoryMediaDbContext("SearchContacts_Skip");

            for (int i = 1; i <= 5; i++)
            {
                Contact contact = new()
                {
                    ContactId = i,
                    ProgenyId = 1,
                    FamilyId = 0,
                    FirstName = $"John{i}",
                    MiddleName = "",
                    LastName = "Doe",
                    DisplayName = "",
                    Email1 = "",
                    Email2 = "",
                    Context = "",
                    Notes = "",
                    Website = "",
                    Tags = "",
                    DateAdded = DateTime.UtcNow.AddDays(-i),
                    CreatedTime = DateTime.UtcNow.AddDays(-i)
                };
                progenyContext.ContactsDb.Add(contact);
            }
            await progenyContext.SaveChangesAsync(TestContext.Current.CancellationToken);

            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(1, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Contact, It.IsAny<int>(), _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Contact, It.IsAny<int>(), 1, 0, _testUser, null))
                .ReturnsAsync(new TimelineItemPermission { PermissionLevel = PermissionLevel.View });

            SearchService service = new(progenyContext, mediaContext, _mockAccessManagementService.Object, _mockTimelineService.Object);

            SearchRequest request = new()
            {
                Query = "john",
                ProgenyIds = [1],
                Skip = 2,
                NumberOfItems = 10
            };

            // Act
            SearchResponse<Contact> result = await service.SearchContacts(request, _testUser);

            // Assert
            Assert.Equal(3, result.Results.Count);
            Assert.Equal(5, result.TotalCount);
        }

        [Fact]
        public async Task SearchContacts_Should_Apply_Pagination_NumberOfItems()
        {
            // Arrange
            await using ProgenyDbContext progenyContext = GetInMemoryProgenyDbContext("SearchContacts_NumberOfItems");
            await using MediaDbContext mediaContext = GetInMemoryMediaDbContext("SearchContacts_NumberOfItems");

            for (int i = 1; i <= 5; i++)
            {
                Contact contact = new()
                {
                    ContactId = i,
                    ProgenyId = 1,
                    FamilyId = 0,
                    FirstName = $"John{i}",
                    MiddleName = "",
                    LastName = "Doe",
                    DisplayName = "",
                    Email1 = "",
                    Email2 = "",
                    Context = "",
                    Notes = "",
                    Website = "",
                    Tags = "",
                    DateAdded = DateTime.UtcNow.AddDays(-i),
                    CreatedTime = DateTime.UtcNow.AddDays(-i)
                };
                progenyContext.ContactsDb.Add(contact);
            }
            await progenyContext.SaveChangesAsync(TestContext.Current.CancellationToken);

            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(1, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Contact, It.IsAny<int>(), _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Contact, It.IsAny<int>(), 1, 0, _testUser, null))
                .ReturnsAsync(new TimelineItemPermission { PermissionLevel = PermissionLevel.View });

            SearchService service = new(progenyContext, mediaContext, _mockAccessManagementService.Object, _mockTimelineService.Object);

            SearchRequest request = new()
            {
                Query = "john",
                ProgenyIds = [1],
                NumberOfItems = 2
            };

            // Act
            SearchResponse<Contact> result = await service.SearchContacts(request, _testUser);

            // Assert
            Assert.Equal(2, result.Results.Count);
            Assert.Equal(5, result.TotalCount);
            Assert.Equal(3, result.RemainingItems);
        }

        [Fact]
        public async Task SearchContacts_Should_Sort_Descending_By_Default()
        {
            // Arrange
            await using ProgenyDbContext progenyContext = GetInMemoryProgenyDbContext("SearchContacts_SortDesc");
            await using MediaDbContext mediaContext = GetInMemoryMediaDbContext("SearchContacts_SortDesc");

            Contact contact1 = new()
            {
                ContactId = 1,
                ProgenyId = 1,
                FamilyId = 0,
                FirstName = "John",
                MiddleName = "",
                LastName = "Older",
                DisplayName = "",
                Email1 = "",
                Email2 = "",
                Context = "",
                Notes = "",
                Website = "",
                Tags = "",
                DateAdded = DateTime.UtcNow.AddDays(-10),
                CreatedTime = DateTime.UtcNow.AddDays(-10)
            };

            Contact contact2 = new()
            {
                ContactId = 2,
                ProgenyId = 1,
                FamilyId = 0,
                FirstName = "Johnny",
                MiddleName = "",
                LastName = "Newer",
                DisplayName = "",
                Email1 = "",
                Email2 = "",
                Context = "",
                Notes = "",
                Website = "",
                Tags = "",
                DateAdded = DateTime.UtcNow.AddDays(-1),
                CreatedTime = DateTime.UtcNow.AddDays(-1)
            };

            progenyContext.ContactsDb.AddRange(contact1, contact2);
            await progenyContext.SaveChangesAsync(TestContext.Current.CancellationToken);

            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(1, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Contact, It.IsAny<int>(), _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Contact, It.IsAny<int>(), 1, 0, _testUser, null))
                .ReturnsAsync(new TimelineItemPermission { PermissionLevel = PermissionLevel.View });

            SearchService service = new(progenyContext, mediaContext, _mockAccessManagementService.Object, _mockTimelineService.Object);

            SearchRequest request = new()
            {
                Query = "john",
                ProgenyIds = [1],
                Sort = 0
            };

            // Act
            SearchResponse<Contact> result = await service.SearchContacts(request, _testUser);

            // Assert
            Assert.Equal(2, result.Results.Count);
            Assert.Equal("Newer", result.Results[0].LastName);
            Assert.Equal("Older", result.Results[1].LastName);
        }

        [Fact]
        public async Task SearchContacts_Should_Sort_Ascending_When_Sort_Is_1()
        {
            // Arrange
            await using ProgenyDbContext progenyContext = GetInMemoryProgenyDbContext("SearchContacts_SortAsc");
            await using MediaDbContext mediaContext = GetInMemoryMediaDbContext("SearchContacts_SortAsc");

            Contact contact1 = new()
            {
                ContactId = 1,
                ProgenyId = 1,
                FamilyId = 0,
                FirstName = "John",
                MiddleName = "",
                LastName = "Older",
                DisplayName = "",
                Email1 = "",
                Email2 = "",
                Context = "",
                Notes = "",
                Website = "",
                Tags = "",
                DateAdded = DateTime.UtcNow.AddDays(-10),
                CreatedTime = DateTime.UtcNow.AddDays(-10)
            };

            Contact contact2 = new()
            {
                ContactId = 2,
                ProgenyId = 1,
                FamilyId = 0,
                FirstName = "Johnny",
                MiddleName = "",
                LastName = "Newer",
                DisplayName = "",
                Email1 = "",
                Email2 = "",
                Context = "",
                Notes = "",
                Website = "",
                Tags = "",
                DateAdded = DateTime.UtcNow.AddDays(-1),
                CreatedTime = DateTime.UtcNow.AddDays(-1)
            };

            progenyContext.ContactsDb.AddRange(contact1, contact2);
            await progenyContext.SaveChangesAsync(TestContext.Current.CancellationToken);

            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(1, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Contact, It.IsAny<int>(), _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Contact, It.IsAny<int>(), 1, 0, _testUser, null))
                .ReturnsAsync(new TimelineItemPermission { PermissionLevel = PermissionLevel.View });

            SearchService service = new(progenyContext, mediaContext, _mockAccessManagementService.Object, _mockTimelineService.Object);

            SearchRequest request = new()
            {
                Query = "john",
                ProgenyIds = [1],
                Sort = 1
            };

            // Act
            SearchResponse<Contact> result = await service.SearchContacts(request, _testUser);

            // Assert
            Assert.Equal(2, result.Results.Count);
            Assert.Equal("Older", result.Results[0].LastName);
            Assert.Equal("Newer", result.Results[1].LastName);
        }

        [Fact]
        public async Task SearchContacts_Should_Use_CreatedTime_When_DateAdded_Is_Null()
        {
            // Arrange
            await using ProgenyDbContext progenyContext = GetInMemoryProgenyDbContext("SearchContacts_NullDateAdded");
            await using MediaDbContext mediaContext = GetInMemoryMediaDbContext("SearchContacts_NullDateAdded");

            Contact contact = new()
            {
                ContactId = 1,
                ProgenyId = 1,
                FamilyId = 0,
                FirstName = "John",
                MiddleName = "",
                LastName = "Doe",
                DisplayName = "",
                Email1 = "",
                Email2 = "",
                Context = "",
                Notes = "",
                Website = "",
                Tags = "",
                DateAdded = null,
                CreatedTime = DateTime.UtcNow.AddDays(-5)
            };

            progenyContext.ContactsDb.Add(contact);
            await progenyContext.SaveChangesAsync(TestContext.Current.CancellationToken);

            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(1, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Contact, 1, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Contact, 1, 1, 0, _testUser, null))
                .ReturnsAsync(new TimelineItemPermission { PermissionLevel = PermissionLevel.View });

            SearchService service = new(progenyContext, mediaContext, _mockAccessManagementService.Object, _mockTimelineService.Object);

            SearchRequest request = new()
            {
                Query = "john",
                ProgenyIds = [1]
            };

            // Act
            SearchResponse<Contact> result = await service.SearchContacts(request, _testUser);

            // Assert
            Assert.Single(result.Results);
        }

        [Fact]
        public async Task SearchContacts_Should_Return_Correct_TotalCount()
        {
            // Arrange
            await using ProgenyDbContext progenyContext = GetInMemoryProgenyDbContext("SearchContacts_TotalCount");
            await using MediaDbContext mediaContext = GetInMemoryMediaDbContext("SearchContacts_TotalCount");

            for (int i = 1; i <= 10; i++)
            {
                Contact contact = new()
                {
                    ContactId = i,
                    ProgenyId = 1,
                    FamilyId = 0,
                    FirstName = $"John{i}",
                    MiddleName = "",
                    LastName = "Doe",
                    DisplayName = "",
                    Email1 = "",
                    Email2 = "",
                    Context = "",
                    Notes = "",
                    Website = "",
                    Tags = "",
                    DateAdded = DateTime.UtcNow,
                    CreatedTime = DateTime.UtcNow
                };
                progenyContext.ContactsDb.Add(contact);
            }
            await progenyContext.SaveChangesAsync(TestContext.Current.CancellationToken);

            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(1, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Contact, It.IsAny<int>(), _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Contact, It.IsAny<int>(), 1, 0, _testUser, null))
                .ReturnsAsync(new TimelineItemPermission { PermissionLevel = PermissionLevel.View });

            SearchService service = new(progenyContext, mediaContext, _mockAccessManagementService.Object, _mockTimelineService.Object);

            SearchRequest request = new()
            {
                Query = "john",
                ProgenyIds = [1],
                NumberOfItems = 5
            };

            // Act
            SearchResponse<Contact> result = await service.SearchContacts(request, _testUser);

            // Assert
            Assert.Equal(10, result.TotalCount);
            Assert.Equal(5, result.Results.Count);
            Assert.Equal(5, result.RemainingItems);
        }

        [Fact]
        public async Task SearchContacts_Should_Not_Return_Contacts_With_Both_ProgenyId_And_FamilyId()
        {
            // Arrange
            await using ProgenyDbContext progenyContext = GetInMemoryProgenyDbContext("SearchContacts_ExclusiveIds");
            await using MediaDbContext mediaContext = GetInMemoryMediaDbContext("SearchContacts_ExclusiveIds");

            // Progeny contact (should be returned when searching by progeny)
            Contact progenyContact = new()
            {
                ContactId = 1,
                ProgenyId = 1,
                FamilyId = 0,
                FirstName = "ProgenyContact",
                MiddleName = "",
                LastName = "Test",
                DisplayName = "",
                Email1 = "",
                Email2 = "",
                Context = "",
                Notes = "",
                Website = "",
                Tags = "",
                DateAdded = DateTime.UtcNow,
                CreatedTime = DateTime.UtcNow
            };

            // Family contact (should be returned when searching by family)
            Contact familyContact = new()
            {
                ContactId = 2,
                ProgenyId = 0,
                FamilyId = 1,
                FirstName = "FamilyContact",
                MiddleName = "",
                LastName = "Test",
                DisplayName = "",
                Email1 = "",
                Email2 = "",
                Context = "",
                Notes = "",
                Website = "",
                Tags = "",
                DateAdded = DateTime.UtcNow,
                CreatedTime = DateTime.UtcNow
            };

            progenyContext.ContactsDb.AddRange(progenyContact, familyContact);
            await progenyContext.SaveChangesAsync(TestContext.Current.CancellationToken);

            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(1, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Contact, 1, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Contact, 1, 1, 0, _testUser, null))
                .ReturnsAsync(new TimelineItemPermission { PermissionLevel = PermissionLevel.View });

            SearchService service = new(progenyContext, mediaContext, _mockAccessManagementService.Object, _mockTimelineService.Object);

            SearchRequest request = new()
            {
                Query = "test",
                ProgenyIds = [1]
            };

            // Act
            SearchResponse<Contact> result = await service.SearchContacts(request, _testUser);

            // Assert
            Assert.Single(result.Results);
            Assert.Equal("ProgenyContact", result.Results[0].FirstName);
        }

        [Fact]
        public async Task SearchContacts_Should_Trim_Query()
        {
            // Arrange
            await using ProgenyDbContext progenyContext = GetInMemoryProgenyDbContext("SearchContacts_TrimQuery");
            await using MediaDbContext mediaContext = GetInMemoryMediaDbContext("SearchContacts_TrimQuery");

            Contact contact = new()
            {
                ContactId = 1,
                ProgenyId = 1,
                FamilyId = 0,
                FirstName = "John",
                MiddleName = "",
                LastName = "Doe",
                DisplayName = "",
                Email1 = "",
                Email2 = "",
                Context = "",
                Notes = "",
                Website = "",
                Tags = "",
                DateAdded = DateTime.UtcNow,
                CreatedTime = DateTime.UtcNow
            };

            progenyContext.ContactsDb.Add(contact);
            await progenyContext.SaveChangesAsync(TestContext.Current.CancellationToken);

            _mockAccessManagementService
                .Setup(x => x.HasProgenyPermission(1, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.HasItemPermission(KinaUnaTypes.TimeLineType.Contact, 1, _testUser, PermissionLevel.View))
                .ReturnsAsync(true);
            _mockAccessManagementService
                .Setup(x => x.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Contact, 1, 1, 0, _testUser, null))
                .ReturnsAsync(new TimelineItemPermission { PermissionLevel = PermissionLevel.View });

            SearchService service = new(progenyContext, mediaContext, _mockAccessManagementService.Object, _mockTimelineService.Object);

            SearchRequest request = new()
            {
                Query = "  john  ",
                ProgenyIds = [1]
            };

            // Act
            SearchResponse<Contact> result = await service.SearchContacts(request, _testUser);

            // Assert
            Assert.Single(result.Results);
        }

        #endregion
    }
}