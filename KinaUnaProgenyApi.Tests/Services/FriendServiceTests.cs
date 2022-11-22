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
    public class FriendServiceTests
    {
        [Fact]
        public async Task GetFriend_Should_Return_Friend_Object_When_Id_Is_Valid()
        {
            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>().UseInMemoryDatabase("GetFriend_Should_Return_Friend_Object_When_Id_Is_Valid").Options;
            await using ProgenyDbContext context = new ProgenyDbContext(dbOptions);

            Friend friend1 = new Friend
            {
                ProgenyId = 1, Author = "User1", AccessLevel = 0, Context = "Testing", Name = "Friend1", PictureLink = Constants.ProfilePictureUrl, Tags = "Tag1, Tag2",
                Notes = "Note1", FriendAddedDate = DateTime.UtcNow, Description = "Friend1", FriendSince = DateTime.Now, Type = 1
            };


            Friend friend2 = new Friend
            {
                ProgenyId = 1,
                Author = "User1",
                AccessLevel = 0,
                Context = "Testing",
                Name = "Friend2",
                PictureLink = Constants.ProfilePictureUrl,
                Tags = "Tag1, Tag2",
                Notes = "Note2",
                FriendAddedDate = DateTime.UtcNow,
                Description = "Friend2",
                FriendSince = DateTime.Now,
                Type = 1
            };

            context.Add(friend1);
            context.Add(friend2);
            await context.SaveChangesAsync();

            IOptions<MemoryDistributedCacheOptions>? memoryCacheOptions = Options.Create(new MemoryDistributedCacheOptions());
            IDistributedCache memoryCache = new MemoryDistributedCache(memoryCacheOptions);
            FriendService friendService = new FriendService(context, memoryCache);

            Friend resultFriend1 = await friendService.GetFriend(1);
            Friend resultFriend2 = await friendService.GetFriend(1); // Uses cache

            Assert.NotNull(resultFriend1);
            Assert.IsType<Friend>(resultFriend1);
            Assert.Equal(friend1.Author, resultFriend1.Author);
            Assert.Equal(friend1.Name, resultFriend1.Name);
            Assert.Equal(friend1.AccessLevel, resultFriend1.AccessLevel);
            Assert.Equal(friend1.ProgenyId, resultFriend1.ProgenyId);

            Assert.NotNull(resultFriend2);
            Assert.IsType<Friend>(resultFriend2);
            Assert.Equal(friend1.Author, resultFriend2.Author);
            Assert.Equal(friend1.Name, resultFriend2.Name);
            Assert.Equal(friend1.AccessLevel, resultFriend2.AccessLevel);
            Assert.Equal(friend1.ProgenyId, resultFriend2.ProgenyId);
        }

        [Fact]
        public async Task GetFriend_Should_Return_Null_When_Id_Is_Invalid()
        {
            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>().UseInMemoryDatabase("GetFriend_Should_Return_Null_When_Id_Is_Invalid").Options;
            await using ProgenyDbContext context = new ProgenyDbContext(dbOptions);

            Friend friend1 = new Friend
            {
                ProgenyId = 1,
                Author = "User1",
                AccessLevel = 0,
                Context = "Testing",
                Name = "Friend1",
                PictureLink = Constants.ProfilePictureUrl,
                Tags = "Tag1, Tag2",
                Notes = "Note1",
                FriendAddedDate = DateTime.UtcNow,
                Description = "Friend1",
                FriendSince = DateTime.Now,
                Type = 1
            };
            
            context.Add(friend1);
            await context.SaveChangesAsync();

            IOptions<MemoryDistributedCacheOptions>? memoryCacheOptions = Options.Create(new MemoryDistributedCacheOptions());
            IDistributedCache memoryCache = new MemoryDistributedCache(memoryCacheOptions);
            FriendService friendService = new FriendService(context, memoryCache);

            Friend resultFriend1 = await friendService.GetFriend(2);
            Friend resultFriend2 = await friendService.GetFriend(2); // Using cache
            
            Assert.Null(resultFriend1);
            Assert.Null(resultFriend2);
        }

        [Fact]
        public async Task AddFriend_Should_Save_Friend()
        {
            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>().UseInMemoryDatabase("AddFriend_Should_Save_Friend").Options;
            await using ProgenyDbContext context = new ProgenyDbContext(dbOptions);

            Friend friend1 = new Friend
            {
                ProgenyId = 1,
                Author = "User1",
                AccessLevel = 0,
                Context = "Testing",
                Name = "Friend1",
                PictureLink = Constants.ProfilePictureUrl,
                Tags = "Tag1, Tag2",
                Notes = "Note1",
                FriendAddedDate = DateTime.UtcNow,
                Description = "Friend1",
                FriendSince = DateTime.Now,
                Type = 1
            };
            context.Add(friend1);
            await context.SaveChangesAsync();

            IOptions<MemoryDistributedCacheOptions>? memoryCacheOptions = Options.Create(new MemoryDistributedCacheOptions());
            IDistributedCache memoryCache = new MemoryDistributedCache(memoryCacheOptions);
            FriendService friendService = new FriendService(context, memoryCache);

            Friend friendToAdd = new Friend
            {
                ProgenyId = 1,
                Author = "User1",
                AccessLevel = 0,
                Context = "Testing",
                Name = "Friend2",
                PictureLink = Constants.ProfilePictureUrl,
                Tags = "Tag1, Tag2",
                Notes = "Note2",
                FriendAddedDate = DateTime.UtcNow,
                Description = "Friend2",
                FriendSince = DateTime.Now,
                Type = 1
            };

            Friend addedFriend = await friendService.AddFriend(friendToAdd);
            Friend? dbFriend = await context.FriendsDb.AsNoTracking().SingleOrDefaultAsync(f => f.FriendId == addedFriend.FriendId);
            Friend savedFriend = await friendService.GetFriend(addedFriend.FriendId);

            Assert.NotNull(addedFriend);
            Assert.IsType<Friend>(addedFriend);
            Assert.Equal(friendToAdd.Author, addedFriend.Author);
            Assert.Equal(friendToAdd.Name, addedFriend.Name);
            Assert.Equal(friendToAdd.AccessLevel, addedFriend.AccessLevel);
            Assert.Equal(friendToAdd.ProgenyId, addedFriend.ProgenyId);

            if (dbFriend != null)
            {
                Assert.IsType<Friend>(dbFriend);
                Assert.Equal(friendToAdd.Author, dbFriend.Author);
                Assert.Equal(friendToAdd.Name, dbFriend.Name);
                Assert.Equal(friendToAdd.AccessLevel, dbFriend.AccessLevel);
                Assert.Equal(friendToAdd.ProgenyId, dbFriend.ProgenyId);
            }
            Assert.NotNull(savedFriend);
            Assert.IsType<Friend>(savedFriend);
            Assert.Equal(friendToAdd.Author, savedFriend.Author);
            Assert.Equal(friendToAdd.Name, savedFriend.Name);
            Assert.Equal(friendToAdd.AccessLevel, savedFriend.AccessLevel);
            Assert.Equal(friendToAdd.ProgenyId, savedFriend.ProgenyId);

        }

        [Fact]
        public async Task UpdateFriend_Should_Save_Friend()
        {
            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>().UseInMemoryDatabase("UpdateFriend_Should_Save_Friend").Options;
            await using ProgenyDbContext context = new ProgenyDbContext(dbOptions);

            Friend friend1 = new Friend
            {
                ProgenyId = 1,
                Author = "User1",
                AccessLevel = 0,
                Context = "Testing",
                Name = "Friend1",
                PictureLink = Constants.ProfilePictureUrl,
                Tags = "Tag1, Tag2",
                Notes = "Note1",
                FriendAddedDate = DateTime.UtcNow,
                Description = "Friend1",
                FriendSince = DateTime.Now,
                Type = 1
            };

            Friend friend2 = new Friend
            {
                ProgenyId = 1,
                Author = "User1",
                AccessLevel = 0,
                Context = "Testing",
                Name = "Friend2",
                PictureLink = Constants.ProfilePictureUrl,
                Tags = "Tag1, Tag2",
                Notes = "Note2",
                FriendAddedDate = DateTime.UtcNow,
                Description = "Friend2",
                FriendSince = DateTime.Now,
                Type = 1
            };
            context.Add(friend1);
            context.Add(friend2);
            await context.SaveChangesAsync();

            IOptions<MemoryDistributedCacheOptions>? memoryCacheOptions = Options.Create(new MemoryDistributedCacheOptions());
            IDistributedCache memoryCache = new MemoryDistributedCache(memoryCacheOptions);
            FriendService friendService = new FriendService(context, memoryCache);

            Friend friendToUpdate = await friendService.GetFriend(1);
            friendToUpdate.AccessLevel = 5;
            Friend updatedFriend = await friendService.UpdateFriend(friendToUpdate);
            Friend? dbFriend = await context.FriendsDb.AsNoTracking().SingleOrDefaultAsync(f => f.FriendId == 1);
            Friend savedFriend = await friendService.GetFriend(1);

            Assert.NotNull(updatedFriend);
            Assert.IsType<Friend>(updatedFriend);
            Assert.NotEqual(0, updatedFriend.FriendId);
            Assert.Equal("User1", updatedFriend.Author);
            Assert.Equal(5, updatedFriend.AccessLevel);
            Assert.Equal(1, updatedFriend.ProgenyId);

            if (dbFriend != null)
            {
                Assert.IsType<Friend>(dbFriend);
                Assert.NotEqual(0, dbFriend.FriendId);
                Assert.Equal("User1", dbFriend.Author);
                Assert.Equal(5, dbFriend.AccessLevel);
                Assert.Equal(1, dbFriend.ProgenyId);
            }

            Assert.NotNull(savedFriend);
            Assert.IsType<Friend>(savedFriend);
            Assert.NotEqual(0, savedFriend.FriendId);
            Assert.Equal("User1", savedFriend.Author);
            Assert.Equal(5, savedFriend.AccessLevel);
            Assert.Equal(1, savedFriend.ProgenyId);
        }

        [Fact]
        public async Task DeleteFriend_Should_Remove_Friend()
        {
            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>().UseInMemoryDatabase("DeleteFriend_Should_Remove_Friend").Options;
            await using ProgenyDbContext context = new ProgenyDbContext(dbOptions);

            Friend friend1 = new Friend
            {
                ProgenyId = 1,
                Author = "User1",
                AccessLevel = 0,
                Context = "Testing",
                Name = "Friend1",
                PictureLink = Constants.ProfilePictureUrl,
                Tags = "Tag1, Tag2",
                Notes = "Note1",
                FriendAddedDate = DateTime.UtcNow,
                Description = "Friend1",
                FriendSince = DateTime.Now,
                Type = 1
            };

            Friend friend2 = new Friend
            {
                ProgenyId = 1,
                Author = "User1",
                AccessLevel = 0,
                Context = "Testing",
                Name = "Friend2",
                PictureLink = Constants.ProfilePictureUrl,
                Tags = "Tag1, Tag2",
                Notes = "Note2",
                FriendAddedDate = DateTime.UtcNow,
                Description = "Friend2",
                FriendSince = DateTime.Now,
                Type = 1
            };
            context.Add(friend1);
            context.Add(friend2);
            await context.SaveChangesAsync();

            IOptions<MemoryDistributedCacheOptions>? memoryCacheOptions = Options.Create(new MemoryDistributedCacheOptions());
            IDistributedCache memoryCache = new MemoryDistributedCache(memoryCacheOptions);
            FriendService friendService = new FriendService(context, memoryCache);

            int friendItemsCountBeforeDelete = context.FriendsDb.Count();
            Friend friendToDelete = await friendService.GetFriend(1);

            await friendService.DeleteFriend(friendToDelete);
            Friend? deletedFriend = await context.FriendsDb.SingleOrDefaultAsync(f => f.FriendId == 1);
            int friendItemsCountAfterDelete = context.FriendsDb.Count();

            Assert.Null(deletedFriend);
            Assert.Equal(2, friendItemsCountBeforeDelete);
            Assert.Equal(1, friendItemsCountAfterDelete);
        }

        [Fact]
        public async Task GetFriendsList_Should_Return_List_Of_Friend_When_Progeny_Has_Saved_Friends()
        {
            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>().UseInMemoryDatabase("GetFriendsList_Should_Return_List_Of_Friend_When_Progeny_Has_Saved_Friends").Options;
            await using ProgenyDbContext context = new ProgenyDbContext(dbOptions);

            Friend friend1 = new Friend
            {
                ProgenyId = 1,
                Author = "User1",
                AccessLevel = 0,
                Context = "Testing",
                Name = "Friend1",
                PictureLink = Constants.ProfilePictureUrl,
                Tags = "Tag1, Tag2",
                Notes = "Note1",
                FriendAddedDate = DateTime.UtcNow,
                Description = "Friend1",
                FriendSince = DateTime.Now,
                Type = 1
            };

            Friend friend2 = new Friend
            {
                ProgenyId = 1,
                Author = "User1",
                AccessLevel = 0,
                Context = "Testing",
                Name = "Friend2",
                PictureLink = Constants.ProfilePictureUrl,
                Tags = "Tag1, Tag2",
                Notes = "Note2",
                FriendAddedDate = DateTime.UtcNow,
                Description = "Friend2",
                FriendSince = DateTime.Now,
                Type = 1
            };

            context.Add(friend1);
            context.Add(friend2);
            await context.SaveChangesAsync();

            IOptions<MemoryDistributedCacheOptions>? memoryCacheOptions = Options.Create(new MemoryDistributedCacheOptions());
            IDistributedCache memoryCache = new MemoryDistributedCache(memoryCacheOptions);
            FriendService friendService = new FriendService(context, memoryCache);

            List<Friend> friendsList = await friendService.GetFriendsList(1);
            List<Friend> friendsList2 = await friendService.GetFriendsList(1); // Test cached result.
            Friend firstFriend = friendsList.First();

            Assert.NotNull(friendsList);
            Assert.IsType<List<Friend>>(friendsList);
            Assert.Equal(2, friendsList.Count);
            Assert.NotNull(friendsList2);
            Assert.IsType<List<Friend>>(friendsList2);
            Assert.Equal(2, friendsList2.Count);
            Assert.NotNull(firstFriend);
            Assert.IsType<Friend>(firstFriend);
        }

        [Fact]
        public async Task GetFriendsList_Should_Return_Empty_List_Of_Friend_When_Progeny_Has_No_Saved_Friends()
        {
            
            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>().UseInMemoryDatabase("GetFriendsList_Should_Return_Empty_List_Of_Friend_When_Progeny_Has_No_Saved_Friends").Options;
            await using ProgenyDbContext context = new ProgenyDbContext(dbOptions);

            Friend friend1 = new Friend
            {
                ProgenyId = 1,
                Author = "User1",
                AccessLevel = 0,
                Context = "Testing",
                Name = "Friend1",
                PictureLink = Constants.ProfilePictureUrl,
                Tags = "Tag1, Tag2",
                Notes = "Note1",
                FriendAddedDate = DateTime.UtcNow,
                Description = "Friend1",
                FriendSince = DateTime.Now,
                Type = 1
            };

            Friend friend2 = new Friend
            {
                ProgenyId = 1,
                Author = "User1",
                AccessLevel = 0,
                Context = "Testing",
                Name = "Friend2",
                PictureLink = Constants.ProfilePictureUrl,
                Tags = "Tag1, Tag2",
                Notes = "Note2",
                FriendAddedDate = DateTime.UtcNow,
                Description = "Friend2",
                FriendSince = DateTime.Now,
                Type = 1
            };

            context.Add(friend1);
            context.Add(friend2);
            await context.SaveChangesAsync();

            IOptions<MemoryDistributedCacheOptions>? memoryCacheOptions = Options.Create(new MemoryDistributedCacheOptions());
            IDistributedCache memoryCache = new MemoryDistributedCache(memoryCacheOptions);
            FriendService friendService = new FriendService(context, memoryCache);

            List<Friend> friendsList = await friendService.GetFriendsList(2);
            List<Friend> friendsList2 = await friendService.GetFriendsList(2); // Test cached result.

            Assert.NotNull(friendsList);
            Assert.IsType<List<Friend>>(friendsList);
            Assert.Empty(friendsList);
            Assert.NotNull(friendsList2);
            Assert.IsType<List<Friend>>(friendsList2);
            Assert.Empty(friendsList2);
        }
    }
}
