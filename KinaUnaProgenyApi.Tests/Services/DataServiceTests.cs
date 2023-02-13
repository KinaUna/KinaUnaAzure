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
            await using ProgenyDbContext context = new ProgenyDbContext(dbOptions);

            MobileNotification notification1 = new MobileNotification { Title = "Test1", UserId = "User1", IconLink = Constants.ProfilePictureUrl, ItemId = "1", ItemType = 1,
                Language = "EN", Message = "Test message 1", Read = false, Time = DateTime.UtcNow};
            
            MobileNotification notification2 = new MobileNotification
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
            await using WebDbContext webContext = new WebDbContext(dbWebOptions);

            DataService dataService = new DataService(context, webContext);

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
            await using ProgenyDbContext context = new ProgenyDbContext(dbOptions);

            MobileNotification notification1 = new MobileNotification
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

            MobileNotification notification2 = new MobileNotification
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
            await using WebDbContext webContext = new WebDbContext(dbWebOptions);

            DataService dataService = new DataService(context, webContext);

            MobileNotification resultNotification1 = await dataService.GetMobileNotification(3);
            
            Assert.Null(resultNotification1);
        }

        [Fact]
        public async Task AddMobileNotification_Should_Save_MobileNotification()
        {
            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>().UseInMemoryDatabase("AddMobileNotification_Should_Save_MobileNotification").Options;
            await using ProgenyDbContext context = new ProgenyDbContext(dbOptions);

            MobileNotification notification1 = new MobileNotification
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
            await using WebDbContext webContext = new WebDbContext(dbWebOptions);

            DataService dataService = new DataService(context, webContext);

            MobileNotification notificationToAdd = new MobileNotification
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
            await using ProgenyDbContext context = new ProgenyDbContext(dbOptions);

            MobileNotification notification1 = new MobileNotification
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

            MobileNotification notification2 = new MobileNotification
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
            await using WebDbContext webContext = new WebDbContext(dbWebOptions);

            DataService dataService = new DataService(context, webContext);

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
            await using ProgenyDbContext context = new ProgenyDbContext(dbOptions);

            MobileNotification notification1 = new MobileNotification
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

            MobileNotification notification2 = new MobileNotification
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
            await using WebDbContext webContext = new WebDbContext(dbWebOptions);

            DataService dataService = new DataService(context, webContext);

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
            await using ProgenyDbContext context = new ProgenyDbContext(dbOptions);

            MobileNotification notification1 = new MobileNotification
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

            MobileNotification notification2 = new MobileNotification
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
            await using WebDbContext webContext = new WebDbContext(dbWebOptions);

            DataService dataService = new DataService(context, webContext);

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
            await using ProgenyDbContext context = new ProgenyDbContext(dbOptions);

            MobileNotification notification1 = new MobileNotification
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

            MobileNotification notification2 = new MobileNotification
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
            await using WebDbContext webContext = new WebDbContext(dbWebOptions);

            DataService dataService = new DataService(context, webContext);

            List<MobileNotification> contactsList = await dataService.GetUsersMobileNotifications("NoUser", "EN");
            
            Assert.NotNull(contactsList);
            Assert.IsType<List<MobileNotification>>(contactsList);
            Assert.Empty(contactsList);
        }

        [Fact]
        public async Task GetPushDeviceById_Should_Return_PushDevices_Object_When_Id_Is_Valid()
        {
            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>().UseInMemoryDatabase("GetPushDeviceById_Should_Return_PushDevices_Object_When_Id_Is_Valid").Options;
            await using ProgenyDbContext context = new ProgenyDbContext(dbOptions);
            
            DbContextOptions<WebDbContext> dbWebOptions = new DbContextOptionsBuilder<WebDbContext>().UseInMemoryDatabase("GetPushDeviceById_Should_Return_PushDevices_Object_When_Id_Is_Valid2").Options;
            await using WebDbContext webContext = new WebDbContext(dbWebOptions);

            DataService dataService = new DataService(context, webContext);

            PushDevices pushDevice1 = new PushDevices
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
            await using ProgenyDbContext context = new ProgenyDbContext(dbOptions);
            
            DbContextOptions<WebDbContext> dbWebOptions = new DbContextOptionsBuilder<WebDbContext>().UseInMemoryDatabase("GetPushDeviceById_Should_Return_Null_When_Id_Is_Invalid2").Options;
            await using WebDbContext webContext = new WebDbContext(dbWebOptions);

            DataService dataService = new DataService(context, webContext);

            PushDevices pushDevice1 = new PushDevices
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
            await using ProgenyDbContext context = new ProgenyDbContext(dbOptions);

            DbContextOptions<WebDbContext> dbWebOptions = new DbContextOptionsBuilder<WebDbContext>().UseInMemoryDatabase("GetPushDevice_Should_Return_PushDevices_Object_When_Parameter_Is_Valid2").Options;
            await using WebDbContext webContext = new WebDbContext(dbWebOptions);

            DataService dataService = new DataService(context, webContext);

            PushDevices pushDevice1 = new PushDevices
            {
                Name = "PushDevice1",
                PushAuth = "Auth1",
                PushEndpoint = "EndPoint1",
                PushP256DH = "P2256DH1"
            };

            webContext.Add(pushDevice1);
            await webContext.SaveChangesAsync();

            PushDevices requestPushDevice1 = new PushDevices
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
            await using ProgenyDbContext context = new ProgenyDbContext(dbOptions);

            DbContextOptions<WebDbContext> dbWebOptions = new DbContextOptionsBuilder<WebDbContext>().UseInMemoryDatabase("GetPushDevice_Should_Return_Null_When_Parameter_Is_Invalid2").Options;
            await using WebDbContext webContext = new WebDbContext(dbWebOptions);

            DataService dataService = new DataService(context, webContext);

            PushDevices pushDevice1 = new PushDevices
            {
                Name = "PushDevice1",
                PushAuth = "Auth1",
                PushEndpoint = "EndPoint1",
                PushP256DH = "P2256DH1"
            };

            webContext.Add(pushDevice1);
            await webContext.SaveChangesAsync();

            PushDevices requestPushDevice1 = new PushDevices
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
            await using ProgenyDbContext context = new ProgenyDbContext(dbOptions);

            DbContextOptions<WebDbContext> dbWebOptions = new DbContextOptionsBuilder<WebDbContext>().UseInMemoryDatabase("GetPushDeviceByUserId_Should_Return_List_of_PushDevices_When_UserId_Is_Valid2").Options;
            await using WebDbContext webContext = new WebDbContext(dbWebOptions);

            DataService dataService = new DataService(context, webContext);

            PushDevices pushDevice1 = new PushDevices
            {
                Name = "PushDevice1",
                PushAuth = "Auth1",
                PushEndpoint = "Endpoint1",
                PushP256DH = "P256DH1"
            };

            PushDevices pushDevice2 = new PushDevices
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
            await using ProgenyDbContext context = new ProgenyDbContext(dbOptions);

            DbContextOptions<WebDbContext> dbWebOptions = new DbContextOptionsBuilder<WebDbContext>().UseInMemoryDatabase("GetPushDeviceByUSerId_Should_Return_Empty_List_When_UserId_Is_Invalid2").Options;
            await using WebDbContext webContext = new WebDbContext(dbWebOptions);

            DataService dataService = new DataService(context, webContext);

            PushDevices pushDevice1 = new PushDevices
            {
                Name = "PushDevice1",
                PushAuth = "Auth1",
                PushEndpoint = "Endpoint1",
                PushP256DH = "P256DH1"
            };

            PushDevices pushDevice2 = new PushDevices
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
            await using ProgenyDbContext context = new ProgenyDbContext(dbOptions);

            DbContextOptions<WebDbContext> dbWebOptions = new DbContextOptionsBuilder<WebDbContext>().UseInMemoryDatabase("GetAllPushDevices_Should_Return_List_of_PushDevices2").Options;
            await using WebDbContext webContext = new WebDbContext(dbWebOptions);

            DataService dataService = new DataService(context, webContext);

            PushDevices pushDevice1 = new PushDevices
            {
                Name = "PushDevice1",
                PushAuth = "Auth1",
                PushEndpoint = "Endpoint1",
                PushP256DH = "P256DH1"
            };

            PushDevices pushDevice2 = new PushDevices
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
            await using ProgenyDbContext context = new ProgenyDbContext(dbOptions);

            DbContextOptions<WebDbContext> dbWebOptions = new DbContextOptionsBuilder<WebDbContext>().UseInMemoryDatabase("AddPushDevice_Should_Save_PushDevices_Object2").Options;
            await using WebDbContext webContext = new WebDbContext(dbWebOptions);

            DataService dataService = new DataService(context, webContext);

            PushDevices pushDevice1 = new PushDevices
            {
                Name = "PushDevice1",
                PushAuth = "Auth1",
                PushEndpoint = "EndPoint1",
                PushP256DH = "P2256DH1"
            };

            webContext.Add(pushDevice1);
            await webContext.SaveChangesAsync();

            PushDevices addPushDevice1 = new PushDevices
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
            await using ProgenyDbContext context = new ProgenyDbContext(dbOptions);

            DbContextOptions<WebDbContext> dbWebOptions = new DbContextOptionsBuilder<WebDbContext>().UseInMemoryDatabase("RemovePushDevice_Should_Remove_PushDevices_Object2").Options;
            await using WebDbContext webContext = new WebDbContext(dbWebOptions);

            DataService dataService = new DataService(context, webContext);

            PushDevices pushDevice1 = new PushDevices
            {
                Name = "PushDevice1",
                PushAuth = "Auth1",
                PushEndpoint = "EndPoint1",
                PushP256DH = "P2256DH1"
            };

            PushDevices pushDevice2 = new PushDevices
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
    }
}
