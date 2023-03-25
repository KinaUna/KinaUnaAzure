using KinaUna.Data.Contexts;
using KinaUna.Data.Models;
using KinaUnaProgenyApi.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace KinaUnaProgenyApi.Tests.Services
{
    public class SkillServiceTests
    {
        [Fact]
        public async Task GetSkill_Should_Return_Skill_Object_When_Id_Is_Valid()
        {
            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>().UseInMemoryDatabase("GetSkill_Should_Return_Skill_Object_When_Id_Is_Valid").Options;
            await using ProgenyDbContext context = new(dbOptions);

            Skill skill1 = new()
            {
                ProgenyId = 1, Author = "User1", AccessLevel = 0, Name = "Skill1", SkillAddedDate = DateTime.UtcNow, Description = "Skill1", Category = "Category1", SkillFirstObservation = DateTime.UtcNow, SkillNumber = 1
            };


            Skill skill2 = new()
            {
                ProgenyId = 1,
                Author = "User1",
                AccessLevel = 0,
                Name = "Skill2",
                SkillAddedDate = DateTime.UtcNow,
                Description = "Skill2",
                Category = "Category2",
                SkillFirstObservation = DateTime.UtcNow,
                SkillNumber = 1
            };

            context.Add(skill1);
            context.Add(skill2);
            await context.SaveChangesAsync();

            IOptions<MemoryDistributedCacheOptions> memoryCacheOptions = Options.Create(new MemoryDistributedCacheOptions());
            IDistributedCache memoryCache = new MemoryDistributedCache(memoryCacheOptions);
            SkillService skillService = new(context, memoryCache);

            Skill resultSkill1 = await skillService.GetSkill(1);
            Skill resultSkill2 = await skillService.GetSkill(1); // Uses cache

            Assert.NotNull(resultSkill1);
            Assert.IsType<Skill>(resultSkill1);
            Assert.Equal(skill1.Author, resultSkill1.Author);
            Assert.Equal(skill1.Name, resultSkill1.Name);
            Assert.Equal(skill1.AccessLevel, resultSkill1.AccessLevel);
            Assert.Equal(skill1.ProgenyId, resultSkill1.ProgenyId);

            Assert.NotNull(resultSkill2);
            Assert.IsType<Skill>(resultSkill2);
            Assert.Equal(skill1.Author, resultSkill2.Author);
            Assert.Equal(skill1.Name, resultSkill2.Name);
            Assert.Equal(skill1.AccessLevel, resultSkill2.AccessLevel);
            Assert.Equal(skill1.ProgenyId, resultSkill2.ProgenyId);
        }

        [Fact]
        public async Task GetSkill_Should_Return_Null_When_Id_Is_Invalid()
        {
            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>().UseInMemoryDatabase("GetSkill_Should_Return_Null_When_Id_Is_Invalid").Options;
            await using ProgenyDbContext context = new(dbOptions);

            Skill skill1 = new()
            {
                ProgenyId = 1,
                Author = "User1",
                AccessLevel = 0,
                Name = "Skill1",
                SkillAddedDate = DateTime.UtcNow,
                Description = "Skill1",
                Category = "Category1",
                SkillFirstObservation = DateTime.UtcNow,
                SkillNumber = 1
            };
            
            context.Add(skill1);
            await context.SaveChangesAsync();

            IOptions<MemoryDistributedCacheOptions> memoryCacheOptions = Options.Create(new MemoryDistributedCacheOptions());
            IDistributedCache memoryCache = new MemoryDistributedCache(memoryCacheOptions);
            SkillService skillService = new(context, memoryCache);

            Skill resultSkill1 = await skillService.GetSkill(2);
            Skill resultSkill2 = await skillService.GetSkill(2); // Using cache
            
            Assert.Null(resultSkill1);
            Assert.Null(resultSkill2);
        }

        [Fact]
        public async Task AddSkill_Should_Save_Skill()
        {
            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>().UseInMemoryDatabase("AddSkill_Should_Save_Skill").Options;
            await using ProgenyDbContext context = new(dbOptions);

            Skill skill1 = new()
            {
                ProgenyId = 1,
                Author = "User1",
                AccessLevel = 0,
                Name = "Skill1",
                SkillAddedDate = DateTime.UtcNow,
                Description = "Skill1",
                Category = "Category1",
                SkillFirstObservation = DateTime.UtcNow,
                SkillNumber = 1
            };

            context.Add(skill1);
            await context.SaveChangesAsync();

            IOptions<MemoryDistributedCacheOptions> memoryCacheOptions = Options.Create(new MemoryDistributedCacheOptions());
            IDistributedCache memoryCache = new MemoryDistributedCache(memoryCacheOptions);
            SkillService skillService = new(context, memoryCache);

            Skill skillToAdd = new()
            {
                ProgenyId = 1,
                Author = "User1",
                AccessLevel = 0,
                Name = "Skill2",
                SkillAddedDate = DateTime.UtcNow,
                Description = "Skill2",
                Category = "Category2",
                SkillFirstObservation = DateTime.UtcNow,
                SkillNumber = 1
            };

            Skill addedSkill = await skillService.AddSkill(skillToAdd);
            Skill? dbSkill = await context.SkillsDb.AsNoTracking().SingleOrDefaultAsync(f => f.SkillId == addedSkill.SkillId);
            Skill savedSkill = await skillService.GetSkill(addedSkill.SkillId);

            Assert.NotNull(addedSkill);
            Assert.IsType<Skill>(addedSkill);
            Assert.Equal(skillToAdd.Author, addedSkill.Author);
            Assert.Equal(skillToAdd.Name, addedSkill.Name);
            Assert.Equal(skillToAdd.AccessLevel, addedSkill.AccessLevel);
            Assert.Equal(skillToAdd.ProgenyId, addedSkill.ProgenyId);

            if (dbSkill != null)
            {
                Assert.IsType<Skill>(dbSkill);
                Assert.Equal(skillToAdd.Author, dbSkill.Author);
                Assert.Equal(skillToAdd.Name, dbSkill.Name);
                Assert.Equal(skillToAdd.AccessLevel, dbSkill.AccessLevel);
                Assert.Equal(skillToAdd.ProgenyId, dbSkill.ProgenyId);
            }
            Assert.NotNull(savedSkill);
            Assert.IsType<Skill>(savedSkill);
            Assert.Equal(skillToAdd.Author, savedSkill.Author);
            Assert.Equal(skillToAdd.Name, savedSkill.Name);
            Assert.Equal(skillToAdd.AccessLevel, savedSkill.AccessLevel);
            Assert.Equal(skillToAdd.ProgenyId, savedSkill.ProgenyId);

        }

        [Fact]
        public async Task UpdateSkill_Should_Save_Skill()
        {
            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>().UseInMemoryDatabase("UpdateSkill_Should_Save_Skill").Options;
            await using ProgenyDbContext context = new(dbOptions);

            Skill skill1 = new()
            {
                ProgenyId = 1,
                Author = "User1",
                AccessLevel = 0,
                Name = "Skill1",
                SkillAddedDate = DateTime.UtcNow,
                Description = "Skill1",
                Category = "Category1",
                SkillFirstObservation = DateTime.UtcNow,
                SkillNumber = 1
            };

            Skill skill2 = new()
            {
                ProgenyId = 1,
                Author = "User1",
                AccessLevel = 0,
                Name = "Skill2",
                SkillAddedDate = DateTime.UtcNow,
                Description = "Skill2",
                Category = "Category2",
                SkillFirstObservation = DateTime.UtcNow,
                SkillNumber = 1
            };
            context.Add(skill1);
            context.Add(skill2);
            await context.SaveChangesAsync();

            IOptions<MemoryDistributedCacheOptions> memoryCacheOptions = Options.Create(new MemoryDistributedCacheOptions());
            IDistributedCache memoryCache = new MemoryDistributedCache(memoryCacheOptions);
            SkillService skillService = new(context, memoryCache);

            Skill skillToUpdate = await skillService.GetSkill(1);
            skillToUpdate.AccessLevel = 5;
            Skill updatedSkill = await skillService.UpdateSkill(skillToUpdate);
            Skill? dbSkill = await context.SkillsDb.AsNoTracking().SingleOrDefaultAsync(f => f.SkillId == 1);
            Skill savedSkill = await skillService.GetSkill(1);

            Assert.NotNull(updatedSkill);
            Assert.IsType<Skill>(updatedSkill);
            Assert.NotEqual(0, updatedSkill.SkillId);
            Assert.Equal("User1", updatedSkill.Author);
            Assert.Equal(5, updatedSkill.AccessLevel);
            Assert.Equal(1, updatedSkill.ProgenyId);

            if (dbSkill != null)
            {
                Assert.IsType<Skill>(dbSkill);
                Assert.NotEqual(0, dbSkill.SkillId);
                Assert.Equal("User1", dbSkill.Author);
                Assert.Equal(5, dbSkill.AccessLevel);
                Assert.Equal(1, dbSkill.ProgenyId);
            }

            Assert.NotNull(savedSkill);
            Assert.IsType<Skill>(savedSkill);
            Assert.NotEqual(0, savedSkill.SkillId);
            Assert.Equal("User1", savedSkill.Author);
            Assert.Equal(5, savedSkill.AccessLevel);
            Assert.Equal(1, savedSkill.ProgenyId);
        }

        [Fact]
        public async Task DeleteSkill_Should_Remove_Skill()
        {
            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>().UseInMemoryDatabase("DeleteSkill_Should_Remove_Skill").Options;
            await using ProgenyDbContext context = new(dbOptions);

            Skill skill1 = new()
            {
                ProgenyId = 1,
                Author = "User1",
                AccessLevel = 0,
                Name = "Skill1",
                SkillAddedDate = DateTime.UtcNow,
                Description = "Skill1",
                Category = "Category1",
                SkillFirstObservation = DateTime.UtcNow,
                SkillNumber = 1
            };

            Skill skill2 = new()
            {
                ProgenyId = 1,
                Author = "User1",
                AccessLevel = 0,
                Name = "Skill2",
                SkillAddedDate = DateTime.UtcNow,
                Description = "Skill2",
                Category = "Category2",
                SkillFirstObservation = DateTime.UtcNow,
                SkillNumber = 1
            };

            context.Add(skill1);
            context.Add(skill2);
            await context.SaveChangesAsync();

            IOptions<MemoryDistributedCacheOptions> memoryCacheOptions = Options.Create(new MemoryDistributedCacheOptions());
            IDistributedCache memoryCache = new MemoryDistributedCache(memoryCacheOptions);
            SkillService skillService = new(context, memoryCache);

            int skillItemsCountBeforeDelete = context.SkillsDb.Count();
            Skill skillToDelete = await skillService.GetSkill(1);

            await skillService.DeleteSkill(skillToDelete);
            Skill? deletedSkill = await context.SkillsDb.SingleOrDefaultAsync(f => f.SkillId == 1);
            int skillItemsCountAfterDelete = context.SkillsDb.Count();

            Assert.Null(deletedSkill);
            Assert.Equal(2, skillItemsCountBeforeDelete);
            Assert.Equal(1, skillItemsCountAfterDelete);
        }

        [Fact]
        public async Task GetSkillsList_Should_Return_List_Of_Skill_When_Progeny_Has_Saved_Skills()
        {
            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>().UseInMemoryDatabase("GetSkillsList_Should_Return_List_Of_Skill_When_Progeny_Has_Saved_Skills").Options;
            await using ProgenyDbContext context = new(dbOptions);

            Skill skill1 = new()
            {
                ProgenyId = 1,
                Author = "User1",
                AccessLevel = 0,
                Name = "Skill1",
                SkillAddedDate = DateTime.UtcNow,
                Description = "Skill1",
                Category = "Category1",
                SkillFirstObservation = DateTime.UtcNow,
                SkillNumber = 1
            };
            
            Skill skill2 = new()
            {
                ProgenyId = 1,
                Author = "User1",
                AccessLevel = 0,
                Name = "Skill2",
                SkillAddedDate = DateTime.UtcNow,
                Description = "Skill2",
                Category = "Category2",
                SkillFirstObservation = DateTime.UtcNow,
                SkillNumber = 1
            };

            context.Add(skill1);
            context.Add(skill2);
            await context.SaveChangesAsync();

            IOptions<MemoryDistributedCacheOptions> memoryCacheOptions = Options.Create(new MemoryDistributedCacheOptions());
            IDistributedCache memoryCache = new MemoryDistributedCache(memoryCacheOptions);
            SkillService skillService = new(context, memoryCache);

            List<Skill> skillsList = await skillService.GetSkillsList(1);
            List<Skill> skillsList2 = await skillService.GetSkillsList(1); // Test cached result.
            Skill firstSkill = skillsList.First();

            Assert.NotNull(skillsList);
            Assert.IsType<List<Skill>>(skillsList);
            Assert.Equal(2, skillsList.Count);
            Assert.NotNull(skillsList2);
            Assert.IsType<List<Skill>>(skillsList2);
            Assert.Equal(2, skillsList2.Count);
            Assert.NotNull(firstSkill);
            Assert.IsType<Skill>(firstSkill);
        }

        [Fact]
        public async Task GetSkillsList_Should_Return_Empty_List_Of_Skill_When_Progeny_Has_No_Saved_Skills()
        {
            
            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>().UseInMemoryDatabase("GetSkillsList_Should_Return_Empty_List_Of_Skill_When_Progeny_Has_No_Saved_Skills").Options;
            await using ProgenyDbContext context = new(dbOptions);

            Skill skill1 = new()
            {
                ProgenyId = 1,
                Author = "User1",
                AccessLevel = 0,
                Name = "Skill1",
                SkillAddedDate = DateTime.UtcNow,
                Description = "Skill1",
                Category = "Category1",
                SkillFirstObservation = DateTime.UtcNow,
                SkillNumber = 1
            };
            
            Skill skill2 = new()
            {
                ProgenyId = 1,
                Author = "User1",
                AccessLevel = 0,
                Name = "Skill2",
                SkillAddedDate = DateTime.UtcNow,
                Description = "Skill2",
                Category = "Category2",
                SkillFirstObservation = DateTime.UtcNow,
                SkillNumber = 1
            };

            context.Add(skill1);
            context.Add(skill2);
            await context.SaveChangesAsync();

            IOptions<MemoryDistributedCacheOptions> memoryCacheOptions = Options.Create(new MemoryDistributedCacheOptions());
            IDistributedCache memoryCache = new MemoryDistributedCache(memoryCacheOptions);
            SkillService skillService = new(context, memoryCache);

            List<Skill> skillsList = await skillService.GetSkillsList(2);
            List<Skill> skillsList2 = await skillService.GetSkillsList(2); // Test cached result.

            Assert.NotNull(skillsList);
            Assert.IsType<List<Skill>>(skillsList);
            Assert.Empty(skillsList);
            Assert.NotNull(skillsList2);
            Assert.IsType<List<Skill>>(skillsList2);
            Assert.Empty(skillsList2);
        }
    }
}
