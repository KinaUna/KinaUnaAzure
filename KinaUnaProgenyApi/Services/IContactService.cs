using System.Collections.Generic;
using System.Threading.Tasks;
using KinaUna.Data.Models;

namespace KinaUnaProgenyApi.Services
{
    public interface IContactService
    {
        Task<Contact> GetContact(int id);
        Task<Contact> AddContact (Contact contact);
        Task<Contact> SetContactInCache(int id);
        Task<Contact> UpdateContact(Contact contact);
        Task<Contact> DeleteContact(Contact contact);
        Task RemoveContactFromCache(int id, int progenyId);
        Task<List<Contact>> GetContactsList(int progenyId);
    }
}
