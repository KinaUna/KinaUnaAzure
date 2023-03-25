using KinaUna.Data;
using KinaUna.Data.Contexts;
using KinaUna.Data.Models;
using KinaUnaProgenyApi.Services;
using Microsoft.EntityFrameworkCore;

namespace KinaUnaProgenyApi.Tests.Services
{
    public class DataServiceTests
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

            DbContextOptions<WebDbContext> dbWebOptions = new DbContextOptionsBuilder<WebDbContext>().UseInMemoryDatabase("GetMobileNotification_Should_Return_MobileNotification_Object_When_Id_Is_Valid2").Options;
            await using WebDbContext webContext = new(dbWebOptions);

            DataService dataService = new(context, webContext);

            MobileNotification resultNotification1 = await dataService.GetMobileNotification(1);
            
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

            DbContextOptions<WebDbContext> dbWebOptions = new DbContextOptionsBuilder<WebDbContext>().UseInMemoryDatabase("GetMobileNotification_Should_Return_Null_When_Id_Is_Invalid2").Options;
            await using WebDbContext webContext = new(dbWebOptions);

            DataService dataService = new(context, webContext);

            MobileNotification resultNotification1 = await dataService.GetMobileNotification(3);
            
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

            DbContextOptions<WebDbContext> dbWebOptions = new DbContextOptionsBuilder<WebDbContext>().UseInMemoryDatabase("AddMobileNotification_Should_Save_MobileNotification2").Options;
            await using WebDbContext webContext = new(dbWebOptions);

            DataService dataService = new(context, webContext);

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

            MobileNotification addedNotification = await dataService.AddMobileNotification(notificationToAdd);
            MobileNotification? dbNotification = await context.MobileNotificationsDb.AsNoTracking().SingleOrDefaultAsync(mn => mn.NotificationId == addedNotification.NotificationId);
            MobileNotification savedNotification = await dataService.GetMobileNotification(addedNotification.NotificationId);

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

            DbContextOptions<WebDbContext> dbWebOptions = new DbContextOptionsBuilder<WebDbContext>().UseInMemoryDatabase("UpdateMobileNotification_Should_Save_MobileNotification2").Options;
            await using WebDbContext webContext = new(dbWebOptions);

            DataService dataService = new(context, webContext);

            MobileNotification notificationToUpdate = await dataService.GetMobileNotification(1);
            notificationToUpdate.Read = true;
            MobileNotification updatedNotification = await dataService.UpdateMobileNotification(notificationToUpdate);
            MobileNotification? dbNotification = await context.MobileNotificationsDb.AsNoTracking().SingleOrDefaultAsync(mn => mn.NotificationId == 1);
            MobileNotification savedNotification = await dataService.GetMobileNotification(1);

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

            DbContextOptions<WebDbContext> dbWebOptions = new DbContextOptionsBuilder<WebDbContext>().UseInMemoryDatabase("DeleteMobileNotification_Should_Remove_MobileNotification2").Options;
            await using WebDbContext webContext = new(dbWebOptions);

            DataService dataService = new(context, webContext);

            int notificationItemsCountBeforeDelete = context.MobileNotificationsDb.Count();
            MobileNotification notificationToDelete = await dataService.GetMobileNotification(1);

            await dataService.DeleteMobileNotification(notificationToDelete);
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

            DbContextOptions<WebDbContext> dbWebOptions = new DbContextOptionsBuilder<WebDbContext>().UseInMemoryDatabase("GetUsersMobileNotifications_Should_Return_List_Of_MobileNotifications_When_User_Has_Saved_MobileNotifications2").Options;
            await using WebDbContext webContext = new(dbWebOptions);

            DataService dataService = new(context, webContext);

            List<MobileNotification> notificationsList = await dataService.GetUsersMobileNotifications("User1", "EN");
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

            DbContextOptions<WebDbContext> dbWebOptions = new DbContextOptionsBuilder<WebDbContext>()
                .UseInMemoryDatabase("GetUsersMobileNotifications_Should_Return_Empty_List_Of_MobileNotifications_When_User_Has_No_Saved_MobileNotifications2").Options;
            await using WebDbContext webContext = new(dbWebOptions);

            DataService dataService = new(context, webContext);

            List<MobileNotification> contactsList = await dataService.GetUsersMobileNotifications("NoUser", "EN");
            
            Assert.NotNull(contactsList);
            Assert.IsType<List<MobileNotification>>(contactsList);
            Assert.Empty(contactsList);
        }

        [Fact]
        public async Task GetPushDeviceById_Should_Return_PushDevices_Object_When_Id_Is_Valid()
        {
            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>().UseInMemoryDatabase("GetPushDeviceById_Should_Return_PushDevices_Object_When_Id_Is_Valid").Options;
            await using ProgenyDbContext context = new(dbOptions);
            
            DbContextOptions<WebDbContext> dbWebOptions = new DbContextOptionsBuilder<WebDbContext>().UseInMemoryDatabase("GetPushDeviceById_Should_Return_PushDevices_Object_When_Id_Is_Valid2").Options;
            await using WebDbContext webContext = new(dbWebOptions);

            DataService dataService = new(context, webContext);

            PushDevices pushDevice1 = new()
            {
                Name = "PushDevice1",
                PushAuth = "",
                PushEndpoint = "",
                PushP256DH = ""
            };

            webContext.Add(pushDevice1);
            await webContext.SaveChangesAsync();
            PushDevices resultPushDevice1 = await dataService.GetPushDeviceById(1);
            
            Assert.NotNull(resultPushDevice1);
            Assert.IsType<PushDevices>(resultPushDevice1);
            Assert.Equal(pushDevice1.Name, resultPushDevice1.Name);
        }

        [Fact]
        public async Task GetPushDeviceById_Should_Return_Null_When_Id_Is_Invalid()
        {
            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>().UseInMemoryDatabase("GetPushDeviceById_Should_Return_Null_When_Id_Is_Invalid").Options;
            await using ProgenyDbContext context = new(dbOptions);
            
            DbContextOptions<WebDbContext> dbWebOptions = new DbContextOptionsBuilder<WebDbContext>().UseInMemoryDatabase("GetPushDeviceById_Should_Return_Null_When_Id_Is_Invalid2").Options;
            await using WebDbContext webContext = new(dbWebOptions);

            DataService dataService = new(context, webContext);

            PushDevices pushDevice1 = new()
            {
                Name = "PushDevice1",
                PushAuth = "",
                PushEndpoint = "",
                PushP256DH = ""
            };

            webContext.Add(pushDevice1);
            await webContext.SaveChangesAsync();
            PushDevices resultPushDevice2 = await dataService.GetPushDeviceById(2);

            Assert.Null(resultPushDevice2);
        }

        [Fact]
        public async Task GetPushDevice_Should_Return_PushDevices_Object_When_Parameter_Is_Valid()
        {
            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>().UseInMemoryDatabase("GetPushDevice_Should_Return_PushDevices_Object_When_Parameter_Is_Valid").Options;
            await using ProgenyDbContext context = new(dbOptions);

            DbContextOptions<WebDbContext> dbWebOptions = new DbContextOptionsBuilder<WebDbContext>().UseInMemoryDatabase("GetPushDevice_Should_Return_PushDevices_Object_When_Parameter_Is_Valid2").Options;
            await using WebDbContext webContext = new(dbWebOptions);

            DataService dataService = new(context, webContext);

            PushDevices pushDevice1 = new()
            {
                Name = "PushDevice1",
                PushAuth = "Auth1",
                PushEndpoint = "EndPoint1",
                PushP256DH = "P2256DH1"
            };

            webContext.Add(pushDevice1);
            await webContext.SaveChangesAsync();

            PushDevices requestPushDevice1 = new()
            {
                Name = "PushDevice1",
                PushAuth = "Auth1",
                PushEndpoint = "EndPoint1",
                PushP256DH = "P2256DH1"
            };

            PushDevices resultPushDevice1 = await dataService.GetPushDevice(requestPushDevice1);

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

            DbContextOptions<WebDbContext> dbWebOptions = new DbContextOptionsBuilder<WebDbContext>().UseInMemoryDatabase("GetPushDevice_Should_Return_Null_When_Parameter_Is_Invalid2").Options;
            await using WebDbContext webContext = new(dbWebOptions);

            DataService dataService = new(context, webContext);

            PushDevices pushDevice1 = new()
            {
                Name = "PushDevice1",
                PushAuth = "Auth1",
                PushEndpoint = "EndPoint1",
                PushP256DH = "P2256DH1"
            };

            webContext.Add(pushDevice1);
            await webContext.SaveChangesAsync();

            PushDevices requestPushDevice1 = new()
            {
                Name = "PushDevice2",
                PushAuth = "Auth2",
                PushEndpoint = "EndPoint2",
                PushP256DH = "P2256DH2"
            };

            PushDevices resultPushDevice1 = await dataService.GetPushDevice(requestPushDevice1);

            Assert.Null(resultPushDevice1);
        }

        [Fact]
        public async Task GetPushDeviceByUserId_Should_Return_List_of_PushDevices_When_UserId_Is_Valid()
        {
            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>().UseInMemoryDatabase("GetPushDeviceByUserId_Should_Return_List_of_PushDevices_When_UserId_Is_Valid").Options;
            await using ProgenyDbContext context = new(dbOptions);

            DbContextOptions<WebDbContext> dbWebOptions = new DbContextOptionsBuilder<WebDbContext>().UseInMemoryDatabase("GetPushDeviceByUserId_Should_Return_List_of_PushDevices_When_UserId_Is_Valid2").Options;
            await using WebDbContext webContext = new(dbWebOptions);

            DataService dataService = new(context, webContext);

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

            webContext.Add(pushDevice1);
            webContext.Add(pushDevice2);
            await webContext.SaveChangesAsync();

            List<PushDevices> resultPushDevicesList1 = await dataService.GetPushDevicesListByUserId("PushDevice1");

            Assert.NotNull(resultPushDevicesList1);
            Assert.Equal(webContext.PushDevices.Count(), resultPushDevicesList1.Count);
            Assert.NotNull(resultPushDevicesList1.FirstOrDefault());
            Assert.IsType<PushDevices>(resultPushDevicesList1.FirstOrDefault());
            Assert.Equal(pushDevice1.Name, resultPushDevicesList1.FirstOrDefault()!.Name);

        }

        [Fact]
        public async Task GetPushDeviceByUSerId_Should_Return_Empty_List_When_UserId_Is_Invalid()
        {
            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>().UseInMemoryDatabase("GetPushDeviceByUSerId_Should_Return_Empty_List_When_UserId_Is_Invalid").Options;
            await using ProgenyDbContext context = new(dbOptions);

            DbContextOptions<WebDbContext> dbWebOptions = new DbContextOptionsBuilder<WebDbContext>().UseInMemoryDatabase("GetPushDeviceByUSerId_Should_Return_Empty_List_When_UserId_Is_Invalid2").Options;
            await using WebDbContext webContext = new(dbWebOptions);

            DataService dataService = new(context, webContext);

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

            webContext.Add(pushDevice1);
            webContext.Add(pushDevice2);
            await webContext.SaveChangesAsync();

            List<PushDevices> resultPushDevicesList2 = await dataService.GetPushDevicesListByUserId("PushDevice2");

            Assert.Empty(resultPushDevicesList2);
        }

        [Fact]
        public async Task GetAllPushDevices_Should_Return_List_of_PushDevices()
        {
            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>().UseInMemoryDatabase("GetAllPushDevices_Should_Return_List_of_PushDevices").Options;
            await using ProgenyDbContext context = new(dbOptions);

            DbContextOptions<WebDbContext> dbWebOptions = new DbContextOptionsBuilder<WebDbContext>().UseInMemoryDatabase("GetAllPushDevices_Should_Return_List_of_PushDevices2").Options;
            await using WebDbContext webContext = new(dbWebOptions);

            DataService dataService = new(context, webContext);

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

            webContext.Add(pushDevice1);
            webContext.Add(pushDevice2);
            await webContext.SaveChangesAsync();

            List<PushDevices> resultPushDevicesList1 = await dataService.GetAllPushDevices();

            Assert.NotNull(resultPushDevicesList1);
            Assert.Equal(webContext.PushDevices.Count(), resultPushDevicesList1.Count);
            Assert.NotNull(resultPushDevicesList1.FirstOrDefault());
            Assert.IsType<PushDevices>(resultPushDevicesList1.FirstOrDefault());
            Assert.Equal(pushDevice1.Name, resultPushDevicesList1.FirstOrDefault()!.Name);

        }

        [Fact]
        public async Task AddPushDevice_Should_Save_PushDevices_Object()
        {
            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>().UseInMemoryDatabase("AddPushDevice_Should_Save_PushDevices_Object").Options;
            await using ProgenyDbContext context = new(dbOptions);

            DbContextOptions<WebDbContext> dbWebOptions = new DbContextOptionsBuilder<WebDbContext>().UseInMemoryDatabase("AddPushDevice_Should_Save_PushDevices_Object2").Options;
            await using WebDbContext webContext = new(dbWebOptions);

            DataService dataService = new(context, webContext);

            PushDevices pushDevice1 = new()
            {
                Name = "PushDevice1",
                PushAuth = "Auth1",
                PushEndpoint = "EndPoint1",
                PushP256DH = "P2256DH1"
            };

            webContext.Add(pushDevice1);
            await webContext.SaveChangesAsync();

            PushDevices addPushDevice1 = new()
            {
                Name = "PushDevice2",
                PushAuth = "Auth2",
                PushEndpoint = "EndPoint2",
                PushP256DH = "P2256DH2"
            };

            PushDevices addedPushDevice = await dataService.AddPushDevice(addPushDevice1);

            PushDevices resultPushDevice1 = await dataService.GetPushDeviceById(addedPushDevice.Id);

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

            DbContextOptions<WebDbContext> dbWebOptions = new DbContextOptionsBuilder<WebDbContext>().UseInMemoryDatabase("RemovePushDevice_Should_Remove_PushDevices_Object2").Options;
            await using WebDbContext webContext = new(dbWebOptions);

            DataService dataService = new(context, webContext);

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

            webContext.Add(pushDevice1);
            webContext.Add(pushDevice2);
            await webContext.SaveChangesAsync();

            int deviceBeforeRemove = webContext.PushDevices.Count();
            await dataService.RemovePushDevice(pushDevice1);
            int deviceAfterRemove = webContext.PushDevices.Count();

            Assert.Equal(2, deviceBeforeRemove);
            Assert.Equal(1, deviceAfterRemove);
        }

        [Fact]
        public async Task GetWebNotificationById_Should_Return_WebNotification_Object_When_Id_Is_Valid()
        {
            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>().UseInMemoryDatabase("GetWebNotificationById_Should_Return_WebNotification_Object_When_Id_Is_Valid").Options;
            await using ProgenyDbContext context = new(dbOptions);

            DbContextOptions<WebDbContext> dbWebOptions = new DbContextOptionsBuilder<WebDbContext>().UseInMemoryDatabase("GetWebNotificationById_Should_Return_WebNotification_Object_When_Id_Is_Valid2").Options;
            await using WebDbContext webContext = new(dbWebOptions);

            DataService dataService = new(context, webContext);

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

            webContext.Add(webNotification1);
            await webContext.SaveChangesAsync();

            WebNotification resultWebNotification1 = await dataService.GetWebNotificationById(1);

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

            DbContextOptions<WebDbContext> dbWebOptions = new DbContextOptionsBuilder<WebDbContext>().UseInMemoryDatabase("GetWebNotificationById_Should_Return_Null_Object_When_Id_Is_Invalid2").Options;
            await using WebDbContext webContext = new(dbWebOptions);

            DataService dataService = new(context, webContext);

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

            webContext.Add(webNotification1);
            await webContext.SaveChangesAsync();

            WebNotification resultWebNotification1 = await dataService.GetWebNotificationById(2);

            Assert.Null(resultWebNotification1);
        }

        [Fact]
        public async Task GetUsersWebNotifications_Should_Return_List_of_WebNotification_When_UserId_Is_Valid()
        {
            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>().UseInMemoryDatabase("GetUsersWebNotifications_Should_Return_List_of_WebNotification_When_UserId_Is_Valid").Options;
            await using ProgenyDbContext context = new(dbOptions);

            DbContextOptions<WebDbContext> dbWebOptions = new DbContextOptionsBuilder<WebDbContext>().UseInMemoryDatabase("GetUsersWebNotifications_Should_Return_List_of_WebNotification_When_UserId_Is_Valid2").Options;
            await using WebDbContext webContext = new(dbWebOptions);

            DataService dataService = new(context, webContext);

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

            webContext.Add(webNotification1);
            webContext.Add(webNotification2);
            webContext.Add(webNotification3);
            await webContext.SaveChangesAsync();

            List<WebNotification> resultWebNotificationsList1 = await dataService.GetUsersWebNotifications("To1");

            Assert.NotNull(resultWebNotificationsList1.FirstOrDefault());
            Assert.IsType<WebNotification>(resultWebNotificationsList1.FirstOrDefault());
            Assert.Equal(2, resultWebNotificationsList1.Count);
        }

        [Fact]
        public async Task GetUsersWebNotifications_Should_Return_Empty_List_of_WebNotification_When_UserId_Is_Invalid()
        {
            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>().UseInMemoryDatabase("GetUsersWebNotifications_Should_Return_Empty_List_of_WebNotification_When_UserId_Is_Invalid").Options;
            await using ProgenyDbContext context = new(dbOptions);

            DbContextOptions<WebDbContext> dbWebOptions = new DbContextOptionsBuilder<WebDbContext>().UseInMemoryDatabase("GetUsersWebNotifications_Should_Return_Empty_List_of_WebNotification_When_UserId_Is_Invalid2").Options;
            await using WebDbContext webContext = new(dbWebOptions);

            DataService dataService = new(context, webContext);

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

            webContext.Add(webNotification1);
            webContext.Add(webNotification2);
            webContext.Add(webNotification3);
            await webContext.SaveChangesAsync();

            List<WebNotification> resultWebNotificationsList1 = await dataService.GetUsersWebNotifications("To3");

            Assert.NotNull(resultWebNotificationsList1);
            Assert.Empty(resultWebNotificationsList1);
        }

        [Fact]
        public async Task AddWebNotification_Should_Save_WebNotification()
        {
            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>().UseInMemoryDatabase("AddWebNotification_Should_Save_WebNotification").Options;
            await using ProgenyDbContext context = new(dbOptions);

            DbContextOptions<WebDbContext> dbWebOptions = new DbContextOptionsBuilder<WebDbContext>().UseInMemoryDatabase("AddWebNotification_Should_Save_WebNotification2").Options;
            await using WebDbContext webContext = new(dbWebOptions);

            DataService dataService = new(context, webContext);

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

            webContext.Add(webNotification1);
            await webContext.SaveChangesAsync();

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

            WebNotification addedWebNotification = await dataService.AddWebNotification(webNotificationToAdd);

            WebNotification resultWebNotification1 = await dataService.GetWebNotificationById(addedWebNotification.Id);

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

            DbContextOptions<WebDbContext> dbWebOptions = new DbContextOptionsBuilder<WebDbContext>().UseInMemoryDatabase("UpdateWebNotification_Should_Save_WebNotification2").Options;
            await using WebDbContext webContext = new(dbWebOptions);

            DataService dataService = new(context, webContext);

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
            webContext.Add(webNotification1);
            webContext.Add(webNotification2);
            await webContext.SaveChangesAsync();

            WebNotification webNotificationToUpdate = await dataService.GetWebNotificationById(1);
            webNotificationToUpdate.Title = "Title3";
            webNotificationToUpdate.IsRead = true;

            await dataService.UpdateWebNotification(webNotificationToUpdate);

            WebNotification resultWebNotification1 = await dataService.GetWebNotificationById(1);

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

            DbContextOptions<WebDbContext> dbWebOptions = new DbContextOptionsBuilder<WebDbContext>().UseInMemoryDatabase("RemoveWebNotification_Should_Delete_WebNotification").Options;
            await using WebDbContext webContext = new(dbWebOptions);

            DataService dataService = new(context, webContext);

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
            webContext.Add(webNotification1);
            webContext.Add(webNotification2);
            await webContext.SaveChangesAsync();

            WebNotification webNotificationToDelete = await dataService.GetWebNotificationById(1);
            int countBeforeDelete = webContext.WebNotificationsDb.Count();
            
            await dataService.RemoveWebNotification(webNotificationToDelete);
            
            int countAfterDelete = webContext.WebNotificationsDb.Count();

            WebNotification resultWebNotification1 = await dataService.GetWebNotificationById(1);

            Assert.Null(resultWebNotification1);
            Assert.Equal(2, countBeforeDelete);
            Assert.Equal(1, countAfterDelete);

        }
    }
}
