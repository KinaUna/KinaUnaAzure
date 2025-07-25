using Duende.IdentityModel.Client;
using KinaUna.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace KinaUnaWeb.Services.HttpClients
{
    /// <summary>
    /// Provides methods for interacting with the Locations API and Address API.
    /// </summary>
    public class LocationsHttpClient : ILocationsHttpClient
    {
        private readonly HttpClient _httpClient;
        private readonly ITokenService _tokenService;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public LocationsHttpClient(HttpClient httpClient, IConfiguration configuration, ITokenService tokenService, IHttpContextAccessor httpContextAccessor, IHostEnvironment env)
        {
            _httpClient = httpClient;
            _httpContextAccessor = httpContextAccessor;
            _tokenService = tokenService;
            string clientUri = configuration.GetValue<string>(AuthConstants.ProgenyApiUrlKey);
            if (env.IsDevelopment())
            {
                clientUri = configuration.GetValue<string>(AuthConstants.ProgenyApiUrlKey + "Local");
            }

            if (env.IsStaging())
            {
                clientUri = configuration.GetValue<string>(AuthConstants.ProgenyApiUrlKey + "Azure");
            }

            httpClient.BaseAddress = new Uri(clientUri!);
            httpClient.DefaultRequestHeaders.Accept.Clear();
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            httpClient.DefaultRequestVersion = new Version(2, 0);

        }

        /// <summary>
        /// Gets the Location with a given LocationId.
        /// </summary>
        /// <param name="locationId">The LocationId of the Location.</param>
        /// <returns>Location object with the given LocationId.</returns>
        public async Task<Location> GetLocation(int locationId)
        {
            string signedInUserId = _httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value ?? string.Empty;
            TokenInfo tokenInfo = await _tokenService.GetValidTokenAsync(signedInUserId);
            _httpClient.SetBearerToken(tokenInfo.AccessToken);

            Location locationItem = new();
            string locationsApiPath = "/api/Locations/" + locationId;
            HttpResponseMessage locationResponse = await _httpClient.GetAsync(locationsApiPath);
            if (!locationResponse.IsSuccessStatusCode) return locationItem;

            string locationAsString = await locationResponse.Content.ReadAsStringAsync();
            locationItem = JsonConvert.DeserializeObject<Location>(locationAsString);

            return locationItem;
        }

        /// <summary>
        /// Adds a new Location.
        /// </summary>
        /// <param name="location">The Location to be added.</param>
        /// <returns>The Location object that was added.</returns>
        public async Task<Location> AddLocation(Location location)
        {
            string signedInUserId = _httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value ?? string.Empty;
            TokenInfo tokenInfo = await _tokenService.GetValidTokenAsync(signedInUserId);
            _httpClient.SetBearerToken(tokenInfo.AccessToken);

            const string locationsApiPath = "/api/Locations/";
            HttpResponseMessage locationsResponse = await _httpClient.PostAsync(locationsApiPath, new StringContent(JsonConvert.SerializeObject(location), System.Text.Encoding.UTF8, "application/json"));
            if (!locationsResponse.IsSuccessStatusCode) return new Location();
            string locationsAsString = await locationsResponse.Content.ReadAsStringAsync();
            location = JsonConvert.DeserializeObject<Location>(locationsAsString);
            return location;

        }

        /// <summary>
        /// Updates a Location. The Location with the same LocationId will be updated.
        /// </summary>
        /// <param name="location">The Location to update.</param>
        /// <returns>The updated Location object.</returns>
        public async Task<Location> UpdateLocation(Location location)
        {
            string signedInUserId = _httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value ?? string.Empty;
            TokenInfo tokenInfo = await _tokenService.GetValidTokenAsync(signedInUserId);
            _httpClient.SetBearerToken(tokenInfo.AccessToken);

            string updateApiPath = "/api/Locations/" + location.LocationId;
            HttpResponseMessage locationResponse = await _httpClient.PutAsync(updateApiPath, new StringContent(JsonConvert.SerializeObject(location), System.Text.Encoding.UTF8, "application/json"));
            if (!locationResponse.IsSuccessStatusCode) return new Location();

            string locationAsString = await locationResponse.Content.ReadAsStringAsync();
            location = JsonConvert.DeserializeObject<Location>(locationAsString);
            return location;

        }

        /// <summary>
        /// Removes the Location with a given LocationId.
        /// </summary>
        /// <param name="locationId">The LocationId of the Location to remove.</param>
        /// <returns>bool: True if the Location was successfully removed.</returns>
        public async Task<bool> DeleteLocation(int locationId)
        {
            string signedInUserId = _httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value ?? string.Empty;
            TokenInfo tokenInfo = await _tokenService.GetValidTokenAsync(signedInUserId);
            _httpClient.SetBearerToken(tokenInfo.AccessToken);

            string locationsApiPath = "/api/Locations/" + locationId;
            HttpResponseMessage locationResponse = await _httpClient.DeleteAsync(locationsApiPath);
            return locationResponse.IsSuccessStatusCode;
        }

        /// <summary>
        /// Gets the list of all locations for a Progeny that the user is allowed access to.
        /// </summary>
        /// <param name="progenyId">The progeny's Id.</param>
        /// <returns>List of Location objects.</returns>
        public async Task<List<Location>> GetProgenyLocations(int progenyId)
        {
            List<Location> progenyLocations = [];

            string signedInUserId = _httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value ?? string.Empty;
            TokenInfo tokenInfo = await _tokenService.GetValidTokenAsync(signedInUserId);
            _httpClient.SetBearerToken(tokenInfo.AccessToken);

            string locationsApiPath = "/api/Locations/Progeny/" + progenyId;
            HttpResponseMessage locationsResponse = await _httpClient.GetAsync(locationsApiPath);
            if (!locationsResponse.IsSuccessStatusCode) return progenyLocations;

            string locationsAsString = await locationsResponse.Content.ReadAsStringAsync();

            progenyLocations = JsonConvert.DeserializeObject<List<Location>>(locationsAsString);

            return progenyLocations;
        }

        /// <summary>
        /// Gets the list of Locations for a progeny that a user has access to with a given tag.
        /// </summary>
        /// <param name="progenyId">The Id of the Progeny.</param>
        /// <param name="tagFilter">The string to filter the result list by. An empty string will include all locations.</param>
        /// <returns>List of Location objects.</returns>
        public async Task<List<Location>> GetLocationsList(int progenyId, string tagFilter = "")
        {
            List<Location> progenyLocationsList = [];
            string signedInUserId = _httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value ?? string.Empty;
            TokenInfo tokenInfo = await _tokenService.GetValidTokenAsync(signedInUserId);
            _httpClient.SetBearerToken(tokenInfo.AccessToken);

            string locationsApiPath = "/api/Locations/Progeny/" + progenyId;
            HttpResponseMessage locationsResponse = await _httpClient.GetAsync(locationsApiPath);
            if (!locationsResponse.IsSuccessStatusCode) return progenyLocationsList;

            string locationsAsString = await locationsResponse.Content.ReadAsStringAsync();

            progenyLocationsList = JsonConvert.DeserializeObject<List<Location>>(locationsAsString);
            if (!string.IsNullOrEmpty(tagFilter))
            {
                progenyLocationsList = [.. progenyLocationsList.Where(l => l.Tags != null && l.Tags.Contains(tagFilter))];
            }

            progenyLocationsList = [.. progenyLocationsList.OrderBy(l => l.Date)];

            return progenyLocationsList;
        }

        /// <summary>
        /// Gets the Address entity with a given AddressId.
        /// </summary>
        /// <param name="addressId">The AddressId of the address.</param>
        /// <returns>The Address with the given AddressId. If it isn't found a new Address object with AddressId = 0.</returns>
        public async Task<Address> GetAddress(int addressId)
        {
            string signedInUserId = _httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value ?? string.Empty;
            TokenInfo tokenInfo = await _tokenService.GetValidTokenAsync(signedInUserId);
            _httpClient.SetBearerToken(tokenInfo.AccessToken);

            Address addressItem = new();
            string addressApiPath = "/api/Addresses/" + addressId;
            HttpResponseMessage addressResponse = await _httpClient.GetAsync(addressApiPath);
            if (!addressResponse.IsSuccessStatusCode) return addressItem;

            string addressAsString = await addressResponse.Content.ReadAsStringAsync();

            addressItem = JsonConvert.DeserializeObject<Address>(addressAsString);

            return addressItem;
        }

        /// <summary>
        /// Adds a new Address.
        /// </summary>
        /// <param name="address">The Address object to add.</param>
        /// <returns>The added Address object.</returns>
        public async Task<Address> AddAddress(Address address)
        {
            string signedInUserId = _httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value ?? string.Empty;
            TokenInfo tokenInfo = await _tokenService.GetValidTokenAsync(signedInUserId);
            _httpClient.SetBearerToken(tokenInfo.AccessToken);

            string addressApiPath = "/api/Addresses/";
            HttpResponseMessage addressResponse = await _httpClient.PostAsync(addressApiPath, new StringContent(JsonConvert.SerializeObject(address), System.Text.Encoding.UTF8, "application/json"));
            if (!addressResponse.IsSuccessStatusCode) return new Address();

            string addressAsString = await addressResponse.Content.ReadAsStringAsync();
            Address addressResult = JsonConvert.DeserializeObject<Address>(addressAsString);
            return addressResult;

        }

        /// <summary>
        /// Updates an Address. The Address with the same AddressId will be updated.
        /// </summary>
        /// <param name="address">The Address object with the updated properties.</param>
        /// <returns>The updated Address object.</returns>
        public async Task<Address> UpdateAddress(Address address)
        {
            string signedInUserId = _httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value ?? string.Empty;
            TokenInfo tokenInfo = await _tokenService.GetValidTokenAsync(signedInUserId);
            _httpClient.SetBearerToken(tokenInfo.AccessToken);

            string updateAddressApiPath = "/api/Addresses/" + address.AddressId;
            HttpResponseMessage addressResponse = await _httpClient.PutAsync(updateAddressApiPath, new StringContent(JsonConvert.SerializeObject(address), System.Text.Encoding.UTF8, "application/json"));
            if (!addressResponse.IsSuccessStatusCode) return new Address();

            string addressAsString = await addressResponse.Content.ReadAsStringAsync();
            Address resultAddress = JsonConvert.DeserializeObject<Address>(addressAsString);
            return resultAddress;

        }
    }
}
