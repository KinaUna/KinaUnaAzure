using KinaUna.Data;
using KinaUna.Data.Models;
using KinaUna.Data.Models.AccessManagement;
using KinaUna.Data.Models.Family;
using KinaUnaWeb.Models;
using KinaUnaWeb.Services;
using KinaUnaWeb.Services.HttpClients;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Moq;
using Newtonsoft.Json;

namespace KinaUnaWeb.Tests.Services
{
    public class ViewModelSetupServiceTests
    {
        private readonly Mock<IProgenyHttpClient> _progenyHttpClientMock;
        private readonly Mock<IFamiliesHttpClient> _familiesHttpClientMock;
        private readonly Mock<IUserInfosHttpClient> _userInfosHttpClientMock;
        private readonly Mock<ITranslationsHttpClient> _translationsHttpClientMock;
        private readonly IDistributedCache _cache;
        private readonly ViewModelSetupService _service;

        public ViewModelSetupServiceTests()
        {
            _progenyHttpClientMock = new Mock<IProgenyHttpClient>();
            _familiesHttpClientMock = new Mock<IFamiliesHttpClient>();
            _userInfosHttpClientMock = new Mock<IUserInfosHttpClient>();
            _translationsHttpClientMock = new Mock<ITranslationsHttpClient>();

            IOptions<MemoryDistributedCacheOptions> memoryCacheOptions = Options.Create(new MemoryDistributedCacheOptions());
            _cache = new MemoryDistributedCache(memoryCacheOptions);

            _service = new ViewModelSetupService(
                _progenyHttpClientMock.Object,
                _familiesHttpClientMock.Object,
                _userInfosHttpClientMock.Object,
                _translationsHttpClientMock.Object,
                _cache
            );
        }

        #region SetupViewModel Tests

        [Fact]
        public async Task SetupViewModel_Should_Return_BaseItemsViewModel_With_UserInfo()
        {
            // Arrange
            const string userEmail = "testuser@test.com";
            const int languageId = 1;
            const int progenyId = 1;

            UserInfo testUserInfo = new()
            {
                Id = 1,
                UserId = "abc123",
                UserEmail = userEmail,
                ViewChild = 0
            };

            _userInfosHttpClientMock.Setup(x => x.GetUserInfo(userEmail)).ReturnsAsync(testUserInfo);

            Progeny testProgeny = new()
            {
                Id = progenyId,
                NickName = "TestChild",
                Name = "Test Child"
            };

            _progenyHttpClientMock.Setup(x => x.GetProgeny(progenyId)).ReturnsAsync(testProgeny);

            // Act
            BaseItemsViewModel result = await _service.SetupViewModel(languageId, userEmail, progenyId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(testUserInfo.Id, result.CurrentUser.Id);
            Assert.Equal(languageId, result.LanguageId);
            Assert.Equal(progenyId, result.CurrentProgenyId);
            Assert.Equal(testProgeny.NickName, result.CurrentProgeny.NickName);
        }

        [Fact]
        public async Task SetupViewModel_Should_Return_Cached_ViewModel_When_Cache_Exists()
        {
            // Arrange
            const string userEmail = "testuser@test.com";
            const int languageId = 1;
            const int progenyId = 1;
            const int familyId = 0;

            UserInfo testUserInfo = new()
            {
                Id = 1,
                UserId = "abc123",
                UserEmail = userEmail
            };

            BaseItemsViewModel cachedViewModel = new()
            {
                CurrentUser = testUserInfo,
                LanguageId = languageId,
                CurrentProgenyId = progenyId,
                CurrentFamilyId = familyId,
                CurrentProgeny = new Progeny { Id = progenyId, NickName = "CachedChild" }
            };

            string cacheKey = Constants.AppName + Constants.ApiVersion + "SetupViewModel_" + languageId + "_user_" + userEmail.ToUpper() + "_progeny_" + progenyId + "_family_" + familyId;
            await _cache.SetStringAsync(cacheKey, JsonConvert.SerializeObject(cachedViewModel), token: TestContext.Current.CancellationToken);

            _userInfosHttpClientMock.Setup(x => x.GetUserInfo(userEmail)).ReturnsAsync(testUserInfo);

            // Act
            BaseItemsViewModel result = await _service.SetupViewModel(languageId, userEmail, progenyId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("CachedChild", result.CurrentProgeny.NickName);
            _progenyHttpClientMock.Verify(x => x.GetProgeny(It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task SetupViewModel_Should_Set_CurrentProgeny_When_ProgenyId_Is_Valid()
        {
            // Arrange
            const string userEmail = "testuser@test.com";
            const int languageId = 1;
            const int progenyId = 5;

            UserInfo testUserInfo = new() { Id = 1, UserEmail = userEmail };
            Progeny testProgeny = new() { Id = progenyId, NickName = "TestProgeny" };

            _userInfosHttpClientMock.Setup(x => x.GetUserInfo(userEmail)).ReturnsAsync(testUserInfo);
            _progenyHttpClientMock.Setup(x => x.GetProgeny(progenyId)).ReturnsAsync(testProgeny);

            // Act
            BaseItemsViewModel result = await _service.SetupViewModel(languageId, userEmail, progenyId);

            // Assert
            Assert.NotNull(result.CurrentProgeny);
            Assert.Equal(progenyId, result.CurrentProgeny.Id);
            Assert.Equal("TestProgeny", result.CurrentProgeny.NickName);
        }

        [Fact]
        public async Task SetupViewModel_Should_Reset_ProgenyId_When_Progeny_Not_Found()
        {
            // Arrange
            const string userEmail = "testuser@test.com";
            const int languageId = 1;
            const int progenyId = 999;

            UserInfo testUserInfo = new() { Id = 1, UserEmail = userEmail };
            Progeny invalidProgeny = new() { Id = 0 };

            _userInfosHttpClientMock.Setup(x => x.GetUserInfo(userEmail)).ReturnsAsync(testUserInfo);
            _progenyHttpClientMock.Setup(x => x.GetProgeny(progenyId)).ReturnsAsync(invalidProgeny);

            // Act
            BaseItemsViewModel result = await _service.SetupViewModel(languageId, userEmail, progenyId);

            // Assert
            Assert.Equal(0, result.CurrentProgenyId);
        }

        [Fact]
        public async Task SetupViewModel_Should_Set_CurrentFamily_When_FamilyId_Is_Valid()
        {
            // Arrange
            const string userEmail = "testuser@test.com";
            const int languageId = 1;
            const int progenyId = 1;
            const int familyId = 5;

            UserInfo testUserInfo = new() { Id = 1, UserEmail = userEmail };
            Family testFamily = new() { FamilyId = familyId, Name = "TestFamily" };

            _userInfosHttpClientMock.Setup(x => x.GetUserInfo(userEmail)).ReturnsAsync(testUserInfo);
            _familiesHttpClientMock.Setup(x => x.GetFamily(familyId)).ReturnsAsync(testFamily);

            // Act
            BaseItemsViewModel result = await _service.SetupViewModel(languageId, userEmail, progenyId, familyId);

            // Assert
            Assert.NotNull(result.CurrentFamily);
            Assert.Equal(familyId, result.CurrentFamily.FamilyId);
            Assert.Equal("TestFamily", result.CurrentFamily.Name);
        }

        [Fact]
        public async Task SetupViewModel_Should_Reset_FamilyId_When_Family_Not_Found()
        {
            // Arrange
            const string userEmail = "testuser@test.com";
            const int languageId = 1;
            const int progenyId = 1;
            const int familyId = 999;

            UserInfo testUserInfo = new() { Id = 1, UserEmail = userEmail };
            Family invalidFamily = new() { FamilyId = 0 };

            _userInfosHttpClientMock.Setup(x => x.GetUserInfo(userEmail)).ReturnsAsync(testUserInfo);
            _familiesHttpClientMock.Setup(x => x.GetFamily(familyId)).ReturnsAsync(invalidFamily);

            // Act
            BaseItemsViewModel result = await _service.SetupViewModel(languageId, userEmail, progenyId, familyId);

            // Assert
            Assert.Equal(0, result.CurrentFamilyId);
        }

        [Fact]
        public async Task SetupViewModel_Should_Not_Call_UseViewChild_When_UseViewChild_Is_False()
        {
            // Arrange
            const string userEmail = "testuser@test.com";
            const int languageId = 1;
            const int progenyId = 1;

            Progeny progeny = new () { Id = progenyId, NickName = "TestProgeny" };
            UserInfo testUserInfo = new() { Id = 1, UserEmail = userEmail, ViewChild = 5 };

            _userInfosHttpClientMock.Setup(x => x.GetUserInfo(userEmail)).ReturnsAsync(testUserInfo);
            _progenyHttpClientMock.Setup(x => x.GetProgeny(progenyId)).ReturnsAsync(progeny);

            // Act
            BaseItemsViewModel result = await _service.SetupViewModel(languageId, userEmail, progenyId, 0, false);

            // Assert
            Assert.Equal(progenyId, result.CurrentProgenyId);
        }

        [Fact]
        public async Task SetupViewModel_Should_Cache_ViewModel_After_Creation()
        {
            // Arrange
            const string userEmail = "testuser@test.com";
            const int languageId = 1;
            const int progenyId = 1;
            const int familyId = 0;

            UserInfo testUserInfo = new() { Id = 1, UserEmail = userEmail };
            Progeny testProgeny = new() { Id = progenyId, NickName = "TestProgeny" };

            _userInfosHttpClientMock.Setup(x => x.GetUserInfo(userEmail)).ReturnsAsync(testUserInfo);
            _progenyHttpClientMock.Setup(x => x.GetProgeny(progenyId)).ReturnsAsync(testProgeny);

            // Act
            await _service.SetupViewModel(languageId, userEmail, progenyId);

            // Assert
            string cacheKey = Constants.AppName + Constants.ApiVersion + "SetupViewModel_" + languageId + "_user_" + userEmail.ToUpper() + "_progeny_" + progenyId + "_family_" + familyId;
            string cachedValue = (await _cache.GetStringAsync(cacheKey, token: TestContext.Current.CancellationToken))!;
            Assert.NotNull(cachedValue);
        }

        #endregion

        #region GetProgenySelectList Tests

        [Fact]
        public async Task GetProgenySelectList_Should_Return_Empty_List_When_No_Progenies()
        {
            // Arrange
            _progenyHttpClientMock.Setup(x => x.GetProgeniesUserCanAccess(PermissionLevel.Add))
                .ReturnsAsync([]);

            // Act
            List<SelectListItem> result = await _service.GetProgenySelectList();

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetProgenySelectList_Should_Return_List_Of_SelectListItems()
        {
            // Arrange
            List<Progeny> progenies =
            [
                new() { Id = 1, NickName = "Child1" },
                new() { Id = 2, NickName = "Child2" },
                new() { Id = 3, NickName = "Child3" }
            ];

            _progenyHttpClientMock.Setup(x => x.GetProgeniesUserCanAccess(PermissionLevel.Add))
                .ReturnsAsync(progenies);

            // Act
            List<SelectListItem> result = await _service.GetProgenySelectList();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(3, result.Count);
            Assert.Contains(result, x => x.Text == "Child1" && x.Value == "1");
            Assert.Contains(result, x => x.Text == "Child2" && x.Value == "2");
            Assert.Contains(result, x => x.Text == "Child3" && x.Value == "3");
        }

        [Fact]
        public async Task GetProgenySelectList_Should_Mark_Selected_Progeny()
        {
            // Arrange
            const int selectedId = 2;
            List<Progeny> progenies =
            [
                new() { Id = 1, NickName = "Child1" },
                new() { Id = 2, NickName = "Child2" },
                new() { Id = 3, NickName = "Child3" }
            ];

            _progenyHttpClientMock.Setup(x => x.GetProgeniesUserCanAccess(PermissionLevel.Add))
                .ReturnsAsync(progenies);

            // Act
            List<SelectListItem> result = await _service.GetProgenySelectList(selectedId);

            // Assert
            Assert.NotNull(result);
            SelectListItem selectedItem = result.FirstOrDefault(x => x.Value == selectedId.ToString())!;
            Assert.NotNull(selectedItem);
            Assert.True(selectedItem.Selected);
            Assert.Equal("Child2", selectedItem.Text);
        }

        [Fact]
        public async Task GetProgenySelectList_Should_Not_Mark_Any_Selected_When_Id_Is_Zero()
        {
            // Arrange
            List<Progeny> progenies =
            [
                new() { Id = 1, NickName = "Child1" },
                new() { Id = 2, NickName = "Child2" }
            ];

            _progenyHttpClientMock.Setup(x => x.GetProgeniesUserCanAccess(PermissionLevel.Add))
                .ReturnsAsync(progenies);

            // Act
            List<SelectListItem> result = await _service.GetProgenySelectList();

            // Assert
            Assert.NotNull(result);
            Assert.DoesNotContain(result, x => x.Selected);
        }

        #endregion

        #region GetFamilySelectList Tests

        [Fact]
        public async Task GetFamilySelectList_Should_Return_Empty_List_When_No_Families()
        {
            // Arrange
            _familiesHttpClientMock.Setup(x => x.GetFamiliesUserCanAccess(PermissionLevel.Add))
                .ReturnsAsync([]);

            // Act
            List<SelectListItem> result = await _service.GetFamilySelectList();

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetFamilySelectList_Should_Return_List_Of_SelectListItems()
        {
            // Arrange
            List<Family> families =
            [
                new() { FamilyId = 1, Name = "Family1" },
                new() { FamilyId = 2, Name = "Family2" },
                new() { FamilyId = 3, Name = "Family3" }
            ];

            _familiesHttpClientMock.Setup(x => x.GetFamiliesUserCanAccess(PermissionLevel.Add))
                .ReturnsAsync(families);

            // Act
            List<SelectListItem> result = await _service.GetFamilySelectList();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(3, result.Count);
            Assert.Contains(result, x => x.Text == "Family1" && x.Value == "1");
            Assert.Contains(result, x => x.Text == "Family2" && x.Value == "2");
            Assert.Contains(result, x => x.Text == "Family3" && x.Value == "3");
        }

        [Fact]
        public async Task GetFamilySelectList_Should_Mark_Selected_Family()
        {
            // Arrange
            const int selectedId = 2;
            List<Family> families =
            [
                new() { FamilyId = 1, Name = "Family1" },
                new() { FamilyId = 2, Name = "Family2" },
                new() { FamilyId = 3, Name = "Family3" }
            ];

            _familiesHttpClientMock.Setup(x => x.GetFamiliesUserCanAccess(PermissionLevel.Add))
                .ReturnsAsync(families);

            // Act
            List<SelectListItem> result = await _service.GetFamilySelectList(selectedId);

            // Assert
            Assert.NotNull(result);
            SelectListItem selectedItem = result.FirstOrDefault(x => x.Value == selectedId.ToString())!;
            Assert.NotNull(selectedItem);
            Assert.True(selectedItem.Selected);
            Assert.Equal("Family2", selectedItem.Text);
        }

        [Fact]
        public async Task GetFamilySelectList_Should_Not_Mark_Any_Selected_When_Id_Is_Zero()
        {
            // Arrange
            List<Family> families =
            [
                new() { FamilyId = 1, Name = "Family1" },
                new() { FamilyId = 2, Name = "Family2" }
            ];

            _familiesHttpClientMock.Setup(x => x.GetFamiliesUserCanAccess(PermissionLevel.Add))
                .ReturnsAsync(families);

            // Act
            List<SelectListItem> result = await _service.GetFamilySelectList();

            // Assert
            Assert.NotNull(result);
            Assert.DoesNotContain(result, x => x.Selected);
        }

        #endregion

        #region CreateReminderOffsetSelectListItems Tests

        [Fact]
        public async Task CreateReminderOffsetSelectListItems_Should_Return_8_Items()
        {
            // Arrange
            const int languageId = 1;
            _translationsHttpClientMock.Setup(x => x.GetTranslation("minutes before", PageNames.Calendar, languageId, false))
                .ReturnsAsync("minutes before");
            _translationsHttpClientMock.Setup(x => x.GetTranslation("hour before", PageNames.Calendar, languageId, false))
                .ReturnsAsync("hour before");
            _translationsHttpClientMock.Setup(x => x.GetTranslation("day before", PageNames.Calendar, languageId, false))
                .ReturnsAsync("day before");
            _translationsHttpClientMock.Setup(x => x.GetTranslation("Custom...", PageNames.Calendar, languageId, false))
                .ReturnsAsync("Custom...");

            // Act
            List<SelectListItem> result = await _service.CreateReminderOffsetSelectListItems(languageId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(8, result.Count);
        }

        [Fact]
        public async Task CreateReminderOffsetSelectListItems_Should_Have_30_Minutes_Selected_By_Default()
        {
            // Arrange
            const int languageId = 1;
            _translationsHttpClientMock.Setup(x => x.GetTranslation("minutes before", PageNames.Calendar, languageId, false))
                .ReturnsAsync("minutes before");
            _translationsHttpClientMock.Setup(x => x.GetTranslation("hour before", PageNames.Calendar, languageId, false))
                .ReturnsAsync("hour before");
            _translationsHttpClientMock.Setup(x => x.GetTranslation("day before", PageNames.Calendar, languageId, false))
                .ReturnsAsync("day before");
            _translationsHttpClientMock.Setup(x => x.GetTranslation("Custom...", PageNames.Calendar, languageId, false))
                .ReturnsAsync("Custom...");

            // Act
            List<SelectListItem> result = await _service.CreateReminderOffsetSelectListItems(languageId);

            // Assert
            SelectListItem selectedItem = result.FirstOrDefault(x => x.Selected)!;
            Assert.NotNull(selectedItem);
            Assert.Equal("30", selectedItem.Value);
        }

        [Fact]
        public async Task CreateReminderOffsetSelectListItems_Should_Have_Correct_Values()
        {
            // Arrange
            const int languageId = 1;
            _translationsHttpClientMock.Setup(x => x.GetTranslation("minutes before", PageNames.Calendar, languageId, false))
                .ReturnsAsync("minutes before");
            _translationsHttpClientMock.Setup(x => x.GetTranslation("hour before", PageNames.Calendar, languageId, false))
                .ReturnsAsync("hour before");
            _translationsHttpClientMock.Setup(x => x.GetTranslation("day before", PageNames.Calendar, languageId, false))
                .ReturnsAsync("day before");
            _translationsHttpClientMock.Setup(x => x.GetTranslation("Custom...", PageNames.Calendar, languageId, false))
                .ReturnsAsync("Custom...");

            // Act
            List<SelectListItem> result = await _service.CreateReminderOffsetSelectListItems(languageId);

            // Assert
            Assert.Contains(result, x => x.Value == "5");
            Assert.Contains(result, x => x.Value == "10");
            Assert.Contains(result, x => x.Value == "15");
            Assert.Contains(result, x => x.Value == "20");
            Assert.Contains(result, x => x.Value == "30");
            Assert.Contains(result, x => x.Value == "60");
            Assert.Contains(result, x => x.Value == "1440");
            Assert.Contains(result, x => x.Value == "0");
        }

        [Fact]
        public async Task CreateReminderOffsetSelectListItems_Should_Use_Translated_Text()
        {
            // Arrange
            const int languageId = 2;
            _translationsHttpClientMock.Setup(x => x.GetTranslation("minutes before", PageNames.Calendar, languageId, false))
                .ReturnsAsync("minutter før");
            _translationsHttpClientMock.Setup(x => x.GetTranslation("hour before", PageNames.Calendar, languageId, false))
                .ReturnsAsync("time før");
            _translationsHttpClientMock.Setup(x => x.GetTranslation("day before", PageNames.Calendar, languageId, false))
                .ReturnsAsync("dag før");
            _translationsHttpClientMock.Setup(x => x.GetTranslation("Custom...", PageNames.Calendar, languageId, false))
                .ReturnsAsync("Tilpasset...");

            // Act
            List<SelectListItem> result = await _service.CreateReminderOffsetSelectListItems(languageId);

            // Assert
            Assert.Contains(result, x => x.Text == "5 minutter før");
            Assert.Contains(result, x => x.Text == "1 time før");
            Assert.Contains(result, x => x.Text == "1 dag før");
            Assert.Contains(result, x => x.Text == "Tilpasset...");
        }

        [Fact]
        public async Task CreateReminderOffsetSelectListItems_Should_Have_Custom_Option_With_Value_Zero()
        {
            // Arrange
            const int languageId = 1;
            _translationsHttpClientMock.Setup(x => x.GetTranslation("minutes before", PageNames.Calendar, languageId, false))
                .ReturnsAsync("minutes before");
            _translationsHttpClientMock.Setup(x => x.GetTranslation("hour before", PageNames.Calendar, languageId, false))
                .ReturnsAsync("hour before");
            _translationsHttpClientMock.Setup(x => x.GetTranslation("day before", PageNames.Calendar, languageId, false))
                .ReturnsAsync("day before");
            _translationsHttpClientMock.Setup(x => x.GetTranslation("Custom...", PageNames.Calendar, languageId, false))
                .ReturnsAsync("Custom...");

            // Act
            List<SelectListItem> result = await _service.CreateReminderOffsetSelectListItems(languageId);

            // Assert
            SelectListItem customItem = result.FirstOrDefault(x => x.Value == "0")!;
            Assert.NotNull(customItem);
            Assert.Equal("Custom...", customItem.Text);
        }

        #endregion
    }
}