using KinaUnaProgenyApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using KinaUna.Data.Contexts;
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

        public ContactsController(ProgenyDbContext context, ImageStore imageStore)
        {
            _context = context;
            _imageStore = imageStore;
        }
        // GET api/contacts
        [HttpGet]
        public async Task<IActionResult> Get()
        {
            List<Contact> resultList = await _context.ContactsDb.AsNoTracking().ToListAsync();

            return Ok(resultList);
        }

        // GET api/contacts/progeny/[id]
        [HttpGet]
        [Route("[action]/{id}")]
        public async Task<IActionResult> Progeny(int id, [FromQuery] int accessLevel = 5)
        {
            List<Contact> contactsList = await _context.ContactsDb.AsNoTracking().Where(c => c.ProgenyId == id && c.AccessLevel >= accessLevel).ToListAsync();
            if (contactsList.Any())
            {
                return Ok(contactsList);
            }
            else
            {
                return NotFound();
            }

        }

        // GET api/contacts/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetContactItem(int id)
        {
            Contact result = await _context.ContactsDb.AsNoTracking().SingleOrDefaultAsync(c => c.ContactId == id);

            return Ok(result);
        }

        // POST api/contact
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] Contact value)
        {
            // Todo: address info
            Contact contactItem = new Contact();
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

            _context.ContactsDb.Add(contactItem);
            await _context.SaveChangesAsync();

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

            _context.ContactsDb.Update(contactItem);
            await _context.SaveChangesAsync();

            return Ok(contactItem);
        }

        // DELETE api/contacts/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            Contact contactItem = await _context.ContactsDb.SingleOrDefaultAsync(c => c.ContactId == id);
            if (contactItem != null)
            {
                _context.ContactsDb.Remove(contactItem);
                await _context.SaveChangesAsync();
                return NoContent();
            }
            else
            {
                return NotFound();
            }
        }

        [HttpGet("[action]/{id}")]
        public async Task<IActionResult> GetContactMobile(int id)
        {
            Contact result = await _context.ContactsDb.AsNoTracking().SingleOrDefaultAsync(c => c.ContactId == id);
            if (!result.PictureLink.ToLower().StartsWith("http"))
            {
                result.PictureLink = _imageStore.UriFor(result.PictureLink, "contacts");
            }
            return Ok(result);
        }

        [HttpGet]
        [Route("[action]/{id}/{accessLevel}")]
        public async Task<IActionResult> ProgenyMobile(int id, int accessLevel = 5)
        {
            List<Contact> contactsList = await _context.ContactsDb.AsNoTracking().Where(c => c.ProgenyId == id && c.AccessLevel >= accessLevel).ToListAsync();
            if (contactsList.Any())
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
            else
            {
                return Ok(new List<Contact>());
            }

        }

        [HttpGet]
        [Route("[action]/{contactId}")]
        public async Task<IActionResult> DownloadPicture(int contactId)
        {
            Contact contact = await _context.ContactsDb.SingleOrDefaultAsync(c => c.ContactId == contactId);
            if (contact != null && contact.PictureLink.ToLower().StartsWith("http"))
            {
                using (Stream stream = GetStreamFromUrl(contact.PictureLink))
                {
                    contact.PictureLink = await _imageStore.SaveImage(stream, "contacts");
                }

                _context.ContactsDb.Update(contact);
                await _context.SaveChangesAsync();
                return Ok(contact);
            }
            else
            {
                return NotFound();
            }
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
