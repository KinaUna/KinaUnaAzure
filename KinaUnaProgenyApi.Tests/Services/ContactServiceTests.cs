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
    public class ContactServiceTests
    {
        [Fact]
        public async Task GetContact_Returns_Contact_Object_When_Id_Is_Valid()
        {
            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>().UseInMemoryDatabase("GetContact_Returns_Contact_Object_When_Id_Is_Valid").Options;
            await using ProgenyDbContext context = new ProgenyDbContext(dbOptions);

            Contact contact1 = new Contact
            {
                ProgenyId = 1, Author = "User1", AccessLevel = 0, Active = true, Context = "Testing", DisplayName = "Contact1", DateAdded = DateTime.UtcNow, Email1 = "testcontact1@test.com",
                FirstName = "First1", MiddleName = "Middle1", LastName = "Last1", PictureLink = Constants.ProfilePictureUrl, Tags = "Tag1, Tag2", MobileNumber = "111222333444", Notes = "Note1",
                Website = "https://test1.com"
            };
            Contact contact2 = new Contact
            {
                ProgenyId = 1,
                Author = "User2",
                AccessLevel = 0,
                Active = true,
                Context = "Testing",
                DisplayName = "Contact2",
                DateAdded = DateTime.UtcNow,
                Email1 = "testcontact2@test.com",
                FirstName = "First2",
                MiddleName = "Middle2",
                LastName = "Last2",
                PictureLink = Constants.ProfilePictureUrl,
                Tags = "Tag1, Tag2",
                MobileNumber = "222333444555",
                Notes = "Note2",
                Website = "https://test2.com"
            };

            context.Add(contact1);
            context.Add(contact2);
            await context.SaveChangesAsync();

            IOptions<MemoryDistributedCacheOptions>? memoryCacheOptions = Options.Create(new MemoryDistributedCacheOptions());
            IDistributedCache memoryCache = new MemoryDistributedCache(memoryCacheOptions);
            ContactService contactService = new ContactService(context, memoryCache);

            Contact resultContact1 = await contactService.GetContact(1);
            Contact resultContact2 = await contactService.GetContact(1); // Uses cache

            Assert.NotNull(resultContact1);
            Assert.IsType<Contact>(resultContact1);
            Assert.Equal(contact1.Author, resultContact1.Author);
            Assert.Equal(contact1.DisplayName, contact1.DisplayName);
            Assert.Equal(contact1.AccessLevel, resultContact1.AccessLevel);
            Assert.Equal(contact1.ProgenyId, resultContact1.ProgenyId);

            Assert.NotNull(resultContact2);
            Assert.IsType<Contact>(resultContact2);
            Assert.Equal(contact1.Author, resultContact2.Author);
            Assert.Equal(contact1.DisplayName, resultContact2.DisplayName);
            Assert.Equal(contact1.AccessLevel, resultContact2.AccessLevel);
            Assert.Equal(contact1.ProgenyId, resultContact2.ProgenyId);
        }

        [Fact]
        public async Task GetContact_Should_Return_Null_When_Id_Is_Invalid()
        {
            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>().UseInMemoryDatabase("GetContact_Should_Return_Null_When_Id_Is_Invalid").Options;
            await using ProgenyDbContext context = new ProgenyDbContext(dbOptions);

            Contact contact1 = new Contact
            {
                ProgenyId = 1,
                Author = "User1",
                AccessLevel = 0,
                Active = true,
                Context = "Testing",
                DisplayName = "Contact1",
                DateAdded = DateTime.UtcNow,
                Email1 = "testcontact1@test.com",
                FirstName = "First1",
                MiddleName = "Middle1",
                LastName = "Last1",
                PictureLink = Constants.ProfilePictureUrl,
                Tags = "Tag1, Tag2",
                MobileNumber = "111222333444",
                Notes = "Note1",
                Website = "https://test1.com"
            };
            context.Add(contact1);
            await context.SaveChangesAsync();

            IOptions<MemoryDistributedCacheOptions>? memoryCacheOptions = Options.Create(new MemoryDistributedCacheOptions());
            IDistributedCache memoryCache = new MemoryDistributedCache(memoryCacheOptions);
            ContactService contactService = new ContactService(context, memoryCache);

            Contact resultContact1 = await contactService.GetContact(2);
            Contact resultContact2 = await contactService.GetContact(2);
            
            Assert.Null(resultContact1);
            Assert.Null(resultContact2);
        }

        [Fact]
        public async Task AddContact_Should_Save_Contact()
        {
            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>().UseInMemoryDatabase("AddContact_Should_Save_Contact").Options;
            await using ProgenyDbContext context = new ProgenyDbContext(dbOptions);

            Contact contact1 = new Contact
            {
                ProgenyId = 1,
                Author = "User1",
                AccessLevel = 0,
                Active = true,
                Context = "Testing",
                DisplayName = "Contact1",
                DateAdded = DateTime.UtcNow,
                Email1 = "testcontact1@test.com",
                FirstName = "First1",
                MiddleName = "Middle1",
                LastName = "Last1",
                PictureLink = Constants.ProfilePictureUrl,
                Tags = "Tag1, Tag2",
                MobileNumber = "111222333444",
                Notes = "Note1",
                Website = "https://test1.com"
            };
            context.Add(contact1);
            await context.SaveChangesAsync();

            IOptions<MemoryDistributedCacheOptions>? memoryCacheOptions = Options.Create(new MemoryDistributedCacheOptions());
            IDistributedCache memoryCache = new MemoryDistributedCache(memoryCacheOptions);
            ContactService contactService = new ContactService(context, memoryCache);

            Contact contactToAdd = new Contact
            {
                ProgenyId = 1,
                Author = "User2",
                AccessLevel = 0,
                Active = true,
                Context = "Testing",
                DisplayName = "Contact2",
                DateAdded = DateTime.UtcNow,
                Email1 = "testcontact2@test.com",
                FirstName = "First2",
                MiddleName = "Middle2",
                LastName = "Last2",
                PictureLink = Constants.ProfilePictureUrl,
                Tags = "Tag1, Tag2",
                MobileNumber = "222333444555",
                Notes = "Note2",
                Website = "https://test2.com"
            };

            Contact addedContact = await contactService.AddContact(contactToAdd);
            Contact? dbContact = await context.ContactsDb.AsNoTracking().SingleOrDefaultAsync(c => c.ContactId == addedContact.ContactId);
            Contact savedContact = await contactService.GetContact(addedContact.ContactId);

            Assert.NotNull(addedContact);
            Assert.IsType<Contact>(addedContact);
            Assert.Equal(contactToAdd.Author, addedContact.Author);
            Assert.Equal(contactToAdd.DisplayName, addedContact.DisplayName);
            Assert.Equal(contactToAdd.AccessLevel, addedContact.AccessLevel);
            Assert.Equal(contactToAdd.ProgenyId, addedContact.ProgenyId);

            if (dbContact != null)
            {
                Assert.IsType<Contact>(dbContact);
                Assert.Equal(contactToAdd.Author, dbContact.Author);
                Assert.Equal(contactToAdd.DisplayName, dbContact.DisplayName);
                Assert.Equal(contactToAdd.AccessLevel, dbContact.AccessLevel);
                Assert.Equal(contactToAdd.ProgenyId, dbContact.ProgenyId);
            }
            Assert.NotNull(savedContact);
            Assert.IsType<Contact>(savedContact);
            Assert.Equal(contactToAdd.Author, savedContact.Author);
            Assert.Equal(contactToAdd.DisplayName, savedContact.DisplayName);
            Assert.Equal(contactToAdd.AccessLevel, savedContact.AccessLevel);
            Assert.Equal(contactToAdd.ProgenyId, savedContact.ProgenyId);

        }

        [Fact]
        public async Task UpdateContact_Should_Save_Contact()
        {
            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>().UseInMemoryDatabase("UpdateContact_Should_Save_Contact").Options;
            await using ProgenyDbContext context = new ProgenyDbContext(dbOptions);

            Contact contact1 = new Contact
            {
                ProgenyId = 1,
                Author = "User1",
                AccessLevel = 0,
                Active = true,
                Context = "Testing",
                DisplayName = "Contact1",
                DateAdded = DateTime.UtcNow,
                Email1 = "testcontact1@test.com",
                FirstName = "First1",
                MiddleName = "Middle1",
                LastName = "Last1",
                PictureLink = Constants.ProfilePictureUrl,
                Tags = "Tag1, Tag2",
                MobileNumber = "111222333444",
                Notes = "Note1",
                Website = "https://test1.com"
            };
            Contact contact2 = new Contact
            {
                ProgenyId = 1,
                Author = "User2",
                AccessLevel = 0,
                Active = true,
                Context = "Testing",
                DisplayName = "Contact2",
                DateAdded = DateTime.UtcNow,
                Email1 = "testcontact2@test.com",
                FirstName = "First2",
                MiddleName = "Middle2",
                LastName = "Last2",
                PictureLink = Constants.ProfilePictureUrl,
                Tags = "Tag1, Tag2",
                MobileNumber = "222333444555",
                Notes = "Note2",
                Website = "https://test2.com"
            };

            context.Add(contact1);
            context.Add(contact2);
            await context.SaveChangesAsync();

            IOptions<MemoryDistributedCacheOptions>? memoryCacheOptions = Options.Create(new MemoryDistributedCacheOptions());
            IDistributedCache memoryCache = new MemoryDistributedCache(memoryCacheOptions);
            ContactService contactService = new ContactService(context, memoryCache);

            Contact contactToUpdate = await contactService.GetContact(1);
            contactToUpdate.AccessLevel = 5;
            Contact updatedContact = await contactService.UpdateContact(contactToUpdate);
            Contact? dbContact = await context.ContactsDb.AsNoTracking().SingleOrDefaultAsync(c => c.ContactId == 1);
            Contact savedContact = await contactService.GetContact(1);

            Assert.NotNull(updatedContact);
            Assert.IsType<Contact>(updatedContact);
            Assert.NotEqual(0, updatedContact.ContactId);
            Assert.Equal("User1", updatedContact.Author);
            Assert.Equal(5, updatedContact.AccessLevel);
            Assert.Equal(1, updatedContact.ProgenyId);

            if (dbContact != null)
            {
                Assert.IsType<Contact>(dbContact);
                Assert.NotEqual(0, dbContact.ContactId);
                Assert.Equal("User1", dbContact.Author);
                Assert.Equal(5, dbContact.AccessLevel);
                Assert.Equal(1, dbContact.ProgenyId);
            }

            Assert.NotNull(savedContact);
            Assert.IsType<Contact>(savedContact);
            Assert.NotEqual(0, savedContact.ContactId);
            Assert.Equal("User1", savedContact.Author);
            Assert.Equal(5, savedContact.AccessLevel);
            Assert.Equal(1, savedContact.ProgenyId);
        }

        [Fact]
        public async Task DeleteContact_Should_Remove_Contact()
        {
            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>().UseInMemoryDatabase("DeleteContact_Should_Remove_Contact").Options;
            await using ProgenyDbContext context = new ProgenyDbContext(dbOptions);

            Contact contact1 = new Contact
            {
                ProgenyId = 1,
                Author = "User1",
                AccessLevel = 0,
                Active = true,
                Context = "Testing",
                DisplayName = "Contact1",
                DateAdded = DateTime.UtcNow,
                Email1 = "testcontact1@test.com",
                FirstName = "First1",
                MiddleName = "Middle1",
                LastName = "Last1",
                PictureLink = Constants.ProfilePictureUrl,
                Tags = "Tag1, Tag2",
                MobileNumber = "111222333444",
                Notes = "Note1",
                Website = "https://test1.com"
            };
            Contact contact2 = new Contact
            {
                ProgenyId = 1,
                Author = "User2",
                AccessLevel = 0,
                Active = true,
                Context = "Testing",
                DisplayName = "Contact2",
                DateAdded = DateTime.UtcNow,
                Email1 = "testcontact2@test.com",
                FirstName = "First2",
                MiddleName = "Middle2",
                LastName = "Last2",
                PictureLink = Constants.ProfilePictureUrl,
                Tags = "Tag1, Tag2",
                MobileNumber = "222333444555",
                Notes = "Note2",
                Website = "https://test2.com"
            };

            context.Add(contact1);
            context.Add(contact2);
            await context.SaveChangesAsync();

            IOptions<MemoryDistributedCacheOptions>? memoryCacheOptions = Options.Create(new MemoryDistributedCacheOptions());
            IDistributedCache memoryCache = new MemoryDistributedCache(memoryCacheOptions);
            ContactService contactService = new ContactService(context, memoryCache);

            int contactItemsCountBeforeDelete = context.ContactsDb.Count();
            Contact contactToDelete = await contactService.GetContact(1);

            await contactService.DeleteContact(contactToDelete);
            Contact? deletedContact = await context.ContactsDb.SingleOrDefaultAsync(c => c.ContactId == 1);
            int contactItemsCountAfterDelete = context.ContactsDb.Count();

            Assert.Null(deletedContact);
            Assert.Equal(2, contactItemsCountBeforeDelete);
            Assert.Equal(1, contactItemsCountAfterDelete);
        }

        [Fact]
        public async Task GetContactsList_Should_Return_List_Of_Contact_When_Progeny_Has_Saved_Contacts()
        {
            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>().UseInMemoryDatabase("GetContactsList_Should_Return_List_Of_Contact_When_Progeny_Has_Saved_Contacts").Options;
            await using ProgenyDbContext context = new ProgenyDbContext(dbOptions);

            Contact contact1 = new Contact
            {
                ProgenyId = 1,
                Author = "User1",
                AccessLevel = 0,
                Active = true,
                Context = "Testing",
                DisplayName = "Contact1",
                DateAdded = DateTime.UtcNow,
                Email1 = "testcontact1@test.com",
                FirstName = "First1",
                MiddleName = "Middle1",
                LastName = "Last1",
                PictureLink = Constants.ProfilePictureUrl,
                Tags = "Tag1, Tag2",
                MobileNumber = "111222333444",
                Notes = "Note1",
                Website = "https://test1.com"
            };
            Contact contact2 = new Contact
            {
                ProgenyId = 1,
                Author = "User2",
                AccessLevel = 0,
                Active = true,
                Context = "Testing",
                DisplayName = "Contact2",
                DateAdded = DateTime.UtcNow,
                Email1 = "testcontact2@test.com",
                FirstName = "First2",
                MiddleName = "Middle2",
                LastName = "Last2",
                PictureLink = Constants.ProfilePictureUrl,
                Tags = "Tag1, Tag2",
                MobileNumber = "222333444555",
                Notes = "Note2",
                Website = "https://test2.com"
            };

            context.Add(contact1);
            context.Add(contact2);
            await context.SaveChangesAsync();

            IOptions<MemoryDistributedCacheOptions>? memoryCacheOptions = Options.Create(new MemoryDistributedCacheOptions());
            IDistributedCache memoryCache = new MemoryDistributedCache(memoryCacheOptions);
            ContactService contactService = new ContactService(context, memoryCache);

            List<Contact> contactsList = await contactService.GetContactsList(1);
            List<Contact> contactsList2 = await contactService.GetContactsList(1); // Test cached result.
            Contact firstContact = contactsList.First();

            Assert.NotNull(contactsList);
            Assert.IsType<List<Contact>>(contactsList);
            Assert.Equal(2, contactsList.Count);
            Assert.NotNull(contactsList2);
            Assert.IsType<List<Contact>>(contactsList2);
            Assert.Equal(2, contactsList2.Count);
            Assert.NotNull(firstContact);
            Assert.IsType<Contact>(firstContact);

        }

        [Fact]
        public async Task GetContactsList_Should_Return_Empty_List_Of_Contact_When_Progeny_Has_No_Saved_Contacts()
        {
            
            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>().UseInMemoryDatabase("GetContactsList_Should_Return_Empty_List_Of_Contact_When_Progeny_Has_No_Saved_Contacts").Options;
            await using ProgenyDbContext context = new ProgenyDbContext(dbOptions);

            Contact contact1 = new Contact
            {
                ProgenyId = 1,
                Author = "User1",
                AccessLevel = 0,
                Active = true,
                Context = "Testing",
                DisplayName = "Contact1",
                DateAdded = DateTime.UtcNow,
                Email1 = "testcontact1@test.com",
                FirstName = "First1",
                MiddleName = "Middle1",
                LastName = "Last1",
                PictureLink = Constants.ProfilePictureUrl,
                Tags = "Tag1, Tag2",
                MobileNumber = "111222333444",
                Notes = "Note1",
                Website = "https://test1.com"
            };
            Contact contact2 = new Contact
            {
                ProgenyId = 1,
                Author = "User2",
                AccessLevel = 0,
                Active = true,
                Context = "Testing",
                DisplayName = "Contact2",
                DateAdded = DateTime.UtcNow,
                Email1 = "testcontact2@test.com",
                FirstName = "First2",
                MiddleName = "Middle2",
                LastName = "Last2",
                PictureLink = Constants.ProfilePictureUrl,
                Tags = "Tag1, Tag2",
                MobileNumber = "222333444555",
                Notes = "Note2",
                Website = "https://test2.com"
            };

            context.Add(contact1);
            context.Add(contact2);
            await context.SaveChangesAsync();

            IOptions<MemoryDistributedCacheOptions>? memoryCacheOptions = Options.Create(new MemoryDistributedCacheOptions());
            IDistributedCache memoryCache = new MemoryDistributedCache(memoryCacheOptions);
            ContactService contactService = new ContactService(context, memoryCache);

            List<Contact> contactsList = await contactService.GetContactsList(2);
            List<Contact> contactsList2 = await contactService.GetContactsList(2); // Test cached result.

            Assert.NotNull(contactsList);
            Assert.IsType<List<Contact>>(contactsList);
            Assert.Empty(contactsList);
            Assert.NotNull(contactsList2);
            Assert.IsType<List<Contact>>(contactsList2);
            Assert.Empty(contactsList2);
        }
    }
}
