using KinaUna.Data.Contexts;
using KinaUna.Data.Models;
using KinaUnaProgenyApi.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace KinaUnaProgenyApi.Tests.Services
{
    public class LocationServiceTests
    {
        [Fact]
        public async Task GetLocation_Should_Return_Location_Object_When_Id_Is_Valid()
        {
            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>().UseInMemoryDatabase("GetLocation_Should_Return_Location_Object_When_Id_Is_Valid").Options;
            await using ProgenyDbContext context = new(dbOptions);

            Location location1 = new()
            {
                Name = "Location1", ProgenyId = 1, Author = "User1", AccessLevel = 0, City = "City1", Country = "Country1", County = "County1", Date = DateTime.UtcNow, DateAdded = DateTime.UtcNow,
                District = "District1", HouseNumber = "1", Latitude = 0.0, Longitude = 0.0, LocationNumber = 1, Notes = "Note1", PostalCode = "PostalCode1", State = "State1", StreetName = "Street1", Tags = "Tag1, Tag2"
            };


            Location location2 = new()
            {
                Name = "Location2",
                ProgenyId = 1,
                Author = "User1",
                AccessLevel = 0,
                City = "City2",
                Country = "Country2",
                County = "County2",
                Date = DateTime.UtcNow,
                DateAdded = DateTime.UtcNow,
                District = "District2",
                HouseNumber = "2",
                Latitude = 0.0,
                Longitude = 0.0,
                LocationNumber = 2,
                Notes = "Note2",
                PostalCode = "PostalCode2",
                State = "State2",
                StreetName = "Street2",
                Tags = "Tag1, Tag3"
            };

            context.Add(location1);
            context.Add(location2);
            await context.SaveChangesAsync();

            IOptions<MemoryDistributedCacheOptions> memoryCacheOptions = Options.Create(new MemoryDistributedCacheOptions());
            IDistributedCache memoryCache = new MemoryDistributedCache(memoryCacheOptions);
            LocationService locationService = new(context, memoryCache);

            Location resultLocation1 = await locationService.GetLocation(1);
            Location resultLocation2 = await locationService.GetLocation(1); // Uses cache

            Assert.NotNull(resultLocation1);
            Assert.IsType<Location>(resultLocation1);
            Assert.Equal(location1.Author, resultLocation1.Author);
            Assert.Equal(location1.Name, resultLocation1.Name);
            Assert.Equal(location1.AccessLevel, resultLocation1.AccessLevel);
            Assert.Equal(location1.ProgenyId, resultLocation1.ProgenyId);

            Assert.NotNull(resultLocation2);
            Assert.IsType<Location>(resultLocation2);
            Assert.Equal(location1.Author, resultLocation2.Author);
            Assert.Equal(location1.Name, resultLocation2.Name);
            Assert.Equal(location1.AccessLevel, resultLocation2.AccessLevel);
            Assert.Equal(location1.ProgenyId, resultLocation2.ProgenyId);
        }

        [Fact]
        public async Task GetLocation_Should_Return_Null_When_Id_Is_Invalid()
        {
            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>().UseInMemoryDatabase("GetLocation_Should_Return_Null_When_Id_Is_Invalid").Options;
            await using ProgenyDbContext context = new(dbOptions);

            Location location1 = new()
            {
                Name = "Location1",
                ProgenyId = 1,
                Author = "User1",
                AccessLevel = 0,
                City = "City1",
                Country = "Country1",
                County = "County1",
                Date = DateTime.UtcNow,
                DateAdded = DateTime.UtcNow,
                District = "District1",
                HouseNumber = "1",
                Latitude = 0.0,
                Longitude = 0.0,
                LocationNumber = 1,
                Notes = "Note1",
                PostalCode = "PostalCode1",
                State = "State1",
                StreetName = "Street1",
                Tags = "Tag1, Tag2"
            };
            
            context.Add(location1);
            await context.SaveChangesAsync();

            IOptions<MemoryDistributedCacheOptions> memoryCacheOptions = Options.Create(new MemoryDistributedCacheOptions());
            IDistributedCache memoryCache = new MemoryDistributedCache(memoryCacheOptions);
            LocationService locationService = new(context, memoryCache);

            Location resultLocation1 = await locationService.GetLocation(2);
            Location resultLocation2 = await locationService.GetLocation(2); // Using cache
            
            Assert.Null(resultLocation1);
            Assert.Null(resultLocation2);
        }

        [Fact]
        public async Task AddLocation_Should_Save_Location()
        {
            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>().UseInMemoryDatabase("AddLocation_Should_Save_Location").Options;
            await using ProgenyDbContext context = new(dbOptions);

            Location location1 = new()
            {
                Name = "Location1",
                ProgenyId = 1,
                Author = "User1",
                AccessLevel = 0,
                City = "City1",
                Country = "Country1",
                County = "County1",
                Date = DateTime.UtcNow,
                DateAdded = DateTime.UtcNow,
                District = "District1",
                HouseNumber = "1",
                Latitude = 0.0,
                Longitude = 0.0,
                LocationNumber = 1,
                Notes = "Note1",
                PostalCode = "PostalCode1",
                State = "State1",
                StreetName = "Street1",
                Tags = "Tag1, Tag2"
            };
            
            context.Add(location1);
            await context.SaveChangesAsync();

            IOptions<MemoryDistributedCacheOptions> memoryCacheOptions = Options.Create(new MemoryDistributedCacheOptions());
            IDistributedCache memoryCache = new MemoryDistributedCache(memoryCacheOptions);
            LocationService locationService = new(context, memoryCache);

            Location locationToAdd = new()
            {
                Name = "Location1",
                ProgenyId = 1,
                Author = "User1",
                AccessLevel = 0,
                City = "City1",
                Country = "Country1",
                County = "County1",
                Date = DateTime.UtcNow,
                DateAdded = DateTime.UtcNow,
                District = "District1",
                HouseNumber = "1",
                Latitude = 0.0,
                Longitude = 0.0,
                LocationNumber = 1,
                Notes = "Note1",
                PostalCode = "PostalCode1",
                State = "State1",
                StreetName = "Street1",
                Tags = "Tag1, Tag2"
            };
            
            Location addedLocation = await locationService.AddLocation(locationToAdd);
            Location? dbLocation = await context.LocationsDb.AsNoTracking().SingleOrDefaultAsync(f => f.LocationId == addedLocation.LocationId);
            Location savedLocation = await locationService.GetLocation(addedLocation.LocationId);

            Assert.NotNull(addedLocation);
            Assert.IsType<Location>(addedLocation);
            Assert.Equal(locationToAdd.Author, addedLocation.Author);
            Assert.Equal(locationToAdd.Name, addedLocation.Name);
            Assert.Equal(locationToAdd.AccessLevel, addedLocation.AccessLevel);
            Assert.Equal(locationToAdd.ProgenyId, addedLocation.ProgenyId);

            if (dbLocation != null)
            {
                Assert.IsType<Location>(dbLocation);
                Assert.Equal(locationToAdd.Author, dbLocation.Author);
                Assert.Equal(locationToAdd.Name, dbLocation.Name);
                Assert.Equal(locationToAdd.AccessLevel, dbLocation.AccessLevel);
                Assert.Equal(locationToAdd.ProgenyId, dbLocation.ProgenyId);
            }
            Assert.NotNull(savedLocation);
            Assert.IsType<Location>(savedLocation);
            Assert.Equal(locationToAdd.Author, savedLocation.Author);
            Assert.Equal(locationToAdd.Name, savedLocation.Name);
            Assert.Equal(locationToAdd.AccessLevel, savedLocation.AccessLevel);
            Assert.Equal(locationToAdd.ProgenyId, savedLocation.ProgenyId);

        }

        [Fact]
        public async Task UpdateLocation_Should_Save_Location()
        {
            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>().UseInMemoryDatabase("UpdateLocation_Should_Save_Location").Options;
            await using ProgenyDbContext context = new(dbOptions);

            Location location1 = new()
            {
                Name = "Location1",
                ProgenyId = 1,
                Author = "User1",
                AccessLevel = 0,
                City = "City1",
                Country = "Country1",
                County = "County1",
                Date = DateTime.UtcNow,
                DateAdded = DateTime.UtcNow,
                District = "District1",
                HouseNumber = "1",
                Latitude = 0.0,
                Longitude = 0.0,
                LocationNumber = 1,
                Notes = "Note1",
                PostalCode = "PostalCode1",
                State = "State1",
                StreetName = "Street1",
                Tags = "Tag1, Tag2"
            };


            Location location2 = new()
            {
                Name = "Location2",
                ProgenyId = 1,
                Author = "User1",
                AccessLevel = 0,
                City = "City2",
                Country = "Country2",
                County = "County2",
                Date = DateTime.UtcNow,
                DateAdded = DateTime.UtcNow,
                District = "District2",
                HouseNumber = "2",
                Latitude = 0.0,
                Longitude = 0.0,
                LocationNumber = 2,
                Notes = "Note2",
                PostalCode = "PostalCode2",
                State = "State2",
                StreetName = "Street2",
                Tags = "Tag1, Tag3"
            };

            context.Add(location1);
            context.Add(location2);
            await context.SaveChangesAsync();

            IOptions<MemoryDistributedCacheOptions> memoryCacheOptions = Options.Create(new MemoryDistributedCacheOptions());
            IDistributedCache memoryCache = new MemoryDistributedCache(memoryCacheOptions);
            LocationService locationService = new(context, memoryCache);

            Location locationToUpdate = await locationService.GetLocation(1);
            locationToUpdate.AccessLevel = 5;
            Location updatedLocation = await locationService.UpdateLocation(locationToUpdate);
            Location? dbLocation = await context.LocationsDb.AsNoTracking().SingleOrDefaultAsync(f => f.LocationId == 1);
            Location savedLocation = await locationService.GetLocation(1);

            Assert.NotNull(updatedLocation);
            Assert.IsType<Location>(updatedLocation);
            Assert.NotEqual(0, updatedLocation.LocationId);
            Assert.Equal("User1", updatedLocation.Author);
            Assert.Equal(5, updatedLocation.AccessLevel);
            Assert.Equal(1, updatedLocation.ProgenyId);

            if (dbLocation != null)
            {
                Assert.IsType<Location>(dbLocation);
                Assert.NotEqual(0, dbLocation.LocationId);
                Assert.Equal("User1", dbLocation.Author);
                Assert.Equal(5, dbLocation.AccessLevel);
                Assert.Equal(1, dbLocation.ProgenyId);
            }

            Assert.NotNull(savedLocation);
            Assert.IsType<Location>(savedLocation);
            Assert.NotEqual(0, savedLocation.LocationId);
            Assert.Equal("User1", savedLocation.Author);
            Assert.Equal(5, savedLocation.AccessLevel);
            Assert.Equal(1, savedLocation.ProgenyId);
        }

        [Fact]
        public async Task DeleteLocation_Should_Remove_Location()
        {
            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>().UseInMemoryDatabase("DeleteLocation_Should_Remove_Location").Options;
            await using ProgenyDbContext context = new(dbOptions);

            Location location1 = new()
            {
                Name = "Location1",
                ProgenyId = 1,
                Author = "User1",
                AccessLevel = 0,
                City = "City1",
                Country = "Country1",
                County = "County1",
                Date = DateTime.UtcNow,
                DateAdded = DateTime.UtcNow,
                District = "District1",
                HouseNumber = "1",
                Latitude = 0.0,
                Longitude = 0.0,
                LocationNumber = 1,
                Notes = "Note1",
                PostalCode = "PostalCode1",
                State = "State1",
                StreetName = "Street1",
                Tags = "Tag1, Tag2"
            };


            Location location2 = new()
            {
                Name = "Location2",
                ProgenyId = 1,
                Author = "User1",
                AccessLevel = 0,
                City = "City2",
                Country = "Country2",
                County = "County2",
                Date = DateTime.UtcNow,
                DateAdded = DateTime.UtcNow,
                District = "District2",
                HouseNumber = "2",
                Latitude = 0.0,
                Longitude = 0.0,
                LocationNumber = 2,
                Notes = "Note2",
                PostalCode = "PostalCode2",
                State = "State2",
                StreetName = "Street2",
                Tags = "Tag1, Tag3"
            };
            context.Add(location1);
            context.Add(location2);
            await context.SaveChangesAsync();

            IOptions<MemoryDistributedCacheOptions> memoryCacheOptions = Options.Create(new MemoryDistributedCacheOptions());
            IDistributedCache memoryCache = new MemoryDistributedCache(memoryCacheOptions);
            LocationService locationService = new(context, memoryCache);

            int locationItemsCountBeforeDelete = context.LocationsDb.Count();
            Location locationToDelete = await locationService.GetLocation(1);

            await locationService.DeleteLocation(locationToDelete);
            Location? deletedLocation = await context.LocationsDb.AsNoTracking().SingleOrDefaultAsync(f => f.LocationId == 1);
            int locationItemsCountAfterDelete = context.LocationsDb.Count();

            Assert.Null(deletedLocation);
            Assert.Equal(2, locationItemsCountBeforeDelete);
            Assert.Equal(1, locationItemsCountAfterDelete);
        }

        [Fact]
        public async Task GetLocationsList_Should_Return_List_Of_Location_When_Progeny_Has_Saved_Locations()
        {
            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>().UseInMemoryDatabase("GetLocationsList_Should_Return_List_Of_Location_When_Progeny_Has_Saved_Locations").Options;
            await using ProgenyDbContext context = new(dbOptions);

            Location location1 = new()
            {
                Name = "Location1",
                ProgenyId = 1,
                Author = "User1",
                AccessLevel = 0,
                City = "City1",
                Country = "Country1",
                County = "County1",
                Date = DateTime.UtcNow,
                DateAdded = DateTime.UtcNow,
                District = "District1",
                HouseNumber = "1",
                Latitude = 0.0,
                Longitude = 0.0,
                LocationNumber = 1,
                Notes = "Note1",
                PostalCode = "PostalCode1",
                State = "State1",
                StreetName = "Street1",
                Tags = "Tag1, Tag2"
            };


            Location location2 = new()
            {
                Name = "Location2",
                ProgenyId = 1,
                Author = "User1",
                AccessLevel = 0,
                City = "City2",
                Country = "Country2",
                County = "County2",
                Date = DateTime.UtcNow,
                DateAdded = DateTime.UtcNow,
                District = "District2",
                HouseNumber = "2",
                Latitude = 0.0,
                Longitude = 0.0,
                LocationNumber = 2,
                Notes = "Note2",
                PostalCode = "PostalCode2",
                State = "State2",
                StreetName = "Street2",
                Tags = "Tag1, Tag3"
            };

            context.Add(location1);
            context.Add(location2);
            await context.SaveChangesAsync();

            IOptions<MemoryDistributedCacheOptions> memoryCacheOptions = Options.Create(new MemoryDistributedCacheOptions());
            IDistributedCache memoryCache = new MemoryDistributedCache(memoryCacheOptions);
            LocationService locationService = new(context, memoryCache);

            List<Location> locationsList = await locationService.GetLocationsList(1);
            List<Location> locationsList2 = await locationService.GetLocationsList(1); // Test cached result.
            Location firstLocation = locationsList.First();

            Assert.NotNull(locationsList);
            Assert.IsType<List<Location>>(locationsList);
            Assert.Equal(2, locationsList.Count);
            Assert.NotNull(locationsList2);
            Assert.IsType<List<Location>>(locationsList2);
            Assert.Equal(2, locationsList2.Count);
            Assert.NotNull(firstLocation);
            Assert.IsType<Location>(firstLocation);
        }

        [Fact]
        public async Task GetLocationsList_Should_Return_Empty_List_Of_Location_When_Progeny_Has_No_Saved_Locations()
        {
            
            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>().UseInMemoryDatabase("GetLocationsList_Should_Return_Empty_List_Of_Location_When_Progeny_Has_No_Saved_Locations").Options;
            await using ProgenyDbContext context = new(dbOptions);

            Location location1 = new()
            {
                Name = "Location1",
                ProgenyId = 1,
                Author = "User1",
                AccessLevel = 0,
                City = "City1",
                Country = "Country1",
                County = "County1",
                Date = DateTime.UtcNow,
                DateAdded = DateTime.UtcNow,
                District = "District1",
                HouseNumber = "1",
                Latitude = 0.0,
                Longitude = 0.0,
                LocationNumber = 1,
                Notes = "Note1",
                PostalCode = "PostalCode1",
                State = "State1",
                StreetName = "Street1",
                Tags = "Tag1, Tag2"
            };


            Location location2 = new()
            {
                Name = "Location2",
                ProgenyId = 1,
                Author = "User1",
                AccessLevel = 0,
                City = "City2",
                Country = "Country2",
                County = "County2",
                Date = DateTime.UtcNow,
                DateAdded = DateTime.UtcNow,
                District = "District2",
                HouseNumber = "2",
                Latitude = 0.0,
                Longitude = 0.0,
                LocationNumber = 2,
                Notes = "Note2",
                PostalCode = "PostalCode2",
                State = "State2",
                StreetName = "Street2",
                Tags = "Tag1, Tag3"
            };

            context.Add(location1);
            context.Add(location2);
            await context.SaveChangesAsync();

            IOptions<MemoryDistributedCacheOptions> memoryCacheOptions = Options.Create(new MemoryDistributedCacheOptions());
            IDistributedCache memoryCache = new MemoryDistributedCache(memoryCacheOptions);
            LocationService locationService = new(context, memoryCache);

            List<Location> locationsList = await locationService.GetLocationsList(2);
            List<Location> locationsList2 = await locationService.GetLocationsList(2); // Test cached result.

            Assert.NotNull(locationsList);
            Assert.IsType<List<Location>>(locationsList);
            Assert.Empty(locationsList);
            Assert.NotNull(locationsList2);
            Assert.IsType<List<Location>>(locationsList2);
            Assert.Empty(locationsList2);
        }

        [Fact]
        public async Task GetAddress_Returns_Address_Object_When_Id_Is_Valid()
        {
            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>().UseInMemoryDatabase("GetAddress_Returns_Address_Object_When_Id_Is_Valid").Options;
            await using ProgenyDbContext context = new(dbOptions);

            Address address1 = new()
            {
                AddressLine1 = "Address1 Line1",
                AddressLine2 = "Address1 Line2",
                PostalCode = "PostalCode1",
                State = "State1",
                City = "City1",
                Country = "Country1"
            };

            Address address2 = new()
            {
                AddressLine1 = "Address2 Line1",
                AddressLine2 = "Address2 Line2",
                PostalCode = "PostalCode2",
                State = "State2",
                City = "City2",
                Country = "Country2"
            };

            context.Add(address1);
            context.Add(address2);
            await context.SaveChangesAsync();

            IOptions<MemoryDistributedCacheOptions> memoryCacheOptions = Options.Create(new MemoryDistributedCacheOptions());
            IDistributedCache memoryCache = new MemoryDistributedCache(memoryCacheOptions);
            LocationService locationService = new(context, memoryCache);

            Address resultAddress1 = await locationService.GetAddressItem(1);
            Address resultAddress2 = await locationService.GetAddressItem(1); // Uses cache

            Assert.NotNull(resultAddress1);
            Assert.IsType<Address>(resultAddress1);
            Assert.Equal(address1.AddressLine1, resultAddress1.AddressLine1);
            Assert.Equal(address1.AddressLine2, resultAddress1.AddressLine2);
            Assert.Equal(address1.PostalCode, resultAddress1.PostalCode);
            Assert.Equal(address1.City, resultAddress1.City);

            Assert.NotNull(resultAddress2);
            Assert.IsType<Address>(resultAddress2);
            Assert.Equal(address1.AddressLine1, resultAddress2.AddressLine1);
            Assert.Equal(address1.AddressLine2, resultAddress2.AddressLine2);
            Assert.Equal(address1.PostalCode, resultAddress2.PostalCode);
            Assert.Equal(address1.City, resultAddress2.City);
        }

        [Fact]
        public async Task GetAddress_Should_Return_Null_When_Id_Is_Invalid()
        {
            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>().UseInMemoryDatabase("GetAddress_Should_Return_Null_When_Id_Is_Invalid").Options;
            await using ProgenyDbContext context = new(dbOptions);

            Address address1 = new()
            {
                AddressLine1 = "Address1 Line1",
                AddressLine2 = "Address1 Line2",
                PostalCode = "PostalCode1",
                State = "State1",
                City = "City1",
                Country = "Country1"
            };
            
            context.Add(address1);
            await context.SaveChangesAsync();

            IOptions<MemoryDistributedCacheOptions> memoryCacheOptions = Options.Create(new MemoryDistributedCacheOptions());
            IDistributedCache memoryCache = new MemoryDistributedCache(memoryCacheOptions);
            LocationService locationService = new(context, memoryCache);

            Address resultAddress1 = await locationService.GetAddressItem(2);
            Address resultAddress2 = await locationService.GetAddressItem(2); // Using cache

            Assert.Null(resultAddress1);
            Assert.Null(resultAddress2);
        }

        [Fact]
        public async Task AddAddress_Should_Save_Address()
        {
            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>().UseInMemoryDatabase("AddAddress_Should_Save_Address").Options;
            await using ProgenyDbContext context = new(dbOptions);

            Address address1 = new()
            {
                AddressLine1 = "Address1 Line1",
                AddressLine2 = "Address1 Line2",
                PostalCode = "PostalCode1",
                State = "State1",
                City = "City1",
                Country = "Country1"
            };
            
            context.Add(address1);
            await context.SaveChangesAsync();

            IOptions<MemoryDistributedCacheOptions> memoryCacheOptions = Options.Create(new MemoryDistributedCacheOptions());
            IDistributedCache memoryCache = new MemoryDistributedCache(memoryCacheOptions);
            LocationService locationService = new(context, memoryCache);

            Address addressToAdd = new()
            {
                AddressLine1 = "Address2 Line1",
                AddressLine2 = "Address2 Line2",
                PostalCode = "PostalCode2",
                State = "State2",
                City = "City2",
                Country = "Country2"
            };

            Address addedAddress = await locationService.AddAddressItem(addressToAdd);
            Address? dbAddress = await context.AddressDb.AsNoTracking().SingleOrDefaultAsync(f => f.AddressId == addedAddress.AddressId);
            Address savedAddress = await locationService.GetAddressItem(addedAddress.AddressId);

            Assert.NotNull(addedAddress);
            Assert.IsType<Address>(addedAddress);
            Assert.Equal(addressToAdd.AddressLine1, addedAddress.AddressLine1);
            Assert.Equal(addressToAdd.AddressLine2, addedAddress.AddressLine2);
            Assert.Equal(addressToAdd.PostalCode, addedAddress.PostalCode);
            Assert.Equal(addressToAdd.City, addedAddress.City);

            if (dbAddress != null)
            {
                Assert.IsType<Address>(dbAddress);
                Assert.Equal(addressToAdd.AddressLine1, dbAddress.AddressLine1);
                Assert.Equal(addressToAdd.AddressLine2, dbAddress.AddressLine2);
                Assert.Equal(addressToAdd.PostalCode, dbAddress.PostalCode);
                Assert.Equal(addressToAdd.City, dbAddress.City);
            }
            Assert.NotNull(savedAddress);
            Assert.IsType<Address>(savedAddress);
            Assert.Equal(addressToAdd.AddressLine1, savedAddress.AddressLine1);
            Assert.Equal(addressToAdd.AddressLine2, savedAddress.AddressLine2);
            Assert.Equal(addressToAdd.PostalCode, savedAddress.PostalCode);
            Assert.Equal(addressToAdd.City, savedAddress.City);

        }

        [Fact]
        public async Task UpdateAddress_Should_Save_Address()
        {
            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>().UseInMemoryDatabase("UpdateAddress_Should_Save_Address").Options;
            await using ProgenyDbContext context = new(dbOptions);

            Address address1 = new()
            {
                AddressLine1 = "Address1 Line1",
                AddressLine2 = "Address1 Line2",
                PostalCode = "PostalCode1",
                State = "State1",
                City = "City1",
                Country = "Country1"
            };

            Address address2 = new()
            {
                AddressLine1 = "Address2 Line1",
                AddressLine2 = "Address2 Line2",
                PostalCode = "PostalCode2",
                State = "State2",
                City = "City2",
                Country = "Country2"
            };
            context.Add(address1);
            context.Add(address2);
            await context.SaveChangesAsync();

            IOptions<MemoryDistributedCacheOptions> memoryCacheOptions = Options.Create(new MemoryDistributedCacheOptions());
            IDistributedCache memoryCache = new MemoryDistributedCache(memoryCacheOptions);
            LocationService locationService = new(context, memoryCache);

            Address addressToUpdate = await locationService.GetAddressItem(1);
            addressToUpdate.PostalCode = "PostalCode3";
            Address updatedAddress = await locationService.UpdateAddressItem(addressToUpdate);
            Address? dbAddress = await context.AddressDb.AsNoTracking().SingleOrDefaultAsync(f => f.AddressId == 1);
            Address savedAddress = await locationService.GetAddressItem(1);

            Assert.NotNull(updatedAddress);
            Assert.IsType<Address>(updatedAddress);
            Assert.NotEqual(0, updatedAddress.AddressId);
            Assert.Equal("State1", updatedAddress.State);
            Assert.Equal("PostalCode3", updatedAddress.PostalCode);
            Assert.Equal("City1", updatedAddress.City);

            if (dbAddress != null)
            {
                Assert.IsType<Address>(dbAddress);
                Assert.NotEqual(0, dbAddress.AddressId);
                Assert.Equal("State1", dbAddress.State);
                Assert.Equal("PostalCode3", dbAddress.PostalCode);
                Assert.Equal("City1", dbAddress.City);
            }

            Assert.NotNull(savedAddress);
            Assert.IsType<Address>(savedAddress);
            Assert.NotEqual(0, savedAddress.AddressId);
            Assert.Equal("State1", savedAddress.State);
            Assert.Equal("PostalCode3", savedAddress.PostalCode);
            Assert.Equal("City1", savedAddress.City);
        }

        [Fact]
        public async Task DeleteAddress_Should_Remove_Address()
        {
            DbContextOptions<ProgenyDbContext> dbOptions = new DbContextOptionsBuilder<ProgenyDbContext>().UseInMemoryDatabase("DeleteAddress_Should_Remove_Address").Options;
            await using ProgenyDbContext context = new(dbOptions);

            Address address1 = new()
            {
                AddressLine1 = "Address1 Line1",
                AddressLine2 = "Address1 Line2",
                PostalCode = "PostalCode1",
                State = "State1",
                City = "City1",
                Country = "Country1"
            };

            Address address2 = new()
            {
                AddressLine1 = "Address2 Line1", AddressLine2 = "Address2 Line2", PostalCode = "PostalCode2", State = "State2", City = "City2", Country = "Country2"
            };
            context.Add(address1);
            context.Add(address2);
            await context.SaveChangesAsync();

            IOptions<MemoryDistributedCacheOptions> memoryCacheOptions = Options.Create(new MemoryDistributedCacheOptions());
            IDistributedCache memoryCache = new MemoryDistributedCache(memoryCacheOptions);
            LocationService locationService = new(context, memoryCache);

            int addressItemsCountBeforeDelete = context.AddressDb.Count();
            Address addressToDelete = await locationService.GetAddressItem(1);

            await locationService.RemoveAddressItem(addressToDelete.AddressId);
            Address? deletedAddress = await context.AddressDb.SingleOrDefaultAsync(f => f.AddressId == 1);
            int addressItemsCountAfterDelete = context.AddressDb.Count();

            Assert.Null(deletedAddress);
            Assert.Equal(2, addressItemsCountBeforeDelete);
            Assert.Equal(1, addressItemsCountAfterDelete);
        }
    }
}
