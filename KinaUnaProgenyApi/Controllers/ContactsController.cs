using KinaUnaProgenyApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

        public ContactsController(ProgenyDbContext context, ImageStore imageStore, IDataService dataService)
        {
            _context = context;
            _imageStore = imageStore;
            _dataService = dataService;
        }
        
        // GET api/contacts/progeny/[id]
        [HttpGet]
        [Route("[action]/{id}")]
        public async Task<IActionResult> Progeny(int id, [FromQuery] int accessLevel = 5)
        {
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserAccess userAccess = await _dataService.GetProgenyUserAccessForUser(id, userEmail); // _context.UserAccessDb.AsNoTracking().SingleOrDefault(u => u.ProgenyId == id && u.UserId.ToUpper() == userEmail.ToUpper());
            if (userAccess != null || id == Constants.DefaultChildId)
            {
                List<Contact> contactsList = await _dataService.GetContactsList(id); // await _context.ContactsDb.AsNoTracking().Where(c => c.ProgenyId == id && c.AccessLevel >= accessLevel).ToListAsync();
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
            Contact result = await _dataService.GetContact(id); // await _context.ContactsDb.AsNoTracking().SingleOrDefaultAsync(c => c.ContactId == id);

            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserAccess userAccess = await _dataService.GetProgenyUserAccessForUser(result.ProgenyId, userEmail); // _context.UserAccessDb.AsNoTracking().SingleOrDefault(u => u.ProgenyId == result.ProgenyId && u.UserId.ToUpper() == userEmail.ToUpper());
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

                if (!prog.Admins.ToUpper().Contains(userEmail.ToUpper()))
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
            contactItem.DateAdded = DateTime.UtcNow;
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
            contactItem.PictureLink = value.PictureLink;
            contactItem.Tags = value.Tags;
            contactItem.Website = value.Website;
            contactItem.Address = value.Address;

            if (contactItem.Address != null)
            {
                _context.AddressDb.Add(contactItem.Address);
                await _context.SaveChangesAsync();
                contactItem.AddressIdNumber = contactItem.Address.AddressId;
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
            tItem.ProgenyTime = DateTime.UtcNow;

            await _context.TimeLineDb.AddAsync(tItem);
            await _context.SaveChangesAsync();
            await _dataService.SetTimeLineItem(tItem.TimeLineId);

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

                if (!prog.Admins.ToUpper().Contains(userEmail.ToUpper()))
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
            contactItem.DateAdded = DateTime.UtcNow;
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
            contactItem.PictureLink = value.PictureLink;
            contactItem.Tags = value.Tags;
            contactItem.Website = value.Website;
            contactItem.Address = value.Address;
            if (contactItem.AddressIdNumber != null)
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
                }
                else
                {
                    _context.AddressDb.Remove(addressOld);
                    contactItem.AddressIdNumber = null;
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

            return Ok(contactItem);
        }

        // DELETE api/contacts/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            Contact contactItem = await _context.ContactsDb.SingleOrDefaultAsync(c => c.ContactId == id);
            if (contactItem != null)
            {
                // Check if child exists.
                Progeny prog = await _context.ProgenyDb.SingleOrDefaultAsync(p => p.Id == contactItem.ProgenyId);
                if (prog != null)
                {
                    // Check if user is allowed to delete contacts for this child.
                    string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
                    if (!prog.Admins.ToUpper().Contains(userEmail.ToUpper()))
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
                    Address address = await _context.AddressDb.SingleAsync(a => a.AddressId == contactItem.AddressIdNumber);
                    _context.AddressDb.Remove(address);
                    await _dataService.RemoveAddressItem(address.AddressId);
                }
                if (!contactItem.PictureLink.ToLower().StartsWith("http"))
                {
                    await _imageStore.DeleteImage(contactItem.PictureLink, "contacts");
                }

                _context.ContactsDb.Remove(contactItem);
                await _context.SaveChangesAsync();
                await _dataService.RemoveContact(contactItem.ContactId, contactItem.ProgenyId);

                return NoContent();
            }

            return NotFound();
        }

        [HttpGet("[action]/{id}")]
        public async Task<IActionResult> GetContactMobile(int id)
        {
            Contact result = await _dataService.GetContact(id); // await _context.ContactsDb.AsNoTracking().SingleOrDefaultAsync(c => c.ContactId == id);

            if (result != null)
            {
                string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
                UserAccess userAccess = await _dataService.GetProgenyUserAccessForUser(result.ProgenyId, userEmail); // _context.UserAccessDb.AsNoTracking().SingleOrDefault(u => u.ProgenyId == result.ProgenyId && u.UserId.ToUpper() == userEmail.ToUpper());

                if (userAccess != null || result.ProgenyId == Constants.DefaultChildId)
                {
                    if (!result.PictureLink.ToLower().StartsWith("http"))
                    {
                        result.PictureLink = _imageStore.UriFor(result.PictureLink, "contacts");
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
            List<Contact> contactsList = await _dataService.GetContactsList(id); // await _context.ContactsDb.AsNoTracking().Where(c => c.ProgenyId == id && c.AccessLevel >= accessLevel).ToListAsync();
            contactsList = contactsList.Where(c => c.AccessLevel >= accessLevel).ToList();

            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserAccess userAccess = await _dataService.GetProgenyUserAccessForUser(id, userEmail); // _context.UserAccessDb.AsNoTracking().SingleOrDefault(u => u.ProgenyId == id && u.UserId.ToUpper() == userEmail.ToUpper());
            
            if ((userAccess != null || id == Constants.DefaultChildId) && contactsList.Any())
            {
                foreach (Contact cont in contactsList)
                {
                    if (!cont.PictureLink.ToLower().StartsWith("http"))
                    {
                        cont.PictureLink = _imageStore.UriFor(cont.PictureLink, "contacts");
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
                using (Stream stream = GetStreamFromUrl(contact.PictureLink))
                {
                    contact.PictureLink = await _imageStore.SaveImage(stream, "contacts");
                }

                _context.ContactsDb.Update(contact);
                await _context.SaveChangesAsync();
                return Ok(contact);
            }

            return NotFound();
        }

        private static Stream GetStreamFromUrl(string url)
        {
            byte[] imageData;

            using (var wc = new System.Net.WebClient())
                imageData = wc.DownloadData(url);

            return new MemoryStream(imageData);
        }
    }
}
