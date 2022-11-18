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
    public class NoteServiceTests
    {
        [Fact]
        public async Task GetNote_Returns_Note_Object_When_Id_Is_Valid()
        {
            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>().UseInMemoryDatabase("GetNote_Returns_Note_Object_When_Id_Is_Valid").Options;
            await using ProgenyDbContext context = new ProgenyDbContext(dbOptions);

            Note note1 = new Note
            {
                ProgenyId = 1, AccessLevel = 0, Category = "Category1", Content = "Content1", CreatedDate = DateTime.UtcNow, NoteNumber = 1, Owner = "User1", Title = "Title1"
            };


            Note note2 = new Note
            {
                ProgenyId = 1,
                AccessLevel = 0,
                Category = "Category2",
                Content = "Content2",
                CreatedDate = DateTime.UtcNow,
                NoteNumber = 1,
                Owner = "User1",
                Title = "Title2"
            };

            context.Add(note1);
            context.Add(note2);
            await context.SaveChangesAsync();

            IOptions<MemoryDistributedCacheOptions>? memoryCacheOptions = Options.Create(new MemoryDistributedCacheOptions());
            IDistributedCache memoryCache = new MemoryDistributedCache(memoryCacheOptions);
            NoteService noteService = new NoteService(context, memoryCache);

            Note resultNote1 = await noteService.GetNote(1);
            Note resultNote2 = await noteService.GetNote(1); // Uses cache

            Assert.NotNull(resultNote1);
            Assert.IsType<Note>(resultNote1);
            Assert.Equal(note1.Owner, resultNote1.Owner);
            Assert.Equal(note1.Title, resultNote1.Title);
            Assert.Equal(note1.AccessLevel, resultNote1.AccessLevel);
            Assert.Equal(note1.ProgenyId, resultNote1.ProgenyId);

            Assert.NotNull(resultNote2);
            Assert.IsType<Note>(resultNote2);
            Assert.Equal(note1.Owner, resultNote2.Owner);
            Assert.Equal(note1.Title, resultNote2.Title);
            Assert.Equal(note1.AccessLevel, resultNote2.AccessLevel);
            Assert.Equal(note1.ProgenyId, resultNote2.ProgenyId);
        }

        [Fact]
        public async Task GetNote_Should_Return_Null_When_Id_Is_Invalid()
        {
            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>().UseInMemoryDatabase("GetNote_Should_Return_Null_When_Id_Is_Invalid").Options;
            await using ProgenyDbContext context = new ProgenyDbContext(dbOptions);

            Note note1 = new Note
            {
                ProgenyId = 1,
                AccessLevel = 0,
                Category = "Category1",
                Content = "Content1",
                CreatedDate = DateTime.UtcNow,
                NoteNumber = 1,
                Owner = "User1",
                Title = "Title1"
            };
            
            context.Add(note1);
            await context.SaveChangesAsync();

            IOptions<MemoryDistributedCacheOptions>? memoryCacheOptions = Options.Create(new MemoryDistributedCacheOptions());
            IDistributedCache memoryCache = new MemoryDistributedCache(memoryCacheOptions);
            NoteService noteService = new NoteService(context, memoryCache);

            Note resultNote1 = await noteService.GetNote(2);
            Note resultNote2 = await noteService.GetNote(2); // Using cache
            
            Assert.Null(resultNote1);
            Assert.Null(resultNote2);
        }

        [Fact]
        public async Task AddNote_Should_Save_Note()
        {
            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>().UseInMemoryDatabase("AddNote_Should_Save_Note").Options;
            await using ProgenyDbContext context = new ProgenyDbContext(dbOptions);

            Note note1 = new Note
            {
                ProgenyId = 1,
                AccessLevel = 0,
                Category = "Category1",
                Content = "Content1",
                CreatedDate = DateTime.UtcNow,
                NoteNumber = 1,
                Owner = "User1",
                Title = "Title1"
            };

            context.Add(note1);
            await context.SaveChangesAsync();

            IOptions<MemoryDistributedCacheOptions>? memoryCacheOptions = Options.Create(new MemoryDistributedCacheOptions());
            IDistributedCache memoryCache = new MemoryDistributedCache(memoryCacheOptions);
            NoteService noteService = new NoteService(context, memoryCache);

            Note noteToAdd = new Note
            {
                ProgenyId = 1,
                AccessLevel = 0,
                Category = "Category2",
                Content = "Content2",
                CreatedDate = DateTime.UtcNow,
                NoteNumber = 1,
                Owner = "User1",
                Title = "Title2"
            };

            Note addedNote = await noteService.AddNote(noteToAdd);
            Note? dbNote = await context.NotesDb.AsNoTracking().SingleOrDefaultAsync(f => f.NoteId == addedNote.NoteId);
            Note savedNote = await noteService.GetNote(addedNote.NoteId);

            Assert.NotNull(addedNote);
            Assert.IsType<Note>(addedNote);
            Assert.Equal(noteToAdd.Owner, addedNote.Owner);
            Assert.Equal(noteToAdd.Title, addedNote.Title);
            Assert.Equal(noteToAdd.AccessLevel, addedNote.AccessLevel);
            Assert.Equal(noteToAdd.ProgenyId, addedNote.ProgenyId);

            if (dbNote != null)
            {
                Assert.IsType<Note>(dbNote);
                Assert.Equal(noteToAdd.Owner, dbNote.Owner);
                Assert.Equal(noteToAdd.Title, dbNote.Title);
                Assert.Equal(noteToAdd.AccessLevel, dbNote.AccessLevel);
                Assert.Equal(noteToAdd.ProgenyId, dbNote.ProgenyId);
            }
            Assert.NotNull(savedNote);
            Assert.IsType<Note>(savedNote);
            Assert.Equal(noteToAdd.Owner, savedNote.Owner);
            Assert.Equal(noteToAdd.Title, savedNote.Title);
            Assert.Equal(noteToAdd.AccessLevel, savedNote.AccessLevel);
            Assert.Equal(noteToAdd.ProgenyId, savedNote.ProgenyId);

        }

        [Fact]
        public async Task UpdateNote_Should_Save_Note()
        {
            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>().UseInMemoryDatabase("UpdateNote_Should_Save_Note").Options;
            await using ProgenyDbContext context = new ProgenyDbContext(dbOptions);

            Note note1 = new Note
            {
                ProgenyId = 1,
                AccessLevel = 0,
                Category = "Category1",
                Content = "Content1",
                CreatedDate = DateTime.UtcNow,
                NoteNumber = 1,
                Owner = "User1",
                Title = "Title1"
            };

            Note note2 = new Note
            {
                ProgenyId = 1,
                AccessLevel = 0,
                Category = "Category2",
                Content = "Content2",
                CreatedDate = DateTime.UtcNow,
                NoteNumber = 1,
                Owner = "User1",
                Title = "Title2"
            };
            context.Add(note1);
            context.Add(note2);
            await context.SaveChangesAsync();

            IOptions<MemoryDistributedCacheOptions>? memoryCacheOptions = Options.Create(new MemoryDistributedCacheOptions());
            IDistributedCache memoryCache = new MemoryDistributedCache(memoryCacheOptions);
            NoteService noteService = new NoteService(context, memoryCache);

            Note noteToUpdate = await noteService.GetNote(1);
            noteToUpdate.AccessLevel = 5;
            Note updatedNote = await noteService.UpdateNote(noteToUpdate);
            Note? dbNote = await context.NotesDb.AsNoTracking().SingleOrDefaultAsync(f => f.NoteId == 1);
            Note savedNote = await noteService.GetNote(1);

            Assert.NotNull(updatedNote);
            Assert.IsType<Note>(updatedNote);
            Assert.NotEqual(0, updatedNote.NoteId);
            Assert.Equal("User1", updatedNote.Owner);
            Assert.Equal(5, updatedNote.AccessLevel);
            Assert.Equal(1, updatedNote.ProgenyId);

            if (dbNote != null)
            {
                Assert.IsType<Note>(dbNote);
                Assert.NotEqual(0, dbNote.NoteId);
                Assert.Equal("User1", dbNote.Owner);
                Assert.Equal(5, dbNote.AccessLevel);
                Assert.Equal(1, dbNote.ProgenyId);
            }

            Assert.NotNull(savedNote);
            Assert.IsType<Note>(savedNote);
            Assert.NotEqual(0, savedNote.NoteId);
            Assert.Equal("User1", savedNote.Owner);
            Assert.Equal(5, savedNote.AccessLevel);
            Assert.Equal(1, savedNote.ProgenyId);
        }

        [Fact]
        public async Task DeleteNote_Should_Remove_Note()
        {
            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>().UseInMemoryDatabase("DeleteNote_Should_Remove_Note").Options;
            await using ProgenyDbContext context = new ProgenyDbContext(dbOptions);

            Note note1 = new Note
            {
                ProgenyId = 1,
                AccessLevel = 0,
                Category = "Category1",
                Content = "Content1",
                CreatedDate = DateTime.UtcNow,
                NoteNumber = 1,
                Owner = "User1",
                Title = "Title1"
            };

            Note note2 = new Note
            {
                ProgenyId = 1,
                AccessLevel = 0,
                Category = "Category2",
                Content = "Content2",
                CreatedDate = DateTime.UtcNow,
                NoteNumber = 1,
                Owner = "User1",
                Title = "Title2"
            };
            context.Add(note1);
            context.Add(note2);
            await context.SaveChangesAsync();

            IOptions<MemoryDistributedCacheOptions>? memoryCacheOptions = Options.Create(new MemoryDistributedCacheOptions());
            IDistributedCache memoryCache = new MemoryDistributedCache(memoryCacheOptions);
            NoteService noteService = new NoteService(context, memoryCache);

            int noteItemsCountBeforeDelete = context.NotesDb.Count();
            Note noteToDelete = await noteService.GetNote(1);

            await noteService.DeleteNote(noteToDelete);
            Note? deletedNote = await context.NotesDb.SingleOrDefaultAsync(f => f.NoteId == 1);
            int noteItemsCountAfterDelete = context.NotesDb.Count();

            Assert.Null(deletedNote);
            Assert.Equal(2, noteItemsCountBeforeDelete);
            Assert.Equal(1, noteItemsCountAfterDelete);
        }

        [Fact]
        public async Task GetNotesList_Should_Return_List_Of_Note_When_Progeny_Has_Saved_Notes()
        {
            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>().UseInMemoryDatabase("GetNotesList_Should_Return_List_Of_Note_When_Progeny_Has_Saved_Notes").Options;
            await using ProgenyDbContext context = new ProgenyDbContext(dbOptions);

            Note note1 = new Note
            {
                ProgenyId = 1,
                AccessLevel = 0,
                Category = "Category1",
                Content = "Content1",
                CreatedDate = DateTime.UtcNow,
                NoteNumber = 1,
                Owner = "User1",
                Title = "Title1"
            };

            Note note2 = new Note
            {
                ProgenyId = 1,
                AccessLevel = 0,
                Category = "Category2",
                Content = "Content2",
                CreatedDate = DateTime.UtcNow,
                NoteNumber = 1,
                Owner = "User1",
                Title = "Title2"
            };

            context.Add(note1);
            context.Add(note2);
            await context.SaveChangesAsync();

            IOptions<MemoryDistributedCacheOptions>? memoryCacheOptions = Options.Create(new MemoryDistributedCacheOptions());
            IDistributedCache memoryCache = new MemoryDistributedCache(memoryCacheOptions);
            NoteService noteService = new NoteService(context, memoryCache);

            List<Note> notesList = await noteService.GetNotesList(1);
            List<Note> notesList2 = await noteService.GetNotesList(1); // Test cached result.
            Note firstNote = notesList.First();

            Assert.NotNull(notesList);
            Assert.IsType<List<Note>>(notesList);
            Assert.Equal(2, notesList.Count);
            Assert.NotNull(notesList2);
            Assert.IsType<List<Note>>(notesList2);
            Assert.Equal(2, notesList2.Count);
            Assert.NotNull(firstNote);
            Assert.IsType<Note>(firstNote);
        }

        [Fact]
        public async Task GetNotesList_Should_Return_Empty_List_Of_Note_When_Progeny_Has_No_Saved_Notes()
        {
            
            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>().UseInMemoryDatabase("GetNotesList_Should_Return_Empty_List_Of_Note_When_Progeny_Has_No_Saved_Notes").Options;
            await using ProgenyDbContext context = new ProgenyDbContext(dbOptions);

            Note note1 = new Note
            {
                ProgenyId = 1,
                AccessLevel = 0,
                Category = "Category1",
                Content = "Content1",
                CreatedDate = DateTime.UtcNow,
                NoteNumber = 1,
                Owner = "User1",
                Title = "Title1"
            };

            Note note2 = new Note
            {
                ProgenyId = 1,
                AccessLevel = 0,
                Category = "Category2",
                Content = "Content2",
                CreatedDate = DateTime.UtcNow,
                NoteNumber = 2,
                Owner = "User1",
                Title = "Title2"
            };

            context.Add(note1);
            context.Add(note2);
            await context.SaveChangesAsync();

            IOptions<MemoryDistributedCacheOptions>? memoryCacheOptions = Options.Create(new MemoryDistributedCacheOptions());
            IDistributedCache memoryCache = new MemoryDistributedCache(memoryCacheOptions);
            NoteService noteService = new NoteService(context, memoryCache);

            List<Note> notesList = await noteService.GetNotesList(2);
            List<Note> notesList2 = await noteService.GetNotesList(2); // Test cached result.

            Assert.NotNull(notesList);
            Assert.IsType<List<Note>>(notesList);
            Assert.Empty(notesList);
            Assert.NotNull(notesList2);
            Assert.IsType<List<Note>>(notesList2);
            Assert.Empty(notesList2);
        }
    }
}
