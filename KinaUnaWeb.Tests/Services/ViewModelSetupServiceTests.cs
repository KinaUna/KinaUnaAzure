using KinaUna.Data.Models;
using KinaUnaWeb.Models;
using KinaUnaWeb.Services;
using KinaUnaWeb.Services.HttpClients;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Moq;

namespace KinaUnaWeb.Tests.Services
{
    public class ViewModelSetupServiceTests
    {
        private readonly Mock<IProgenyHttpClient> _progenyHttpClientMock = new();
        private readonly Mock<IUserInfosHttpClient> _userInfosHttpClientMock = new();
        private readonly Mock<IUserAccessHttpClient> _userAccessHttpClientMock = new();

        [Fact]
        public async Task SetupViewModel_Should_Return_BaseItemsViewModel_When_Parameters_Are_Valid()
        {

            UserInfo testUserInfo = new()
            {
                Id = 1,
                UserId = "abc123",
                UserEmail = "testuser@test.com"
            };
            _userInfosHttpClientMock.Setup(p => p.GetUserInfo("testuser@test.com")).ReturnsAsync(testUserInfo);


            Progeny testProgeny = new()
            {
                Id = 1,
                NickName = "Test"
            };
            _progenyHttpClientMock.Setup(p => p.GetProgeny(1)).ReturnsAsync(testProgeny);


            List<UserAccess> testAccessList = [];
            UserAccess testUserAccess = new()
            {
                AccessId = 1,
                AccessLevel = 1,
                ProgenyId = testProgeny.Id,
                UserId = testUserInfo.UserEmail
            };
            testAccessList.Add(testUserAccess);

            _userAccessHttpClientMock.Setup(p => p.GetProgenyAccessList(1)).ReturnsAsync(testAccessList);


            IOptions<MemoryDistributedCacheOptions> memoryCacheOptions = Options.Create(new MemoryDistributedCacheOptions());
            IDistributedCache memoryCache = new MemoryDistributedCache(memoryCacheOptions);


            ViewModelSetupService viewModelSetupService = new(_progenyHttpClientMock.Object, _userInfosHttpClientMock.Object, _userAccessHttpClientMock.Object, memoryCache);

            BaseItemsViewModel baseItemsViewModel = await viewModelSetupService.SetupViewModel(1, testUserInfo.UserEmail, testProgeny.Id);

            Assert.NotNull(baseItemsViewModel);
            Assert.Equal(testUserAccess.AccessLevel, baseItemsViewModel.CurrentAccessLevel);
            Assert.Equal(testProgeny.NickName, baseItemsViewModel.CurrentProgeny.NickName);
            Assert.Equal(testUserAccess.AccessId, baseItemsViewModel.CurrentProgenyAccessList.FirstOrDefault()?.AccessId);
            Assert.Equal(testUserInfo.Id, baseItemsViewModel.CurrentUser.Id);
        }

    }
}
