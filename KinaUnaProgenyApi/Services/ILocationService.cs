using System.Collections.Generic;
using System.Threading.Tasks;
using KinaUna.Data.Models;

namespace KinaUnaProgenyApi.Services
{
    public interface ILocationService
    {
        Task<Location> GetLocation(int id);
        Task<Location> AddLocation(Location location);
        Task<Location> SetLocation(int id);
        Task<Location> UpdateLocation(Location location);
        Task<Location> DeleteLocation(Location location);
        Task RemoveLocation(int id, int progenyId);
        Task<List<Location>> GetLocationsList(int progenyId);

        Task<Address> GetAddressItem(int id);
        Task<Address> AddAddressItem(Address addressItem);
        Task<Address> UpdateAddressItem(Address addressItem);
        Task<Address> SetAddressItem(int id);
        Task RemoveAddressItem(int id);
    }
}
