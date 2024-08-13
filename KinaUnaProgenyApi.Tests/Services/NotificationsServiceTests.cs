using KinaUna.Data;
using KinaUna.Data.Contexts;
using KinaUna.Data.Models;
using KinaUnaProgenyApi.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace KinaUnaProgenyApi.Tests.Services
{
    public class NotificationsServiceTests
    {
        [Fact]
        public async Task GetMobileNotification_Should_Return_MobileNotification_Object_When_Id_Is_Valid()
        {
            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>().UseInMemoryDatabase("GetMobileNotification_Should_Return_MobileNotification_Object_When_Id_Is_Valid").Options;
            await using ProgenyDbContext context = new(dbOptions);

            MobileNotification notification1 = new()
            { Title = "Test1", UserId = "User1", IconLink = Constants.ProfilePictureUrl, ItemId = "1", ItemType = 1,
                Language = "EN", Message = "Test message 1", Read = false, Time = DateTime.UtcNow};
            
            MobileNotification notification2 = new()
            {
                Title = "Test2",
                UserId = "User1",
                IconLink = Constants.ProfilePictureUrl,
                ItemId = "1",
                ItemType = 1,
                Language = "EN",
                Message = "Test message 2",
                Read = false,
                Time = DateTime.UtcNow
            };
            context.Add(notification1);
            context.Add(notification2);
            await context.SaveChangesAsync();

            IOptions<MemoryDistributedCacheOptions> memoryCacheOptions = Options.Create(new MemoryDistributedCacheOptions());
            IDistributedCache memoryCache = new MemoryDistributedCache(memoryCacheOptions);

            NotificationsService notificationsService = new(context, memoryCache);

            MobileNotification resultNotification1 = await notificationsService.GetMobileNotification(1);
            
            Assert.NotNull(resultNotification1);
            Assert.IsType<MobileNotification>(resultNotification1);
            Assert.Equal(notification1.UserId, resultNotification1.UserId);
            Assert.Equal(notification1.Title, resultNotification1.Title);
            Assert.Equal(notification1.Message, resultNotification1.Message);
            Assert.Equal(notification1.Read, resultNotification1.Read);
        }

        [Fact]
        public async Task GetMobileNotification_Should_Return_Null_When_Id_Is_Invalid()
        {
            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>().UseInMemoryDatabase("GetMobileNotification_Should_Return_Null_When_Id_Is_Invalid").Options;
            await using ProgenyDbContext context = new(dbOptions);

            MobileNotification notification1 = new()
            {
                Title = "Test1",
                UserId = "User1",
                IconLink = Constants.ProfilePictureUrl,
                ItemId = "1",
                ItemType = 1,
                Language = "EN",
                Message = "Test message 1",
                Read = false,
                Time = DateTime.UtcNow
            };

            MobileNotification notification2 = new()
            {
                Title = "Test2",
                UserId = "User1",
                IconLink = Constants.ProfilePictureUrl,
                ItemId = "1",
                ItemType = 1,
                Language = "EN",
                Message = "Test message 2",
                Read = false,
                Time = DateTime.UtcNow
            };
            context.Add(notification1);
            context.Add(notification2);
            await context.SaveChangesAsync();

            IOptions<MemoryDistributedCacheOptions> memoryCacheOptions = Options.Create(new MemoryDistributedCacheOptions());
            IDistributedCache memoryCache = new MemoryDistributedCache(memoryCacheOptions);

            NotificationsService notificationsService = new(context, memoryCache);

            MobileNotification resultNotification1 = await notificationsService.GetMobileNotification(3);
            
            Assert.Null(resultNotification1);
        }

        [Fact]
        public async Task AddMobileNotification_Should_Save_MobileNotification()
        {
            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>().UseInMemoryDatabase("AddMobileNotification_Should_Save_MobileNotification").Options;
            await using ProgenyDbContext context = new(dbOptions);

            MobileNotification notification1 = new()
            {
                Title = "Test1",
                UserId = "User1",
                IconLink = Constants.ProfilePictureUrl,
                ItemId = "1",
                ItemType = 1,
                Language = "EN",
                Message = "Test message 1",
                Read = false,
                Time = DateTime.UtcNow
            };
            
            context.Add(notification1);
            await context.SaveChangesAsync();

            
            IOptions<MemoryDistributedCacheOptions> memoryCacheOptions = Options.Create(new MemoryDistributedCacheOptions());
            IDistributedCache memoryCache = new MemoryDistributedCache(memoryCacheOptions);

            NotificationsService notificationsService = new(context, memoryCache);

            MobileNotification notificationToAdd = new()
            {
                Title = "Test2",
                UserId = "User1",
                IconLink = Constants.ProfilePictureUrl,
                ItemId = "1",
                ItemType = 1,
                Language = "EN",
                Message = "Test message 2",
                Read = false,
                Time = DateTime.UtcNow
            };

            MobileNotification addedNotification = await notificationsService.AddMobileNotification(notificationToAdd);
            MobileNotification? dbNotification = await context.MobileNotificationsDb.AsNoTracking().SingleOrDefaultAsync(mn => mn.NotificationId == addedNotification.NotificationId);
            MobileNotification savedNotification = await notificationsService.GetMobileNotification(addedNotification.NotificationId);

            Assert.NotNull(addedNotification);
            Assert.IsType<MobileNotification>(addedNotification);
            Assert.Equal(notificationToAdd.UserId, addedNotification.UserId);
            Assert.Equal(notificationToAdd.Title, addedNotification.Title);
            Assert.Equal(notificationToAdd.Message, addedNotification.Message);
            Assert.Equal(notificationToAdd.Read, addedNotification.Read);

            if (dbNotification != null)
            {
                Assert.IsType<MobileNotification>(dbNotification);
                Assert.Equal(notificationToAdd.UserId, dbNotification.UserId);
                Assert.Equal(notificationToAdd.Title, dbNotification.Title);
                Assert.Equal(notificationToAdd.Message, dbNotification.Message);
                Assert.Equal(notificationToAdd.Read, dbNotification.Read);
            }
            Assert.NotNull(savedNotification);
            Assert.IsType<MobileNotification>(savedNotification);
            Assert.Equal(notificationToAdd.UserId, savedNotification.UserId);
            Assert.Equal(notificationToAdd.Title, savedNotification.Title);
            Assert.Equal(notificationToAdd.Message, savedNotification.Message);
            Assert.Equal(notificationToAdd.Read, savedNotification.Read);

        }

        [Fact]
        public async Task UpdateMobileNotification_Should_Save_MobileNotification()
        {
            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>().UseInMemoryDatabase("UpdateMobileNotification_Should_Save_MobileNotification").Options;
            await using ProgenyDbContext context = new(dbOptions);

            MobileNotification notification1 = new()
            {
                Title = "Test1",
                UserId = "User1",
                IconLink = Constants.ProfilePictureUrl,
                ItemId = "1",
                ItemType = 1,
                Language = "EN",
                Message = "Test message 1",
                Read = false,
                Time = DateTime.UtcNow
            };

            MobileNotification notification2 = new()
            {
                Title = "Test2",
                UserId = "User1",
                IconLink = Constants.ProfilePictureUrl,
                ItemId = "1",
                ItemType = 1,
                Language = "EN",
                Message = "Test message 2",
                Read = false,
                Time = DateTime.UtcNow
            };
            context.Add(notification1);
            context.Add(notification2);
            await context.SaveChangesAsync();

            IOptions<MemoryDistributedCacheOptions> memoryCacheOptions = Options.Create(new MemoryDistributedCacheOptions());
            IDistributedCache memoryCache = new MemoryDistributedCache(memoryCacheOptions);

            NotificationsService notificationsService = new(context, memoryCache);

            MobileNotification notificationToUpdate = await notificationsService.GetMobileNotification(1);
            notificationToUpdate.Read = true;
            MobileNotification updatedNotification = await notificationsService.UpdateMobileNotification(notificationToUpdate);
            MobileNotification? dbNotification = await context.MobileNotificationsDb.AsNoTracking().SingleOrDefaultAsync(mn => mn.NotificationId == 1);
            MobileNotification savedNotification = await notificationsService.GetMobileNotification(1);

            Assert.NotNull(updatedNotification);
            Assert.IsType<MobileNotification>(updatedNotification);
            Assert.NotEqual(0, updatedNotification.NotificationId);
            Assert.Equal("User1", updatedNotification.UserId);
            Assert.True(updatedNotification.Read);
            
            if (dbNotification != null)
            {
                Assert.IsType<MobileNotification>(dbNotification);
                Assert.NotEqual(0, dbNotification.NotificationId);
                Assert.NotEqual(0, dbNotification.NotificationId);
                Assert.Equal("User1", dbNotification.UserId);
                Assert.True(dbNotification.Read);
            }

            Assert.NotNull(savedNotification);
            Assert.IsType<MobileNotification>(savedNotification);
            Assert.NotEqual(0, savedNotification.NotificationId);
            Assert.NotEqual(0, savedNotification.NotificationId);
            Assert.Equal("User1", savedNotification.UserId);
            Assert.True(savedNotification.Read);
        }

        [Fact]
        public async Task DeleteMobileNotification_Should_Remove_MobileNotification()
        {
            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>().UseInMemoryDatabase("DeleteMobileNotification_Should_Remove_MobileNotification").Options;
            await using ProgenyDbContext context = new(dbOptions);

            MobileNotification notification1 = new()
            {
                Title = "Test1",
                UserId = "User1",
                IconLink = Constants.ProfilePictureUrl,
                ItemId = "1",
                ItemType = 1,
                Language = "EN",
                Message = "Test message 1",
                Read = false,
                Time = DateTime.UtcNow
            };

            MobileNotification notification2 = new()
            {
                Title = "Test2",
                UserId = "User1",
                IconLink = Constants.ProfilePictureUrl,
                ItemId = "1",
                ItemType = 1,
                Language = "EN",
                Message = "Test message 2",
                Read = false,
                Time = DateTime.UtcNow
            };
            context.Add(notification1);
            context.Add(notification2);
            await context.SaveChangesAsync();

            IOptions<MemoryDistributedCacheOptions> memoryCacheOptions = Options.Create(new MemoryDistributedCacheOptions());
            IDistributedCache memoryCache = new MemoryDistributedCache(memoryCacheOptions);

            NotificationsService notificationsService = new(context, memoryCache);

            int notificationItemsCountBeforeDelete = context.MobileNotificationsDb.Count();
            MobileNotification notificationToDelete = await notificationsService.GetMobileNotification(1);

            await notificationsService.DeleteMobileNotification(notificationToDelete);
            MobileNotification? deletedNotification = await context.MobileNotificationsDb.SingleOrDefaultAsync(mn => mn.NotificationId == 1);
            int notificationItemsCountAfterDelete = context.MobileNotificationsDb.Count();

            Assert.Null(deletedNotification);
            Assert.Equal(2, notificationItemsCountBeforeDelete);
            Assert.Equal(1, notificationItemsCountAfterDelete);
        }

        [Fact]
        public async Task GetUsersMobileNotifications_Should_Return_List_Of_MobileNotifications_When_User_Has_Saved_MobileNotifications()
        {
            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>().UseInMemoryDatabase("GetUsersMobileNotifications_Should_Return_List_Of_MobileNotifications_When_User_Has_Saved_MobileNotifications").Options;
            await using ProgenyDbContext context = new(dbOptions);

            MobileNotification notification1 = new()
            {
                Title = "Test1",
                UserId = "User1",
                IconLink = Constants.ProfilePictureUrl,
                ItemId = "1",
                ItemType = 1,
                Language = "EN",
                Message = "Test message 1",
                Read = false,
                Time = DateTime.UtcNow
            };

            MobileNotification notification2 = new()
            {
                Title = "Test2",
                UserId = "User1",
                IconLink = Constants.ProfilePictureUrl,
                ItemId = "1",
                ItemType = 1,
                Language = "EN",
                Message = "Test message 2",
                Read = false,
                Time = DateTime.UtcNow
            };
            context.Add(notification1);
            context.Add(notification2);
            await context.SaveChangesAsync();

            IOptions<MemoryDistributedCacheOptions> memoryCacheOptions = Options.Create(new MemoryDistributedCacheOptions());
            IDistributedCache memoryCache = new MemoryDistributedCache(memoryCacheOptions);

            NotificationsService notificationsService = new(context, memoryCache);

            List<MobileNotification> notificationsList = await notificationsService.GetUsersMobileNotifications("User1", "EN");
            MobileNotification firstNotification = notificationsList.First();

            Assert.NotNull(notificationsList);
            Assert.IsType<List<MobileNotification>>(notificationsList);
            Assert.Equal(2, notificationsList.Count);
            Assert.NotNull(firstNotification);
            Assert.IsType<MobileNotification>(firstNotification);

        }

        [Fact]
        public async Task GetUsersMobileNotifications_Should_Return_Empty_List_Of_MobileNotifications_When_User_Has_No_Saved_MobileNotifications()
        {
            
            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>().UseInMemoryDatabase("GetUsersMobileNotifications_Should_Return_Empty_List_Of_MobileNotifications_When_User_Has_No_Saved_MobileNotifications").Options;
            await using ProgenyDbContext context = new(dbOptions);

            MobileNotification notification1 = new()
            {
                Title = "Test1",
                UserId = "User1",
                IconLink = Constants.ProfilePictureUrl,
                ItemId = "1",
                ItemType = 1,
                Language = "EN",
                Message = "Test message 1",
                Read = false,
                Time = DateTime.UtcNow
            };

            MobileNotification notification2 = new()
            {
                Title = "Test2",
                UserId = "User1",
                IconLink = Constants.ProfilePictureUrl,
                ItemId = "1",
                ItemType = 1,
                Language = "EN",
                Message = "Test message 2",
                Read = false,
                Time = DateTime.UtcNow
            };

            context.Add(notification1);
            context.Add(notification2);
            await context.SaveChangesAsync();

            IOptions<MemoryDistributedCacheOptions> memoryCacheOptions = Options.Create(new MemoryDistributedCacheOptions());
            IDistributedCache memoryCache = new MemoryDistributedCache(memoryCacheOptions);

            NotificationsService notificationsService = new(context, memoryCache);

            List<MobileNotification> contactsList = await notificationsService.GetUsersMobileNotifications("NoUser", "EN");
            
            Assert.NotNull(contactsList);
            Assert.IsType<List<MobileNotification>>(contactsList);
            Assert.Empty(contactsList);
        }

        [Fact]
        public async Task GetPushDeviceById_Should_Return_PushDevices_Object_When_Id_Is_Valid()
        {
            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>().UseInMemoryDatabase("GetPushDeviceById_Should_Return_PushDevices_Object_When_Id_Is_Valid").Options;
            await using ProgenyDbContext context = new(dbOptions);
            
            IOptions<MemoryDistributedCacheOptions> memoryCacheOptions = Options.Create(new MemoryDistributedCacheOptions());
            IDistributedCache memoryCache = new MemoryDistributedCache(memoryCacheOptions);

            NotificationsService notificationsService = new(context, memoryCache);

            PushDevices pushDevice1 = new()
            {
                Name = "PushDevice1",
                PushAuth = "",
                PushEndpoint = "",
                PushP256DH = ""
            };

            context.Add(pushDevice1);
            await context.SaveChangesAsync();
            PushDevices resultPushDevice1 = await notificationsService.GetPushDeviceById(1);
            
            Assert.NotNull(resultPushDevice1);
            Assert.IsType<PushDevices>(resultPushDevice1);
            Assert.Equal(pushDevice1.Name, resultPushDevice1.Name);
        }

        [Fact]
        public async Task GetPushDeviceById_Should_Return_Null_When_Id_Is_Invalid()
        {
            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>().UseInMemoryDatabase("GetPushDeviceById_Should_Return_Null_When_Id_Is_Invalid").Options;
            await using ProgenyDbContext context = new(dbOptions);
            
            IOptions<MemoryDistributedCacheOptions> memoryCacheOptions = Options.Create(new MemoryDistributedCacheOptions());
            IDistributedCache memoryCache = new MemoryDistributedCache(memoryCacheOptions);

            NotificationsService notificationsService = new(context, memoryCache);

            PushDevices pushDevice1 = new()
            {
                Name = "PushDevice1",
                PushAuth = "",
                PushEndpoint = "",
                PushP256DH = ""
            };

            context.Add(pushDevice1);
            await context.SaveChangesAsync();
            PushDevices resultPushDevice2 = await notificationsService.GetPushDeviceById(2);

            Assert.Null(resultPushDevice2);
        }

        [Fact]
        public async Task GetPushDevice_Should_Return_PushDevices_Object_When_Parameter_Is_Valid()
        {
            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>().UseInMemoryDatabase("GetPushDevice_Should_Return_PushDevices_Object_When_Parameter_Is_Valid").Options;
            await using ProgenyDbContext context = new(dbOptions);

            IOptions<MemoryDistributedCacheOptions> memoryCacheOptions = Options.Create(new MemoryDistributedCacheOptions());
            IDistributedCache memoryCache = new MemoryDistributedCache(memoryCacheOptions);

            NotificationsService notificationsService = new(context, memoryCache);

            PushDevices pushDevice1 = new()
            {
                Name = "PushDevice1",
                PushAuth = "Auth1",
                PushEndpoint = "EndPoint1",
                PushP256DH = "P2256DH1"
            };

            context.Add(pushDevice1);
            await context.SaveChangesAsync();

            PushDevices requestPushDevice1 = new()
            {
                Name = "PushDevice1",
                PushAuth = "Auth1",
                PushEndpoint = "EndPoint1",
                PushP256DH = "P2256DH1"
            };

            PushDevices resultPushDevice1 = await notificationsService.GetPushDevice(requestPushDevice1);

            Assert.NotNull(resultPushDevice1);
            Assert.IsType<PushDevices>(resultPushDevice1);
            Assert.Equal(pushDevice1.Name, resultPushDevice1.Name);
            Assert.Equal(pushDevice1.PushAuth, resultPushDevice1.PushAuth);
            Assert.Equal(pushDevice1.PushEndpoint, resultPushDevice1.PushEndpoint);
            Assert.Equal(pushDevice1.PushP256DH, resultPushDevice1.PushP256DH);
            Assert.Equal(pushDevice1.Id, resultPushDevice1.Id);
        }

        [Fact]
        public async Task GetPushDevice_Should_Return_Null_When_Parameter_Is_Invalid()
        {
            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>().UseInMemoryDatabase("GetPushDevice_Should_Return_Null_When_Parameter_Is_Invalid").Options;
            await using ProgenyDbContext context = new(dbOptions);

            IOptions<MemoryDistributedCacheOptions> memoryCacheOptions = Options.Create(new MemoryDistributedCacheOptions());
            IDistributedCache memoryCache = new MemoryDistributedCache(memoryCacheOptions);

            NotificationsService notificationsService = new(context, memoryCache);

            PushDevices pushDevice1 = new()
            {
                Name = "PushDevice1",
                PushAuth = "Auth1",
                PushEndpoint = "EndPoint1",
                PushP256DH = "P2256DH1"
            };

            context.Add(pushDevice1);
            await context.SaveChangesAsync();

            PushDevices requestPushDevice1 = new()
            {
                Name = "PushDevice2",
                PushAuth = "Auth2",
                PushEndpoint = "EndPoint2",
                PushP256DH = "P2256DH2"
            };

            PushDevices resultPushDevice1 = await notificationsService.GetPushDevice(requestPushDevice1);

            Assert.Null(resultPushDevice1);
        }

        [Fact]
        public async Task GetPushDeviceByUserId_Should_Return_List_of_PushDevices_When_UserId_Is_Valid()
        {
            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>().UseInMemoryDatabase("GetPushDeviceByUserId_Should_Return_List_of_PushDevices_When_UserId_Is_Valid").Options;
            await using ProgenyDbContext context = new(dbOptions);

            IOptions<MemoryDistributedCacheOptions> memoryCacheOptions = Options.Create(new MemoryDistributedCacheOptions());
            IDistributedCache memoryCache = new MemoryDistributedCache(memoryCacheOptions);

            NotificationsService notificationsService = new(context, memoryCache);

            PushDevices pushDevice1 = new()
            {
                Name = "PushDevice1",
                PushAuth = "Auth1",
                PushEndpoint = "Endpoint1",
                PushP256DH = "P256DH1"
            };

            PushDevices pushDevice2 = new()
            {
                Name = "PushDevice1",
                PushAuth = "Auth2",
                PushEndpoint = "Endpoint2",
                PushP256DH = "P256DH2"
            };

            context.Add(pushDevice1);
            context.Add(pushDevice2);
            await context.SaveChangesAsync();

            List<PushDevices> resultPushDevicesList1 = await notificationsService.GetPushDevicesListByUserId("PushDevice1");

            Assert.NotNull(resultPushDevicesList1);
            Assert.Equal(context.PushDevices.Count(), resultPushDevicesList1.Count);
            Assert.NotNull(resultPushDevicesList1.FirstOrDefault());
            Assert.IsType<PushDevices>(resultPushDevicesList1.FirstOrDefault());
            Assert.Equal(pushDevice1.Name, resultPushDevicesList1.FirstOrDefault()!.Name);

        }

        [Fact]
        public async Task GetPushDeviceByUSerId_Should_Return_Empty_List_When_UserId_Is_Invalid()
        {
            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>().UseInMemoryDatabase("GetPushDeviceByUSerId_Should_Return_Empty_List_When_UserId_Is_Invalid").Options;
            await using ProgenyDbContext context = new(dbOptions);

            IOptions<MemoryDistributedCacheOptions> memoryCacheOptions = Options.Create(new MemoryDistributedCacheOptions());
            IDistributedCache memoryCache = new MemoryDistributedCache(memoryCacheOptions);

            NotificationsService notificationsService = new(context, memoryCache);

            PushDevices pushDevice1 = new()
            {
                Name = "PushDevice1",
                PushAuth = "Auth1",
                PushEndpoint = "Endpoint1",
                PushP256DH = "P256DH1"
            };

            PushDevices pushDevice2 = new()
            {
                Name = "PushDevice1",
                PushAuth = "Auth2",
                PushEndpoint = "Endpoint2",
                PushP256DH = "P256DH2"
            };

            context.Add(pushDevice1);
            context.Add(pushDevice2);
            await context.SaveChangesAsync();

            List<PushDevices> resultPushDevicesList2 = await notificationsService.GetPushDevicesListByUserId("PushDevice2");

            Assert.Empty(resultPushDevicesList2);
        }

        [Fact]
        public async Task GetAllPushDevices_Should_Return_List_of_PushDevices()
        {
            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>().UseInMemoryDatabase("GetAllPushDevices_Should_Return_List_of_PushDevices").Options;
            await using ProgenyDbContext context = new(dbOptions);

            IOptions<MemoryDistributedCacheOptions> memoryCacheOptions = Options.Create(new MemoryDistributedCacheOptions());
            IDistributedCache memoryCache = new MemoryDistributedCache(memoryCacheOptions);

            NotificationsService notificationsService = new(context, memoryCache);

            PushDevices pushDevice1 = new()
            {
                Name = "PushDevice1",
                PushAuth = "Auth1",
                PushEndpoint = "Endpoint1",
                PushP256DH = "P256DH1"
            };

            PushDevices pushDevice2 = new()
            {
                Name = "PushDevice1",
                PushAuth = "Auth2",
                PushEndpoint = "Endpoint2",
                PushP256DH = "P256DH2"
            };

            context.Add(pushDevice1);
            context.Add(pushDevice2);
            await context.SaveChangesAsync();

            List<PushDevices> resultPushDevicesList1 = await notificationsService.GetAllPushDevices();

            Assert.NotNull(resultPushDevicesList1);
            Assert.Equal(context.PushDevices.Count(), resultPushDevicesList1.Count);
            Assert.NotNull(resultPushDevicesList1.FirstOrDefault());
            Assert.IsType<PushDevices>(resultPushDevicesList1.FirstOrDefault());
            Assert.Equal(pushDevice1.Name, resultPushDevicesList1.FirstOrDefault()!.Name);

        }

        [Fact]
        public async Task AddPushDevice_Should_Save_PushDevices_Object()
        {
            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>().UseInMemoryDatabase("AddPushDevice_Should_Save_PushDevices_Object").Options;
            await using ProgenyDbContext context = new(dbOptions);

            IOptions<MemoryDistributedCacheOptions> memoryCacheOptions = Options.Create(new MemoryDistributedCacheOptions());
            IDistributedCache memoryCache = new MemoryDistributedCache(memoryCacheOptions);

            NotificationsService notificationsService = new(context, memoryCache);

            PushDevices pushDevice1 = new()
            {
                Name = "PushDevice1",
                PushAuth = "Auth1",
                PushEndpoint = "EndPoint1",
                PushP256DH = "P2256DH1"
            };

            context.Add(pushDevice1);
            await context.SaveChangesAsync();

            PushDevices addPushDevice1 = new()
            {
                Name = "PushDevice2",
                PushAuth = "Auth2",
                PushEndpoint = "EndPoint2",
                PushP256DH = "P2256DH2"
            };

            PushDevices addedPushDevice = await notificationsService.AddPushDevice(addPushDevice1);

            PushDevices resultPushDevice1 = await notificationsService.GetPushDeviceById(addedPushDevice.Id);

            Assert.NotNull(addedPushDevice);
            Assert.IsType<PushDevices>(addedPushDevice);
            Assert.NotNull(resultPushDevice1);
            Assert.IsType<PushDevices>(resultPushDevice1);
            Assert.Equal(addPushDevice1.Name, resultPushDevice1.Name);
            Assert.Equal(addPushDevice1.PushAuth, resultPushDevice1.PushAuth);
            Assert.Equal(addPushDevice1.PushEndpoint, resultPushDevice1.PushEndpoint);
            Assert.Equal(addPushDevice1.PushP256DH, resultPushDevice1.PushP256DH);
            Assert.Equal(addPushDevice1.Id, resultPushDevice1.Id);
        }

        [Fact]
        public async Task RemovePushDevice_Should_Remove_PushDevices_Object()
        {
            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>().UseInMemoryDatabase("RemovePushDevice_Should_Remove_PushDevices_Object").Options;
            await using ProgenyDbContext context = new(dbOptions);

            IOptions<MemoryDistributedCacheOptions> memoryCacheOptions = Options.Create(new MemoryDistributedCacheOptions());
            IDistributedCache memoryCache = new MemoryDistributedCache(memoryCacheOptions);

            NotificationsService notificationsService = new(context, memoryCache);

            PushDevices pushDevice1 = new()
            {
                Name = "PushDevice1",
                PushAuth = "Auth1",
                PushEndpoint = "EndPoint1",
                PushP256DH = "P2256DH1"
            };

            PushDevices pushDevice2 = new()
            {
                Name = "PushDevice2",
                PushAuth = "Auth2",
                PushEndpoint = "EndPoint2",
                PushP256DH = "P2256DH2"
            };

            context.Add(pushDevice1);
            context.Add(pushDevice2);
            await context.SaveChangesAsync();

            int deviceBeforeRemove = context.PushDevices.Count();
            await notificationsService.RemovePushDevice(pushDevice1);
            int deviceAfterRemove = context.PushDevices.Count();

            Assert.Equal(2, deviceBeforeRemove);
            Assert.Equal(1, deviceAfterRemove);
        }

        [Fact]
        public async Task GetWebNotificationById_Should_Return_WebNotification_Object_When_Id_Is_Valid()
        {
            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>().UseInMemoryDatabase("GetWebNotificationById_Should_Return_WebNotification_Object_When_Id_Is_Valid").Options;
            await using ProgenyDbContext context = new(dbOptions);

            IOptions<MemoryDistributedCacheOptions> memoryCacheOptions = Options.Create(new MemoryDistributedCacheOptions());
            IDistributedCache memoryCache = new MemoryDistributedCache(memoryCacheOptions);

            NotificationsService notificationsService = new(context, memoryCache);

            WebNotification webNotification1 = new()
            {
                Title = "Title1",
                DateTime = new(2022, 1, 1, 11, 11, 11),
                From = "From1",
                To = "To1",
                Message = "Message1",
                Link = "Link1",
                Icon = "Icon1"
            };

            context.Add(webNotification1);
            await context.SaveChangesAsync();

            WebNotification resultWebNotification1 = await notificationsService.GetWebNotificationById(1);

            Assert.NotNull(resultWebNotification1);
            Assert.IsType<WebNotification>(resultWebNotification1);
            Assert.Equal(webNotification1.Title, resultWebNotification1.Title);
            Assert.Equal(webNotification1.From, resultWebNotification1.From);
            Assert.Equal(webNotification1.To, resultWebNotification1.To);

        }

        [Fact]
        public async Task GetWebNotificationById_Should_Return_Null_Object_When_Id_Is_Invalid()
        {
            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>().UseInMemoryDatabase("GetWebNotificationById_Should_Return_Null_Object_When_Id_Is_Invalid").Options;
            await using ProgenyDbContext context = new(dbOptions);

            IOptions<MemoryDistributedCacheOptions> memoryCacheOptions = Options.Create(new MemoryDistributedCacheOptions());
            IDistributedCache memoryCache = new MemoryDistributedCache(memoryCacheOptions);

            NotificationsService notificationsService = new(context, memoryCache);

            WebNotification webNotification1 = new()
            {
                Title = "Title1",
                DateTime = new(2022, 1, 1, 11, 11, 11),
                From = "From1",
                To = "To1",
                Message = "Message1",
                Link = "Link1",
                Icon = "Icon1"
            };

            context.Add(webNotification1);
            await context.SaveChangesAsync();

            WebNotification resultWebNotification1 = await notificationsService.GetWebNotificationById(2);

            Assert.Null(resultWebNotification1);
        }

        [Fact]
        public async Task GetUsersWebNotifications_Should_Return_List_of_WebNotification_When_UserId_Is_Valid()
        {
            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>().UseInMemoryDatabase("GetUsersWebNotifications_Should_Return_List_of_WebNotification_When_UserId_Is_Valid").Options;
            await using ProgenyDbContext context = new(dbOptions);

            IOptions<MemoryDistributedCacheOptions> memoryCacheOptions = Options.Create(new MemoryDistributedCacheOptions());
            IDistributedCache memoryCache = new MemoryDistributedCache(memoryCacheOptions);

            NotificationsService notificationsService = new(context, memoryCache);

            WebNotification webNotification1 = new()
            {
                Title = "Title1",
                DateTime = new(2022, 1, 1, 11, 11, 11),
                From = "From1",
                To = "To1",
                Message = "Message1",
                Link = "Link1",
                Icon = "Icon1"
            };

            WebNotification webNotification2 = new()
            {
                Title = "Title2",
                DateTime = new(2022, 2, 2, 22, 22, 22),
                From = "From1",
                To = "To1",
                Message = "Message2",
                Link = "Link2",
                Icon = "Icon2"
            };

            WebNotification webNotification3 = new()
            {
                Title = "Title3",
                DateTime = new(2022, 3, 3, 11, 33, 33),
                From = "From1",
                To = "To2",
                Message = "Message3",
                Link = "Link3",
                Icon = "Icon3"
            };

            context.Add(webNotification1);
            context.Add(webNotification2);
            context.Add(webNotification3);
            await context.SaveChangesAsync();

            List<WebNotification> resultWebNotificationsList1 = await notificationsService.GetUsersWebNotifications("To1");

            Assert.NotNull(resultWebNotificationsList1.FirstOrDefault());
            Assert.IsType<WebNotification>(resultWebNotificationsList1.FirstOrDefault());
            Assert.Equal(2, resultWebNotificationsList1.Count);
        }

        [Fact]
        public async Task GetUsersWebNotifications_Should_Return_Empty_List_of_WebNotification_When_UserId_Is_Invalid()
        {
            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>().UseInMemoryDatabase("GetUsersWebNotifications_Should_Return_Empty_List_of_WebNotification_When_UserId_Is_Invalid").Options;
            await using ProgenyDbContext context = new(dbOptions);

            IOptions<MemoryDistributedCacheOptions> memoryCacheOptions = Options.Create(new MemoryDistributedCacheOptions());
            IDistributedCache memoryCache = new MemoryDistributedCache(memoryCacheOptions);

            NotificationsService notificationsService = new(context, memoryCache);

            WebNotification webNotification1 = new()
            {
                Title = "Title1",
                DateTime = new(2022, 1, 1, 11, 11, 11),
                From = "From1",
                To = "To1",
                Message = "Message1",
                Link = "Link1",
                Icon = "Icon1"
            };

            WebNotification webNotification2 = new()
            {
                Title = "Title2",
                DateTime = new(2022, 2, 2, 22, 22, 22),
                From = "From1",
                To = "To1",
                Message = "Message2",
                Link = "Link2",
                Icon = "Icon2"
            };

            WebNotification webNotification3 = new()
            {
                Title = "Title3",
                DateTime = new(2022, 3, 3, 11, 33, 33),
                From = "From1",
                To = "To2",
                Message = "Message3",
                Link = "Link3",
                Icon = "Icon3"
            };

            context.Add(webNotification1);
            context.Add(webNotification2);
            context.Add(webNotification3);
            await context.SaveChangesAsync();

            List<WebNotification> resultWebNotificationsList1 = await notificationsService.GetUsersWebNotifications("To3");

            Assert.NotNull(resultWebNotificationsList1);
            Assert.Empty(resultWebNotificationsList1);
        }

        [Fact]
        public async Task AddWebNotification_Should_Save_WebNotification()
        {
            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>().UseInMemoryDatabase("AddWebNotification_Should_Save_WebNotification").Options;
            await using ProgenyDbContext context = new(dbOptions);

            IOptions<MemoryDistributedCacheOptions> memoryCacheOptions = Options.Create(new MemoryDistributedCacheOptions());
            IDistributedCache memoryCache = new MemoryDistributedCache(memoryCacheOptions);

            NotificationsService notificationsService = new(context, memoryCache);

            WebNotification webNotification1 = new()
            {
                Title = "Title1",
                DateTime = new(2022, 1, 1, 11, 11, 11),
                From = "From1",
                To = "To1",
                Message = "Message1",
                Link = "Link1",
                Icon = "Icon1"
            };

            context.Add(webNotification1);
            await context.SaveChangesAsync();

            WebNotification webNotificationToAdd = new()
            {
                Title = "Title2",
                DateTime = new(2022, 2, 2, 22, 22, 22),
                From = "From1",
                To = "To1",
                Message = "Message2",
                Link = "Link2",
                Icon = "Icon2"
            };

            WebNotification addedWebNotification = await notificationsService.AddWebNotification(webNotificationToAdd);

            WebNotification resultWebNotification1 = await notificationsService.GetWebNotificationById(addedWebNotification.Id);

            Assert.NotNull(resultWebNotification1);
            Assert.IsType<WebNotification>(resultWebNotification1);
            Assert.Equal(webNotificationToAdd.Title, resultWebNotification1.Title);
            Assert.Equal(webNotificationToAdd.From, resultWebNotification1.From);
            Assert.Equal(webNotificationToAdd.To, resultWebNotification1.To);

        }

        [Fact]
        public async Task UpdateWebNotification_Should_Save_WebNotification()
        {
            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>().UseInMemoryDatabase("UpdateWebNotification_Should_Save_WebNotification").Options;
            await using ProgenyDbContext context = new(dbOptions);

            IOptions<MemoryDistributedCacheOptions> memoryCacheOptions = Options.Create(new MemoryDistributedCacheOptions());
            IDistributedCache memoryCache = new MemoryDistributedCache(memoryCacheOptions);

            NotificationsService notificationsService = new(context, memoryCache);

            WebNotification webNotification1 = new()
            {
                Title = "Title1",
                DateTime = new(2022, 1, 1, 11, 11, 11),
                From = "From1",
                To = "To1",
                Message = "Message1",
                Link = "Link1",
                Icon = "Icon1"
            };

            WebNotification webNotification2 = new()
            {
                Title = "Title2",
                DateTime = new(2022, 2, 2, 22, 22, 22),
                From = "From1",
                To = "To1",
                Message = "Message2",
                Link = "Link2",
                Icon = "Icon2"
            };
            context.Add(webNotification1);
            context.Add(webNotification2);
            await context.SaveChangesAsync();

            WebNotification webNotificationToUpdate = await notificationsService.GetWebNotificationById(1);
            webNotificationToUpdate.Title = "Title3";
            webNotificationToUpdate.IsRead = true;

            await notificationsService.UpdateWebNotification(webNotificationToUpdate);

            WebNotification resultWebNotification1 = await notificationsService.GetWebNotificationById(1);

            Assert.NotNull(resultWebNotification1);
            Assert.IsType<WebNotification>(resultWebNotification1);
            Assert.Equal("Title3", resultWebNotification1.Title);
            Assert.True(resultWebNotification1.IsRead);

        }

        [Fact]
        public async Task RemoveWebNotification_Should_Delete_WebNotification()
        {
            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>().UseInMemoryDatabase("RemoveWebNotification_Should_Delete_WebNotification").Options;
            await using ProgenyDbContext context = new(dbOptions);

            IOptions<MemoryDistributedCacheOptions> memoryCacheOptions = Options.Create(new MemoryDistributedCacheOptions());
            IDistributedCache memoryCache = new MemoryDistributedCache(memoryCacheOptions);

            NotificationsService notificationsService = new(context, memoryCache);

            WebNotification webNotification1 = new()
            {
                Title = "Title1",
                DateTime = new(2022, 1, 1, 11, 11, 11),
                From = "From1",
                To = "To1",
                Message = "Message1",
                Link = "Link1",
                Icon = "Icon1"
            };

            WebNotification webNotification2 = new()
            {
                Title = "Title2",
                DateTime = new(2022, 2, 2, 22, 22, 22),
                From = "From1",
                To = "To1",
                Message = "Message2",
                Link = "Link2",
                Icon = "Icon2"
            };


            webNotification1 = await notificationsService.AddWebNotification(webNotification1);
            _ = await notificationsService.AddWebNotification(webNotification2);

            WebNotification webNotificationToDelete = await notificationsService.GetWebNotificationById(webNotification1.Id);

            int countBeforeDelete = context.WebNotificationsDb.AsNoTracking().Count();
            
            await notificationsService.RemoveWebNotification(webNotificationToDelete);
            
            int countAfterDelete = context.WebNotificationsDb.AsNoTracking().Count();

            WebNotification resultWebNotification1 = await notificationsService.GetWebNotificationById(1);

            Assert.Null(resultWebNotification1);
            Assert.Equal(2, countBeforeDelete);
            Assert.Equal(1, countAfterDelete);

        }
    }
}
