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
        public async Task GetMobileNotification_Returns_MobileNotification_Object_When_Id_Is_Valid()
        {
            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>().UseInMemoryDatabase("GetMobileNotification_Returns_MobileNotification_Object_When_Id_Is_Valid").Options;
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
            
            DataService dataService = new DataService(context);

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

            DataService dataService = new DataService(context);

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

            DataService dataService = new DataService(context);

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

            DataService dataService = new DataService(context);

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

            DataService dataService = new DataService(context);

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

            DataService dataService = new DataService(context);

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

            DataService dataService = new DataService(context);

            List<MobileNotification> contactsList = await dataService.GetUsersMobileNotifications("NoUser", "EN");
            
            Assert.NotNull(contactsList);
            Assert.IsType<List<MobileNotification>>(contactsList);
            Assert.Empty(contactsList);
        }
    }
}
