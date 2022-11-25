using KinaUna.Data;
using KinaUna.Data.Contexts;
using KinaUna.Data.Models;
using KinaUnaProgenyApi.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Moq;

namespace KinaUnaProgenyApi.Tests.Services
{
    public class CommentsServiceTests
    {
        private readonly Mock<IPicturesService> _picturesServiceMock;
        private readonly Mock<IVideosService> _videosServiceMock;
        public CommentsServiceTests()
        {
            _picturesServiceMock = new Mock<IPicturesService>();
            _videosServiceMock = new Mock<IVideosService>();
        }

        [Fact]
        public async Task GetComment_Should_Return_Comment_Object_When_Id_Is_Valid()
        {
            DbContextOptions<MediaDbContext> dbOptions = new DbContextOptionsBuilder<MediaDbContext>().UseInMemoryDatabase("GetComment_Should_Return_Comment_Object_When_Id_Is_Valid").Options;
            await using MediaDbContext context = new MediaDbContext(dbOptions);

            Comment comment1 = new Comment
            {
                CommentText = "Test1", Author = "User1", AccessLevel = 0, AuthorImage = Constants.ProfilePictureUrl, CommentThreadNumber = 1, Created = DateTime.UtcNow, DisplayName = "User1", ItemType = 1, ItemId = "1"
            };
            Comment comment2 = new Comment
            {
                CommentText = "Test2",
                Author = "User2",
                AccessLevel = 0,
                AuthorImage = Constants.ProfilePictureUrl,
                CommentThreadNumber = 1,
                Created = DateTime.UtcNow,
                DisplayName = "User2",
                ItemType = 1,
                ItemId = "1"
            };
            context.Add(comment1);
            context.Add(comment2);
            await context.SaveChangesAsync();

            IOptions<MemoryDistributedCacheOptions> memoryCacheOptions = Options.Create(new MemoryDistributedCacheOptions());
            IDistributedCache memoryCache = new MemoryDistributedCache(memoryCacheOptions);
            CommentsService commentsService = new CommentsService(context, memoryCache, _picturesServiceMock.Object, _videosServiceMock.Object);

            Comment resultComment1 = await commentsService.GetComment(1);
            Comment resultComment2 = await commentsService.GetComment(1); // Uses cache

            Assert.NotNull(resultComment1);
            Assert.IsType<Comment>(resultComment1);
            Assert.Equal(comment1.Author, resultComment1.Author);
            Assert.Equal(comment1.CommentText, resultComment1.CommentText);
            Assert.Equal(comment1.AccessLevel, resultComment1.AccessLevel);
            Assert.Equal(comment1.CommentThreadNumber, resultComment1.CommentThreadNumber);

            Assert.NotNull(resultComment2);
            Assert.IsType<Comment>(resultComment2);
            Assert.Equal(comment1.Author, resultComment2.Author);
            Assert.Equal(comment1.CommentText, resultComment2.CommentText);
            Assert.Equal(comment1.AccessLevel, resultComment2.AccessLevel);
            Assert.Equal(comment1.CommentThreadNumber, resultComment2.CommentThreadNumber);
        }

        [Fact]
        public async Task GetComment_Should_Return_Null_When_Id_Is_Invalid()
        {
            DbContextOptions<MediaDbContext> dbOptions = new DbContextOptionsBuilder<MediaDbContext>().UseInMemoryDatabase("GetComment_Should_Return_Null_When_Id_Is_Invalid").Options;
            await using MediaDbContext context = new MediaDbContext(dbOptions);

            Comment comment1 = new Comment
            {
                CommentText = "Test1",
                Author = "User1",
                AccessLevel = 0,
                AuthorImage = Constants.ProfilePictureUrl,
                CommentThreadNumber = 1,
                Created = DateTime.UtcNow,
                DisplayName = "User1",
                ItemType = 1,
                ItemId = "1"
            };
            Comment comment2 = new Comment
            {
                CommentText = "Test2",
                Author = "User2",
                AccessLevel = 0,
                AuthorImage = Constants.ProfilePictureUrl,
                CommentThreadNumber = 1,
                Created = DateTime.UtcNow,
                DisplayName = "User2",
                ItemType = 1,
                ItemId = "1"
            };
            context.Add(comment1);
            context.Add(comment2);
            await context.SaveChangesAsync();

            IOptions<MemoryDistributedCacheOptions> memoryCacheOptions = Options.Create(new MemoryDistributedCacheOptions());
            IDistributedCache memoryCache = new MemoryDistributedCache(memoryCacheOptions);
            CommentsService commentsService = new CommentsService(context, memoryCache, _picturesServiceMock.Object, _videosServiceMock.Object);

            Comment resultComment1 = await commentsService.GetComment(3);
            Comment resultComment2 = await commentsService.GetComment(3); // Uses cache

            Assert.Null(resultComment1);
            
            Assert.Null(resultComment2);
        }

        [Fact]
        public async Task GetCommentsList_Should_Return_List_Of_Comment_When_Id_Is_Valid()
        {
            DbContextOptions<MediaDbContext> dbOptions = new DbContextOptionsBuilder<MediaDbContext>().UseInMemoryDatabase("GetCommentsList_Should_Return_List_Of_Comment_When_Id_Is_Valid").Options;
            await using MediaDbContext context = new MediaDbContext(dbOptions);

            Comment comment1 = new Comment
            {
                CommentText = "Test1",
                Author = "User1",
                AccessLevel = 0,
                AuthorImage = Constants.ProfilePictureUrl,
                CommentThreadNumber = 1,
                Created = DateTime.UtcNow,
                DisplayName = "User1",
                ItemType = 1,
                ItemId = "1"
            };
            Comment comment2 = new Comment
            {
                CommentText = "Test2",
                Author = "User2",
                AccessLevel = 0,
                AuthorImage = Constants.ProfilePictureUrl,
                CommentThreadNumber = 1,
                Created = DateTime.UtcNow,
                DisplayName = "User2",
                ItemType = 1,
                ItemId = "1"
            };
            context.Add(comment1);
            context.Add(comment2);
            await context.SaveChangesAsync();

            IOptions<MemoryDistributedCacheOptions> memoryCacheOptions = Options.Create(new MemoryDistributedCacheOptions());
            IDistributedCache memoryCache = new MemoryDistributedCache(memoryCacheOptions);
            CommentsService commentsService = new CommentsService(context, memoryCache, _picturesServiceMock.Object, _videosServiceMock.Object);

            List<Comment> resultCommentsList1 = await commentsService.GetCommentsList(1);
            List<Comment> resultCommentsList2 = await commentsService.GetCommentsList(1); // Uses cache

            Assert.NotNull(resultCommentsList1);
            Assert.IsType<List<Comment>>(resultCommentsList1);
            Assert.NotEmpty(resultCommentsList1);

            Assert.NotNull(resultCommentsList2);
            Assert.IsType<List<Comment>>(resultCommentsList2);
            Assert.NotEmpty(resultCommentsList2);

        }

        [Fact]
        public async Task GetCommentsList_Should_Return_Empty_List_Of_Comment_When_Id_Is_Invalid()
        {
            DbContextOptions<MediaDbContext> dbOptions = new DbContextOptionsBuilder<MediaDbContext>().UseInMemoryDatabase("GetCommentsList_Should_Return_Empty_List_Of_Comment_When_Id_Is_Invalid").Options;
            await using MediaDbContext context = new MediaDbContext(dbOptions);

            Comment comment1 = new Comment
            {
                CommentText = "Test1",
                Author = "User1",
                AccessLevel = 0,
                AuthorImage = Constants.ProfilePictureUrl,
                CommentThreadNumber = 1,
                Created = DateTime.UtcNow,
                DisplayName = "User1",
                ItemType = 1,
                ItemId = "1"
            };
            Comment comment2 = new Comment
            {
                CommentText = "Test2",
                Author = "User2",
                AccessLevel = 0,
                AuthorImage = Constants.ProfilePictureUrl,
                CommentThreadNumber = 1,
                Created = DateTime.UtcNow,
                DisplayName = "User2",
                ItemType = 1,
                ItemId = "1"
            };
            context.Add(comment1);
            context.Add(comment2);
            await context.SaveChangesAsync();

            IOptions<MemoryDistributedCacheOptions> memoryCacheOptions = Options.Create(new MemoryDistributedCacheOptions());
            IDistributedCache memoryCache = new MemoryDistributedCache(memoryCacheOptions);
            CommentsService commentsService = new CommentsService(context, memoryCache, _picturesServiceMock.Object, _videosServiceMock.Object);

            List<Comment> resultCommentsList1 = await commentsService.GetCommentsList(2);
            List<Comment> resultCommentsList2 = await commentsService.GetCommentsList(2); // Uses cache

            Assert.NotNull(resultCommentsList1);
            Assert.IsType<List<Comment>>(resultCommentsList1);
            Assert.Empty(resultCommentsList1);

            Assert.NotNull(resultCommentsList2);
            Assert.IsType<List<Comment>>(resultCommentsList2);
            Assert.Empty(resultCommentsList2);
        }

        [Fact]
        public async Task AddComment_Should_Save_Comment()
        {
            DbContextOptions<MediaDbContext> dbOptions = new DbContextOptionsBuilder<MediaDbContext>().UseInMemoryDatabase("AddComment_Should_Save_Comment").Options;
            await using MediaDbContext context = new MediaDbContext(dbOptions);

            Comment comment1 = new Comment
            {
                CommentText = "Test1",
                Author = "User1",
                AccessLevel = 0,
                AuthorImage = Constants.ProfilePictureUrl,
                CommentThreadNumber = 1,
                Created = DateTime.UtcNow,
                DisplayName = "User1",
                ItemType = 1,
                ItemId = "1"
            };
            Comment comment2 = new Comment
            {
                CommentText = "Test2",
                Author = "User2",
                AccessLevel = 0,
                AuthorImage = Constants.ProfilePictureUrl,
                CommentThreadNumber = 1,
                Created = DateTime.UtcNow,
                DisplayName = "User2",
                ItemType = 1,
                ItemId = "1"
            };
            context.Add(comment1);
            context.Add(comment2);
            await context.SaveChangesAsync();

            IOptions<MemoryDistributedCacheOptions> memoryCacheOptions = Options.Create(new MemoryDistributedCacheOptions());
            IDistributedCache memoryCache = new MemoryDistributedCache(memoryCacheOptions);
            CommentsService commentsService = new CommentsService(context, memoryCache, _picturesServiceMock.Object, _videosServiceMock.Object);

            Comment commentToAdd = new Comment
            {
                CommentText = "Test3",
                Author = "User3",
                AccessLevel = 0,
                AuthorImage = Constants.ProfilePictureUrl,
                CommentThreadNumber = 2,
                Created = DateTime.UtcNow,
                DisplayName = "User3",
                ItemType = 1,
                ItemId = "2"
            };

            Comment addedComment = await commentsService.AddComment(commentToAdd);
            Comment? dbComment = await context.CommentsDb.AsNoTracking().SingleOrDefaultAsync(ci => ci.CommentId == addedComment.CommentId);
            Comment savedComment = await commentsService.GetComment(addedComment.CommentId);

            Assert.NotNull(addedComment);
            Assert.IsType<Comment>(addedComment);
            Assert.NotEqual(0, addedComment.CommentId);
            Assert.Equal("User3", addedComment.Author);
            Assert.Equal(0, addedComment.AccessLevel);
            Assert.Equal(2, addedComment.CommentThreadNumber);

            if (dbComment != null)
            {
                Assert.IsType<Comment>(dbComment);
                Assert.NotEqual(0, dbComment.CommentId);
                Assert.Equal("User3", dbComment.Author);
                Assert.Equal(0, dbComment.AccessLevel);
                Assert.Equal(2, dbComment.CommentThreadNumber);
            }
            Assert.NotNull(savedComment);
            Assert.IsType<Comment>(savedComment);
            Assert.NotEqual(0, savedComment.CommentId);
            Assert.Equal("User3", savedComment.Author);
            Assert.Equal(0, savedComment.AccessLevel);
            Assert.Equal(2, savedComment.CommentThreadNumber);

        }

        [Fact]
        public async Task AddComment_Should_Increment_CommentCount()
        {
            DbContextOptions<MediaDbContext> dbOptions = new DbContextOptionsBuilder<MediaDbContext>().UseInMemoryDatabase("AddComment_Should_Increment_CommentCount").Options;
            await using MediaDbContext context = new MediaDbContext(dbOptions);

            Comment comment1 = new Comment
            {
                CommentText = "Test1",
                Author = "User1",
                AccessLevel = 0,
                AuthorImage = Constants.ProfilePictureUrl,
                CommentThreadNumber = 1,
                Created = DateTime.UtcNow,
                DisplayName = "User1",
                ItemType = 1,
                ItemId = "1"
            };
            Comment comment2 = new Comment
            {
                CommentText = "Test2",
                Author = "User2",
                AccessLevel = 0,
                AuthorImage = Constants.ProfilePictureUrl,
                CommentThreadNumber = 1,
                Created = DateTime.UtcNow,
                DisplayName = "User2",
                ItemType = 1,
                ItemId = "1"
            };

            CommentThread commentThread1 = new CommentThread{CommentsCount = 2};
            
            context.Add(commentThread1);
            
            context.Add(comment1);
            context.Add(comment2);
            
            await context.SaveChangesAsync();

            IOptions<MemoryDistributedCacheOptions> memoryCacheOptions = Options.Create(new MemoryDistributedCacheOptions());
            IDistributedCache memoryCache = new MemoryDistributedCache(memoryCacheOptions);
            CommentsService commentsService = new CommentsService(context, memoryCache, _picturesServiceMock.Object, _videosServiceMock.Object);

            Comment commentToAdd = new Comment
            {
                CommentText = "Test3",
                Author = "User3",
                AccessLevel = 0,
                AuthorImage = Constants.ProfilePictureUrl,
                CommentThreadNumber = 1,
                Created = DateTime.UtcNow,
                DisplayName = "User3",
                ItemType = 1,
                ItemId = "2"
            };
            int commentsCountBefore = 0;
            CommentThread? testCommentThread = await context.CommentThreadsDb.AsNoTracking().SingleOrDefaultAsync(ct => ct.Id == 1);
            if (testCommentThread != null)
            {
                commentsCountBefore = testCommentThread.CommentsCount;
            }

            Comment addedComment = await commentsService.AddComment(commentToAdd);

            int commentsCountAfter = 0;
            CommentThread? testCommentThread2 = await context.CommentThreadsDb.AsNoTracking().SingleOrDefaultAsync(ct => ct.Id == 1);
            if (testCommentThread2 != null)
            {
                commentsCountAfter = testCommentThread2.CommentsCount;
            }

            Assert.NotNull(addedComment);
            Assert.Equal(2, commentsCountBefore);
            Assert.Equal(3, commentsCountAfter);
        }

        [Fact]
        public async Task UpdateComment_Should_Save_Comment()
        {
            DbContextOptions<MediaDbContext> dbOptions = new DbContextOptionsBuilder<MediaDbContext>().UseInMemoryDatabase("UpdateComment_Should_Save_Comment").Options;
            await using MediaDbContext context = new MediaDbContext(dbOptions);

            Comment comment1 = new Comment
            {
                CommentText = "Test1",
                Author = "User1",
                AccessLevel = 0,
                AuthorImage = Constants.ProfilePictureUrl,
                CommentThreadNumber = 1,
                Created = DateTime.UtcNow,
                DisplayName = "User1",
                ItemType = 1,
                ItemId = "1"
            };
            Comment comment2 = new Comment
            {
                CommentText = "Test2",
                Author = "User2",
                AccessLevel = 0,
                AuthorImage = Constants.ProfilePictureUrl,
                CommentThreadNumber = 1,
                Created = DateTime.UtcNow,
                DisplayName = "User2",
                ItemType = 1,
                ItemId = "1"
            };
            context.Add(comment1);
            context.Add(comment2);
            await context.SaveChangesAsync();

            IOptions<MemoryDistributedCacheOptions> memoryCacheOptions = Options.Create(new MemoryDistributedCacheOptions());
            IDistributedCache memoryCache = new MemoryDistributedCache(memoryCacheOptions);
            CommentsService commentsService = new CommentsService(context, memoryCache, _picturesServiceMock.Object, _videosServiceMock.Object);

            Comment commentToUpdate = await commentsService.GetComment(1);
            commentToUpdate.CommentText = "Test1 Updated";
            Comment updatedComment = await commentsService.UpdateComment(commentToUpdate);
            Comment? dbComment = await context.CommentsDb.AsNoTracking().SingleOrDefaultAsync(ci => ci.CommentId == updatedComment.CommentId);
            Comment savedComment = await commentsService.GetComment(updatedComment.CommentId);

            Assert.NotNull(updatedComment);
            Assert.IsType<Comment>(updatedComment);
            Assert.NotEqual(0, updatedComment.CommentId);
            Assert.Equal("User1", updatedComment.Author);
            Assert.Equal(0, updatedComment.AccessLevel);
            Assert.Equal(1, updatedComment.CommentThreadNumber);
            Assert.Equal("Test1 Updated", updatedComment.CommentText);

            if (dbComment != null)
            {
                Assert.IsType<Comment>(dbComment);
                Assert.NotEqual(0, dbComment.CommentId);
                Assert.Equal("User1", dbComment.Author);
                Assert.Equal(0, dbComment.AccessLevel);
                Assert.Equal(1, dbComment.CommentThreadNumber);
                Assert.Equal("Test1 Updated", dbComment.CommentText);
            }
            Assert.NotNull(savedComment);
            Assert.IsType<Comment>(savedComment);
            Assert.NotEqual(0, savedComment.CommentId);
            Assert.Equal("User1", savedComment.Author);
            Assert.Equal(0, savedComment.AccessLevel);
            Assert.Equal(1, savedComment.CommentThreadNumber);
            Assert.Equal("Test1 Updated", savedComment.CommentText);
        }

        [Fact]
        public async Task DeleteComment_Should_Remove_Comment()
        {
            DbContextOptions<MediaDbContext> dbOptions = new DbContextOptionsBuilder<MediaDbContext>().UseInMemoryDatabase("DeleteComment_Should_Remove_Comment").Options;
            await using MediaDbContext context = new MediaDbContext(dbOptions);

            Comment comment1 = new Comment
            {
                CommentText = "Test1",
                Author = "User1",
                AccessLevel = 0,
                AuthorImage = Constants.ProfilePictureUrl,
                CommentThreadNumber = 1,
                Created = DateTime.UtcNow,
                DisplayName = "User1",
                ItemType = 1,
                ItemId = "1"
            };
            Comment comment2 = new Comment
            {
                CommentText = "Test2",
                Author = "User2",
                AccessLevel = 0,
                AuthorImage = Constants.ProfilePictureUrl,
                CommentThreadNumber = 1,
                Created = DateTime.UtcNow,
                DisplayName = "User2",
                ItemType = 1,
                ItemId = "1"
            };
            context.Add(comment1);
            context.Add(comment2);
            await context.SaveChangesAsync();

            IOptions<MemoryDistributedCacheOptions> memoryCacheOptions = Options.Create(new MemoryDistributedCacheOptions());
            IDistributedCache memoryCache = new MemoryDistributedCache(memoryCacheOptions);
            CommentsService commentsService = new CommentsService(context, memoryCache, _picturesServiceMock.Object, _videosServiceMock.Object);

            Comment commentToRemove = await commentsService.GetComment(1);
            await commentsService.DeleteComment(commentToRemove);
            Comment? dbComment = await context.CommentsDb.AsNoTracking().SingleOrDefaultAsync(ci => ci.CommentId == 1);
            Comment? savedComment = await commentsService.GetComment(1);

            Assert.Null(dbComment);
            Assert.Null(savedComment);
        }

        [Fact]
        public async Task DeleteComment_Should_Decrement_CommentCount()
        {
            DbContextOptions<MediaDbContext> dbOptions = new DbContextOptionsBuilder<MediaDbContext>().UseInMemoryDatabase("DeleteComment_Should_Decrement_CommentCount").Options;
            await using MediaDbContext context = new MediaDbContext(dbOptions);

            Comment comment1 = new Comment
            {
                CommentText = "Test1",
                Author = "User1",
                AccessLevel = 0,
                AuthorImage = Constants.ProfilePictureUrl,
                CommentThreadNumber = 1,
                Created = DateTime.UtcNow,
                DisplayName = "User1",
                ItemType = 1,
                ItemId = "1"
            };
            Comment comment2 = new Comment
            {
                CommentText = "Test2",
                Author = "User2",
                AccessLevel = 0,
                AuthorImage = Constants.ProfilePictureUrl,
                CommentThreadNumber = 1,
                Created = DateTime.UtcNow,
                DisplayName = "User2",
                ItemType = 1,
                ItemId = "1"
            };

            CommentThread commentThread1 = new CommentThread { CommentsCount = 2 };

            context.Add(commentThread1);

            context.Add(comment1);
            context.Add(comment2);

            await context.SaveChangesAsync();

            IOptions<MemoryDistributedCacheOptions> memoryCacheOptions = Options.Create(new MemoryDistributedCacheOptions());
            IDistributedCache memoryCache = new MemoryDistributedCache(memoryCacheOptions);
            CommentsService commentsService = new CommentsService(context, memoryCache, _picturesServiceMock.Object, _videosServiceMock.Object);
            
            int commentsCountBefore = 0;
            CommentThread? testCommentThread = await context.CommentThreadsDb.AsNoTracking().SingleOrDefaultAsync(ct => ct.Id == 1);
            if (testCommentThread != null)
            {
                commentsCountBefore = testCommentThread.CommentsCount;
            }

            Comment commentToDelete = await commentsService.GetComment(1);
            await commentsService.DeleteComment(commentToDelete);

            int commentsCountAfter = 0;
            CommentThread? testCommentThread2 = await context.CommentThreadsDb.AsNoTracking().SingleOrDefaultAsync(ct => ct.Id == 1);
            if (testCommentThread2 != null)
            {
                commentsCountAfter = testCommentThread2.CommentsCount;
            }
            
            Assert.Equal(2, commentsCountBefore);
            Assert.Equal(1, commentsCountAfter);
        }

        [Fact]
        public async Task GetCommentThread_Should_Return_CommentThread_Object_When_Id_Is_Valid()
        {
            DbContextOptions<MediaDbContext> dbOptions = new DbContextOptionsBuilder<MediaDbContext>().UseInMemoryDatabase("GetCommentThread_Should_Return_CommentThread_Object_When_Id_Is_Valid").Options;
            await using MediaDbContext context = new MediaDbContext(dbOptions);

            CommentThread commentThread1 = new CommentThread { CommentsCount = 1};
            CommentThread commentThread2 = new CommentThread { CommentsCount = 1 };
            
            context.Add(commentThread1);
            context.Add(commentThread2);
            await context.SaveChangesAsync();

            IOptions<MemoryDistributedCacheOptions> memoryCacheOptions = Options.Create(new MemoryDistributedCacheOptions());
            IDistributedCache memoryCache = new MemoryDistributedCache(memoryCacheOptions);
            CommentsService commentsService = new CommentsService(context, memoryCache, _picturesServiceMock.Object, _videosServiceMock.Object);

            CommentThread resultCommentThread1 = await commentsService.GetCommentThread(1);
            
            Assert.NotNull(resultCommentThread1);
            Assert.IsType<CommentThread>(resultCommentThread1);
            Assert.Equal(1, resultCommentThread1.CommentsCount);
        }

        [Fact]
        public async Task GetCommentThread__Should_Return_Null_When_Id_Is_Invalid()
        {
            DbContextOptions<MediaDbContext> dbOptions = new DbContextOptionsBuilder<MediaDbContext>().UseInMemoryDatabase("GetCommentThread__Should_Return_Null_When_Id_Is_Invalid").Options;
            await using MediaDbContext context = new MediaDbContext(dbOptions);

            CommentThread commentThread1 = new CommentThread { CommentsCount = 1 };
            CommentThread commentThread2 = new CommentThread { CommentsCount = 1 };

            context.Add(commentThread1);
            context.Add(commentThread2);
            await context.SaveChangesAsync();

            IOptions<MemoryDistributedCacheOptions> memoryCacheOptions = Options.Create(new MemoryDistributedCacheOptions());
            IDistributedCache memoryCache = new MemoryDistributedCache(memoryCacheOptions);
            CommentsService commentsService = new CommentsService(context, memoryCache, _picturesServiceMock.Object, _videosServiceMock.Object);

            CommentThread resultCommentThread1 = await commentsService.GetCommentThread(3);

            Assert.Null(resultCommentThread1);
        }

        [Fact]
        public async Task AddCommentThread_Should_Save_CommentThread_Object()
        {
            DbContextOptions<MediaDbContext> dbOptions = new DbContextOptionsBuilder<MediaDbContext>().UseInMemoryDatabase("AddCommentThread_Should_Save_CommentThread_Object").Options;
            await using MediaDbContext context = new MediaDbContext(dbOptions);

            CommentThread commentThread1 = new CommentThread { CommentsCount = 1 };
            CommentThread commentThread2 = new CommentThread { CommentsCount = 1 };

            context.Add(commentThread1);
            context.Add(commentThread2);
            await context.SaveChangesAsync();

            IOptions<MemoryDistributedCacheOptions> memoryCacheOptions = Options.Create(new MemoryDistributedCacheOptions());
            IDistributedCache memoryCache = new MemoryDistributedCache(memoryCacheOptions);
            CommentsService commentsService = new CommentsService(context, memoryCache, _picturesServiceMock.Object, _videosServiceMock.Object);
            
            CommentThread addedCommentThread = await commentsService.AddCommentThread();
            CommentThread? dbCommentThread = await context.CommentThreadsDb.AsNoTracking().SingleOrDefaultAsync(ct => ct.Id == addedCommentThread.Id);
            CommentThread? savedCommentThread = await commentsService.GetCommentThread(addedCommentThread.Id);

            Assert.NotNull(addedCommentThread);
            Assert.IsType<CommentThread>(addedCommentThread);
            Assert.Equal(0, addedCommentThread.CommentsCount);
            Assert.NotNull(dbCommentThread);
            if (dbCommentThread != null)
            {
                
                Assert.IsType<CommentThread>(dbCommentThread);
                Assert.Equal(0, dbCommentThread.CommentsCount);
            }

            Assert.NotNull(savedCommentThread);
            if (savedCommentThread != null)
            {

                Assert.IsType<CommentThread>(savedCommentThread);
                Assert.Equal(0, savedCommentThread.CommentsCount);
            }
        }

        [Fact]
        public async Task DeleteCommentThread_Should_Remove_CommentThread_Object()
        {
            DbContextOptions<MediaDbContext> dbOptions = new DbContextOptionsBuilder<MediaDbContext>().UseInMemoryDatabase("DeleteCommentThread_Should_Remove_CommentThread_Object").Options;
            await using MediaDbContext context = new MediaDbContext(dbOptions);

            CommentThread commentThread1 = new CommentThread { CommentsCount = 1 };
            CommentThread commentThread2 = new CommentThread { CommentsCount = 1 };

            context.Add(commentThread1);
            context.Add(commentThread2);
            await context.SaveChangesAsync();

            IOptions<MemoryDistributedCacheOptions> memoryCacheOptions = Options.Create(new MemoryDistributedCacheOptions());
            IDistributedCache memoryCache = new MemoryDistributedCache(memoryCacheOptions);
            CommentsService commentsService = new CommentsService(context, memoryCache, _picturesServiceMock.Object, _videosServiceMock.Object);

            int commentThreadsCountBefore = context.CommentThreadsDb.Count();

            CommentThread commentThreadToDelete = await commentsService.GetCommentThread(1);
            await commentsService.DeleteCommentThread(commentThreadToDelete);
            CommentThread? dbCommentThread = await context.CommentThreadsDb.AsNoTracking().SingleOrDefaultAsync(ct => ct.Id == 1);
            CommentThread? savedCommentThread = await commentsService.GetCommentThread(1);

            int commentThreadsCountAfter= context.CommentThreadsDb.Count();

            Assert.Null(dbCommentThread);
            Assert.Null(savedCommentThread);
            Assert.NotEqual(commentThreadsCountBefore, commentThreadsCountAfter);
            Assert.Equal(1, commentThreadsCountAfter);
        }
    }
}
