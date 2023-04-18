using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using IdentityModel.Client;
using KinaUna.Data.Models;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace KinaUnaWeb.Services
{
    public class LocationsHttpClient: ILocationsHttpClient
    {
        private readonly HttpClient _httpClient;
        private readonly ApiTokenInMemoryClient _apiTokenClient;

        public LocationsHttpClient(HttpClient httpClient, IConfiguration configuration, ApiTokenInMemoryClient apiTokenClient)
        {
            _httpClient = httpClient;
            _apiTokenClient = apiTokenClient;
            string clientUri = configuration.GetValue<string>("ProgenyApiServer");

            httpClient.BaseAddress = new Uri(clientUri!);
            httpClient.DefaultRequestHeaders.Accept.Clear();
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            httpClient.DefaultRequestVersion = new Version(2, 0);

        }
        

        public async Task<Location> GetLocation(int locationId)
        {
            string accessToken = await _apiTokenClient.GetProgenyAndMediaApiToken();
            _httpClient.SetBearerToken(accessToken);

            Location locationItem = new();
            string locationsApiPath = "/api/Locations/" + locationId;
            HttpResponseMessage locationResponse = await _httpClient.GetAsync(locationsApiPath);
            if (locationResponse.IsSuccessStatusCode)
            {
                string locationAsString = await locationResponse.Content.ReadAsStringAsync();
                locationItem = JsonConvert.DeserializeObject<Location>(locationAsString);
            }

            return locationItem;
        }

        public async Task<Location> AddLocation(Location location)
        {
            string accessToken = await _apiTokenClient.GetProgenyAndMediaApiToken();
            _httpClient.SetBearerToken(accessToken);

            string locationsApiPath = "/api/Locations/";
            HttpResponseMessage locationsResponse = await _httpClient.PostAsync(locationsApiPath, new StringContent(JsonConvert.SerializeObject(location), System.Text.Encoding.UTF8, "application/json"));
            if (locationsResponse.IsSuccessStatusCode)
            {
                string locationsAsString = await locationsResponse.Content.ReadAsStringAsync();
                location = JsonConvert.DeserializeObject<Location>(locationsAsString);
                return location;
            }

            return new Location();
        }

        public async Task<Location> UpdateLocation(Location location)
        {
            string accessToken = await _apiTokenClient.GetProgenyAndMediaApiToken();
            _httpClient.SetBearerToken(accessToken);
            
            string updateApiPath = "/api/Locations/" + location.LocationId;
            HttpResponseMessage locationResponse = await _httpClient.PutAsync(updateApiPath, new StringContent(JsonConvert.SerializeObject(location), System.Text.Encoding.UTF8, "application/json"));
            if (locationResponse.IsSuccessStatusCode)
            {
                string locationAsString = await locationResponse.Content.ReadAsStringAsync();
                location = JsonConvert.DeserializeObject<Location>(locationAsString);
                return location;
            }

            return new Location();
        }

        public async Task<bool> DeleteLocation(int locationId)
        {
            string accessToken = await _apiTokenClient.GetProgenyAndMediaApiToken();
            _httpClient.SetBearerToken(accessToken);

            string locationsApiPath = "/api/Locations/" + locationId;
            HttpResponseMessage locationResponse = await _httpClient.DeleteAsync(locationsApiPath);
            if (locationResponse.IsSuccessStatusCode)
            {
                return true;
            }

            return false;
        }

        public async Task<List<Location>> GetProgenyLocations(int progenyId, int accessLevel)
        {
            List<Location> progenyLocations = new();

            string accessToken = await _apiTokenClient.GetProgenyAndMediaApiToken();
            _httpClient.SetBearerToken(accessToken);

            string locationsApiPath = "/api/Locations/Progeny/" + progenyId + "?accessLevel=" + accessLevel;
            HttpResponseMessage locationsResponse = await _httpClient.GetAsync(locationsApiPath);
            if (locationsResponse.IsSuccessStatusCode)
            {
                string locationsAsString = await locationsResponse.Content.ReadAsStringAsync();

                progenyLocations = JsonConvert.DeserializeObject<List<Location>>(locationsAsString);
            }

            return progenyLocations;
        }

        public async Task<List<Location>> GetLocationsList(int progenyId, int accessLevel, string tagFilter = "")
        {
            List<Location> progenyLocationsList = new();
            string accessToken = await _apiTokenClient.GetProgenyAndMediaApiToken();
            _httpClient.SetBearerToken(accessToken);

            string locationsApiPath = "/api/Locations/Progeny/" + progenyId + "?accessLevel=" + accessLevel;
            HttpResponseMessage locationsResponse = await _httpClient.GetAsync(locationsApiPath);
            if (locationsResponse.IsSuccessStatusCode)
            {
                string locationsAsString = await locationsResponse.Content.ReadAsStringAsync();

                progenyLocationsList = JsonConvert.DeserializeObject<List<Location>>(locationsAsString);
                if (!string.IsNullOrEmpty(tagFilter))
                {
                    progenyLocationsList = progenyLocationsList.Where(l => l.Tags != null && l.Tags.Contains(tagFilter)).ToList();
                }

                progenyLocationsList = progenyLocationsList.OrderBy(l => l.Date).ToList();
            }

            return progenyLocationsList;
        }

        public async Task<Address> GetAddress(int addressId)
        {
            string accessToken = await _apiTokenClient.GetProgenyAndMediaApiToken();
            _httpClient.SetBearerToken(accessToken);

            Address addressItem = new();
            string addressApiPath = "/api/Addresses/" + addressId;
            HttpResponseMessage addressResponse = await _httpClient.GetAsync(addressApiPath);
            if (addressResponse.IsSuccessStatusCode)
            {
                string addressAsString = await addressResponse.Content.ReadAsStringAsync();

                addressItem = JsonConvert.DeserializeObject<Address>(addressAsString);
            }

            return addressItem;
        }

        public async Task<Address> AddAddress(Address address)
        {
            string accessToken = await _apiTokenClient.GetProgenyAndMediaApiToken();
            _httpClient.SetBearerToken(accessToken);

            string addressApiPath = "/api/Addresses/";
            HttpResponseMessage addressResponse = await _httpClient.PostAsync(addressApiPath, new StringContent(JsonConvert.SerializeObject(address), System.Text.Encoding.UTF8, "application/json"));
            if (addressResponse.IsSuccessStatusCode)
            {
                string addressAsString = await addressResponse.Content.ReadAsStringAsync();
                Address addressResult = JsonConvert.DeserializeObject<Address>(addressAsString);
                return addressResult;
            }

            return new Address();
        }

        public async Task<Address> UpdateAddress(Address address)
        {
            string accessToken = await _apiTokenClient.GetProgenyAndMediaApiToken();
            _httpClient.SetBearerToken(accessToken);

            string updateAddressApiPath = "/api/Addresses/" + address.AddressId;
            HttpResponseMessage addressResponse = await _httpClient.PutAsync(updateAddressApiPath, new StringContent(JsonConvert.SerializeObject(address), System.Text.Encoding.UTF8, "application/json"));
            if (addressResponse.IsSuccessStatusCode)
            {
                string addressAsString = await addressResponse.Content.ReadAsStringAsync();
                Address resultAddress = JsonConvert.DeserializeObject<Address>(addressAsString);
                return resultAddress;
            }

            return new Address();
        }
    }
}
