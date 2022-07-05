using KinaUnaProgenyApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using KinaUna.Data;
using KinaUna.Data.Contexts;
using KinaUna.Data.Extensions;
using KinaUna.Data.Models;

namespace KinaUnaProgenyApi.Controllers
{
    [Authorize(AuthenticationSchemes = "Bearer")]
    [Produces("application/json")]
    [Route("api/[controller]")]
    [ApiController]
    public class ContactsController : ControllerBase
    {
        private readonly ProgenyDbContext _context;
        private readonly ImageStore _imageStore;
        private readonly IDataService _dataService;
        private readonly AzureNotifications _azureNotifications;

        public ContactsController(ProgenyDbContext context, ImageStore imageStore, IDataService dataService, AzureNotifications azureNotifications)
        {
            _context = context;
            _imageStore = imageStore;
            _dataService = dataService;
            _azureNotifications = azureNotifications;
        }
        
        // GET api/contacts/progeny/[id]
        [HttpGet]
        [Route("[action]/{id}")]
        public async Task<IActionResult> Progeny(int id, [FromQuery] int accessLevel = 5)
        {
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserAccess userAccess = await _dataService.GetProgenyUserAccessForUser(id, userEmail);
            if (userAccess != null || id == Constants.DefaultChildId)
            {
                List<Contact> contactsList = await _dataService.GetContactsList(id);
                contactsList = contactsList.Where(c => c.AccessLevel >= accessLevel).ToList();
                if (contactsList.Any())
                {
                    return Ok(contactsList);
                }
            }

            return NotFound();
        }

        // GET api/contacts/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetContactItem(int id)
        {
            Contact result = await _dataService.GetContact(id);

            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserAccess userAccess = await _dataService.GetProgenyUserAccessForUser(result.ProgenyId, userEmail);
            if (userAccess != null || id == Constants.DefaultChildId)
            {
                return Ok(result);
            }

            return NotFound();
        }

        // POST api/contact
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] Contact value)
        {
            // Check if child exists.
            Progeny prog = await _context.ProgenyDb.AsNoTracking().SingleOrDefaultAsync(p => p.Id == value.ProgenyId);
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            if (prog != null)
            {
                // Check if user is allowed to add contacts for this child.

                if (!prog.IsInAdminList(userEmail))
                {
                    return Unauthorized();
                }
            }
            else
            {
                return NotFound();
            }
            
            Contact contactItem = new Contact();
            contactItem.AccessLevel = value.AccessLevel;
            contactItem.Active = value.Active;
            contactItem.AddressString = value.AddressString;
            contactItem.ProgenyId = value.ProgenyId;
            contactItem.Author = value.Author;
            contactItem.DateAdded = value.DateAdded ?? DateTime.UtcNow;
            contactItem.Context = value.Context;
            contactItem.DisplayName = value.DisplayName;
            contactItem.Email1 = value.Email1;
            contactItem.Email2 = value.Email2;
            contactItem.FirstName = value.FirstName;
            contactItem.LastName = value.LastName;
            contactItem.MiddleName = value.MiddleName;
            contactItem.MobileNumber = value.MobileNumber;
            contactItem.Notes = value.Notes;
            contactItem.PhoneNumber = value.PhoneNumber;
            if (value.PictureLink != "[KeepExistingLink]")
            {
                contactItem.PictureLink = value.PictureLink;
            }
            contactItem.Tags = value.Tags;
            contactItem.Website = value.Website;
            contactItem.Address = value.Address;

            if (contactItem.Address != null)
            {
                if (contactItem.Address.AddressLine1 + contactItem.Address.AddressLine2 + contactItem.Address.City + contactItem.Address.Country + contactItem.Address.PostalCode + contactItem.Address.State != "")
                {
                    Address address = new Address();
                    address.AddressLine1 = contactItem.Address.AddressLine1;
                    address.AddressLine2 = contactItem.Address.AddressLine2;
                    address.City = contactItem.Address.City;
                    address.PostalCode = contactItem.Address.PostalCode;
                    address.State = contactItem.Address.State;
                    address.Country = contactItem.Address.Country;
                    await _context.AddressDb.AddAsync(address);
                    await _context.SaveChangesAsync();
                    contactItem.AddressIdNumber = address.AddressId;
                }
            }
            _context.ContactsDb.Add(contactItem);
            await _context.SaveChangesAsync();
            await _dataService.SetContact(contactItem.ContactId);

            TimeLineItem tItem = new TimeLineItem();
            tItem.ProgenyId = contactItem.ProgenyId;
            tItem.AccessLevel = contactItem.AccessLevel;
            tItem.ItemType = (int)KinaUnaTypes.TimeLineType.Contact;
            tItem.ItemId = contactItem.ContactId.ToString();
            UserInfo userinfo = _context.UserInfoDb.SingleOrDefault(u => u.UserEmail.ToUpper() == userEmail.ToUpper());
            tItem.CreatedBy = userinfo?.UserId ?? "User not found";
            tItem.CreatedTime = DateTime.UtcNow;
            tItem.ProgenyTime = contactItem.DateAdded.Value;

            await _context.TimeLineDb.AddAsync(tItem);
            await _context.SaveChangesAsync();
            await _dataService.SetTimeLineItem(tItem.TimeLineId);

            string title = "Contact added for " + prog.NickName;
            if (userinfo != null)
            {
                string message = userinfo.FirstName + " " + userinfo.MiddleName + " " + userinfo.LastName + " added a new contact for " + prog.NickName;
                await _azureNotifications.ProgenyUpdateNotification(title, message, tItem, userinfo.ProfilePicture);
            }

            return Ok(contactItem);
        }

        // PUT api/contacts/5
        [HttpPut("{id}")]
        public async Task<IActionResult> Put(int id, [FromBody] Contact value)
        {
            Contact contactItem = await _context.ContactsDb.SingleOrDefaultAsync(c => c.ContactId == id);
            if (contactItem == null)
            {
                return NotFound();
            }

            // Check if child exists.
            Progeny prog = await _context.ProgenyDb.AsNoTracking().SingleOrDefaultAsync(p => p.Id == value.ProgenyId);
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            if (prog != null)
            {
                // Check if user is allowed to edit contacts for this child.

                if (!prog.IsInAdminList(userEmail))
                {
                    return Unauthorized();
                }
            }
            else
            {
                return NotFound();
            }

            contactItem.AccessLevel = value.AccessLevel;
            contactItem.Active = value.Active;
            contactItem.AddressIdNumber = value.AddressIdNumber;
            contactItem.AddressString = value.AddressString;
            contactItem.ProgenyId = value.ProgenyId;
            contactItem.Author = value.Author;
            if (value.DateAdded.HasValue)
            {
                contactItem.DateAdded = value.DateAdded.Value.ToUniversalTime();
            }
            else
            {
                contactItem.DateAdded = DateTime.UtcNow;
            }
            
            contactItem.Context = value.Context;
            contactItem.DisplayName = value.DisplayName;
            contactItem.Email1 = value.Email1;
            contactItem.Email2 = value.Email2;
            contactItem.FirstName = value.FirstName;
            contactItem.LastName = value.LastName;
            contactItem.MiddleName = value.MiddleName;
            contactItem.MobileNumber = value.MobileNumber;
            contactItem.Notes = value.Notes;
            contactItem.PhoneNumber = value.PhoneNumber;
            if (value.PictureLink != "[KeepExistingLink]")
            {
                contactItem.PictureLink = value.PictureLink;
            }
            contactItem.Tags = value.Tags;
            contactItem.Website = value.Website;
            contactItem.Address = value.Address;
            if (contactItem.AddressIdNumber != null && contactItem.AddressIdNumber.Value != 0)
            {
                Address addressOld = await _context.AddressDb.SingleAsync(c => c.AddressId == contactItem.AddressIdNumber);
                if (contactItem.Address != null)
                {
                    addressOld.AddressLine1 = contactItem.Address.AddressLine1;
                    addressOld.AddressLine2 = contactItem.Address.AddressLine2;
                    addressOld.City = contactItem.Address.City;
                    addressOld.PostalCode = contactItem.Address.PostalCode;
                    addressOld.State = contactItem.Address.State;
                    addressOld.Country = contactItem.Address.Country;
                    contactItem.Address = addressOld;

                    _context.AddressDb.Update(addressOld);
                    await _dataService.SetAddressItem(addressOld.AddressId);
                }
                else
                {
                    int removedAddressId = addressOld.AddressId;
                    contactItem.AddressIdNumber = null;
                    await _dataService.RemoveAddressItem(removedAddressId);
                }
                
                await _context.SaveChangesAsync();
            }
            else
            {
                if (contactItem.Address.AddressLine1 + contactItem.Address.AddressLine2 + contactItem.Address.City + contactItem.Address.Country + contactItem.Address.PostalCode + contactItem.Address.State !=
                    "")
                {
                    Address address = new Address();
                    address.AddressLine1 = contactItem.Address.AddressLine1;
                    address.AddressLine2 = contactItem.Address.AddressLine2;
                    address.City = contactItem.Address.City;
                    address.PostalCode = contactItem.Address.PostalCode;
                    address.State = contactItem.Address.State;
                    address.Country = contactItem.Address.Country;
                    await _context.AddressDb.AddAsync(address);
                    await _context.SaveChangesAsync();
                    contactItem.AddressIdNumber = address.AddressId;
                }
            }


            _context.ContactsDb.Update(contactItem);
            await _context.SaveChangesAsync();
            await _dataService.SetContact(contactItem.ContactId);

            TimeLineItem tItem = await _context.TimeLineDb.SingleOrDefaultAsync(t =>
                t.ItemId == contactItem.ContactId.ToString() && t.ItemType == (int)KinaUnaTypes.TimeLineType.Contact);
            if (tItem != null)
            {
                tItem.ProgenyTime = contactItem.DateAdded.Value;
                tItem.AccessLevel = contactItem.AccessLevel;
                _context.TimeLineDb.Update(tItem);
                await _context.SaveChangesAsync();
                await _dataService.SetTimeLineItem(tItem.TimeLineId);
            }

            UserInfo userinfo = await _dataService.GetUserInfoByEmail(userEmail);
            string title = "Contact edited for " + prog.NickName;
            string message = userinfo.FirstName + " " + userinfo.MiddleName + " " + userinfo.LastName + " edited a contact for " + prog.NickName;
            await _azureNotifications.ProgenyUpdateNotification(title, message, tItem, userinfo.ProfilePicture);

            return Ok(contactItem);
        }

        // DELETE api/contacts/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            Contact contactItem = await _context.ContactsDb.SingleOrDefaultAsync(c => c.ContactId == id);
            if (contactItem != null)
            {
                string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
                // Check if child exists.
                Progeny prog = await _context.ProgenyDb.SingleOrDefaultAsync(p => p.Id == contactItem.ProgenyId);
                if (prog != null)
                {
                    // Check if user is allowed to delete contacts for this child.
                    if (!prog.IsInAdminList(userEmail))
                    {
                        return Unauthorized();
                    }
                }
                else
                {
                    return NotFound();
                }

                TimeLineItem tItem = await _context.TimeLineDb.SingleOrDefaultAsync(t =>
                    t.ItemId == contactItem.ContactId.ToString() && t.ItemType == (int)KinaUnaTypes.TimeLineType.Contact);
                if (tItem != null)
                {
                    _context.TimeLineDb.Remove(tItem);
                    await _context.SaveChangesAsync();
                    await _dataService.RemoveTimeLineItem(tItem.TimeLineId, tItem.ItemType, tItem.ProgenyId);
                }

                if (contactItem.AddressIdNumber != null)
                {
                    Address address = await _dataService.GetAddressItem(contactItem.AddressIdNumber.Value); //_context.AddressDb.SingleAsync(a => a.AddressId == contactItem.AddressIdNumber);
                    if (address != null)
                    {
                        await _dataService.RemoveAddressItem(address.AddressId);
                    }
                }
                if (!contactItem.PictureLink.ToLower().StartsWith("http"))
                {
                    await _imageStore.DeleteImage(contactItem.PictureLink, BlobContainers.Contacts);
                }

                _context.ContactsDb.Remove(contactItem);
                await _context.SaveChangesAsync();
                await _dataService.RemoveContact(contactItem.ContactId, contactItem.ProgenyId);

                UserInfo userinfo = await _dataService.GetUserInfoByEmail(userEmail);
                string title = "Contact deleted for " + prog.NickName;
                string message = userinfo.FirstName + " " + userinfo.MiddleName + " " + userinfo.LastName + " deleted a contact for " + prog.NickName + ". Contact: " + contactItem.DisplayName;
                if (tItem != null)
                {
                    tItem.AccessLevel = 0;
                    await _azureNotifications.ProgenyUpdateNotification(title, message, tItem, userinfo.ProfilePicture);
                }

                return NoContent();
            }

            return NotFound();
        }

        [HttpGet("[action]/{id}")]
        public async Task<IActionResult> GetContactMobile(int id)
        {
            Contact result = await _dataService.GetContact(id);

            if (result != null)
            {
                string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
                UserAccess userAccess = await _dataService.GetProgenyUserAccessForUser(result.ProgenyId, userEmail);

                if (userAccess != null || result.ProgenyId == Constants.DefaultChildId)
                {
                    if (result.AddressIdNumber.HasValue)
                    {
                        result.Address = await _dataService.GetAddressItem(result.AddressIdNumber.Value);
                    }
                    if (!result.PictureLink.ToLower().StartsWith("http"))
                    {
                        result.PictureLink = _imageStore.UriFor(result.PictureLink, BlobContainers.Contacts);
                    }
                    return Ok(result);
                }
            }
            
            return NotFound();
        }

        [HttpGet]
        [Route("[action]/{id}/{accessLevel}")]
        public async Task<IActionResult> ProgenyMobile(int id, int accessLevel = 5)
        {
            List<Contact> contactsList = await _dataService.GetContactsList(id); 
            contactsList = contactsList.Where(c => c.AccessLevel >= accessLevel).ToList();

            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserAccess userAccess = await _dataService.GetProgenyUserAccessForUser(id, userEmail); 
            
            if ((userAccess != null || id == Constants.DefaultChildId) && contactsList.Any())
            {
                foreach (Contact cont in contactsList)
                {
                    if (cont.AddressIdNumber.HasValue)
                    {
                        cont.Address = await _dataService.GetAddressItem(cont.AddressIdNumber.Value);
                    }

                    if (!cont.PictureLink.ToLower().StartsWith("http"))
                    {
                        cont.PictureLink = _imageStore.UriFor(cont.PictureLink, BlobContainers.Contacts);
                    }
                }
                return Ok(contactsList);
            }

            return Ok(new List<Contact>());
        }

        [HttpGet]
        [Route("[action]/{contactId}")]
        public async Task<IActionResult> DownloadPicture(int contactId)
        {
            Contact contact = await _context.ContactsDb.SingleOrDefaultAsync(c => c.ContactId == contactId);
            if (contact == null)
            {
                return NotFound();
            }

            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserAccess userAccess = _context.UserAccessDb.AsNoTracking().SingleOrDefault(u =>
                u.ProgenyId == contact.ProgenyId && u.UserId.ToUpper() == userEmail.ToUpper());

            if (userAccess != null && userAccess.AccessLevel > 0 && contact.PictureLink.ToLower().StartsWith("http"))
            {
                using (Stream stream = await GetStreamFromUrl(contact.PictureLink))
                {
                    contact.PictureLink = await _imageStore.SaveImage(stream, BlobContainers.Contacts);
                }

                _context.ContactsDb.Update(contact);
                await _context.SaveChangesAsync();
                return Ok(contact);
            }

            return NotFound();
        }

        private static async Task<Stream> GetStreamFromUrl(string url)
        {
            using HttpClient client = new HttpClient();
            using HttpResponseMessage response = await client.GetAsync(url);
            await using Stream streamToReadFrom = await response.Content.ReadAsStreamAsync();
            return streamToReadFrom;
        }
    }
}
