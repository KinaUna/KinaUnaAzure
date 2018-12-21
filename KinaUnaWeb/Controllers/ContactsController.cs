using KinaUnaWeb.Data;
using KinaUnaWeb.Models;
using KinaUnaWeb.Models.ItemViewModels;
using KinaUnaWeb.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KinaUnaWeb.Controllers
{
    public class ContactsController : Controller
    {
        private readonly WebDbContext _context;
        private int _progId = 2;
        private readonly IProgenyHttpClient _progenyHttpClient;
        private readonly ImageStore _imageStore;
        private bool _userIsProgenyAdmin = false;
        private readonly string _defaultUser = "testuser@niviaq.com";

        public ContactsController(WebDbContext context, IProgenyHttpClient progenyHttpClient, ImageStore imageStore)
        {
            _context = context;
            _progenyHttpClient = progenyHttpClient;
            _imageStore = imageStore;
        }

        [AllowAnonymous]
        public async Task<IActionResult> Index(int childId = 0, string tagFilter = "")
        {
            _progId = childId;
            string userEmail = HttpContext.User.FindFirst("email")?.Value ?? _defaultUser;
            string userTimeZone = HttpContext.User.FindFirst("timezone")?.Value ?? "Romance Standard Time";
            if (string.IsNullOrEmpty(userTimeZone))
            {
                userTimeZone = "Romance Standard Time";
            }
            UserInfo userinfo = await _progenyHttpClient.GetUserInfo(userEmail);
            if (childId == 0 && userinfo.ViewChild > 0)
            {
                _progId = userinfo.ViewChild;
            }
            else
            {
                _progId = childId;
            }

            if (_progId == 0)
            {
                _progId = 2;
            }


            Progeny progeny = new Progeny();
            progeny = await _progenyHttpClient.GetProgeny(_progId);
            List<UserAccess> accessList = await _progenyHttpClient.GetProgenyAccessList(_progId);

            int userAccessLevel = 5;

            if (accessList.Count != 0)
            {
                UserAccess userAccess = accessList.SingleOrDefault(u => u.UserId.ToUpper() == userEmail.ToUpper());
                if (userAccess != null)
                {
                    userAccessLevel = userAccess.AccessLevel;
                }
            }

            if (progeny.Admins.ToUpper().Contains(userEmail.ToUpper()))
            {
                _userIsProgenyAdmin = true;
                userAccessLevel = 0;
            }

            List<ContactViewModel> model = new List<ContactViewModel>();
            
            List<string> tagsList = new List<string>();

            List<Contact> cList = _context.ContactsDb.Where(w => w.ProgenyId == _progId).ToList();
            if (!string.IsNullOrEmpty(tagFilter))
            {
                cList = _context.ContactsDb.Where(f => f.ProgenyId == _progId && f.Tags.Contains(tagFilter)).ToList();
            }

            if (cList.Count != 0)
            {
                foreach (Contact c in cList)
                {
                    ContactViewModel cIvm = new ContactViewModel();
                    cIvm.ProgenyId = c.ProgenyId;
                    cIvm.AccessLevel = c.AccessLevel;
                    cIvm.FirstName = c.FirstName;
                    cIvm.MiddleName = c.MiddleName;
                    cIvm.LastName = c.LastName;
                    cIvm.DisplayName = c.DisplayName;
                    cIvm.AddressIdNumber = c.AddressIdNumber;
                    cIvm.Email1 = c.Email1;
                    cIvm.Email2 = c.Email2;
                    cIvm.PhoneNumber = c.PhoneNumber;
                    cIvm.MobileNumber = c.MobileNumber;
                    cIvm.Notes = c.Notes;
                    cIvm.PictureLink = c.PictureLink;
                    cIvm.Active = c.Active;
                    cIvm.IsAdmin = _userIsProgenyAdmin;
                    cIvm.ContactId = c.ContactId;
                    cIvm.Context = c.Context;
                    cIvm.Website = c.Website;
                    if (c.AddressIdNumber != null)
                    {
                        Address address = await _context.AddressDb.SingleAsync(a => a.AddressId == c.AddressIdNumber);
                        cIvm.AddressLine1 = address.AddressLine1;
                        cIvm.AddressLine2 = address.AddressLine2;
                        cIvm.City = address.City;
                        cIvm.State = address.State;
                        cIvm.PostalCode = address.PostalCode;
                        cIvm.Country = address.Country;
                    }
                    cIvm.Tags = c.Tags;
                    if (!String.IsNullOrEmpty(cIvm.Tags))
                    {
                        List<string> cvmTags = cIvm.Tags.Split(',').ToList();
                        foreach (string tagstring in cvmTags)
                        {
                            if (!tagsList.Contains(tagstring.TrimStart(' ', ',').TrimEnd(' ', ',')))
                            {
                                tagsList.Add(tagstring.TrimStart(' ', ',').TrimEnd(' ', ','));
                            }
                        }
                    }

                    if (!cIvm.PictureLink.StartsWith("https://"))
                    {
                        cIvm.PictureLink = _imageStore.UriFor(cIvm.PictureLink, "contacts");
                    }

                    if (cIvm.AccessLevel >= userAccessLevel)
                    {
                        model.Add(cIvm);
                    }
                }
                model = model.OrderBy(m => m.DisplayName).ToList();

                string tList = "";
                foreach (string tstr in tagsList)
                {
                    tList = tList + tstr + ",";
                }
                ViewBag.Tags = tList.TrimEnd(',');
            }
            else
            {
                ContactViewModel c = new ContactViewModel();
                c.ProgenyId = _progId;
                c.DisplayName = "No friends found.";
                c.PictureLink = "https://web.kinauna.com/photodb/profile.jpg";
                c.IsAdmin = _userIsProgenyAdmin;
                model.Add(c);
            }

            model[0].Progeny = progeny;
            ViewBag.TagFilter = tagFilter;
            return View(model);

        }

        [AllowAnonymous]
        public async Task<IActionResult> ContactDetails(int contactId, string tagFilter)
        {
            string userEmail = HttpContext.User.FindFirst("email")?.Value ?? _defaultUser;
            string userTimeZone = HttpContext.User.FindFirst("timezone")?.Value ?? "Romance Standard Time";
            if (string.IsNullOrEmpty(userTimeZone))
            {
                userTimeZone = "Romance Standard Time";
            }
            UserInfo userinfo = await _progenyHttpClient.GetUserInfo(userEmail);
            Contact contact = await _context.ContactsDb.SingleAsync(c => c.ContactId == contactId);
            Progeny progeny = new Progeny();
            progeny = await _progenyHttpClient.GetProgeny(contact.ProgenyId);
            List<UserAccess> accessList = await _progenyHttpClient.GetProgenyAccessList(_progId);

            int userAccessLevel = 5;

            if (accessList.Count != 0)
            {
                UserAccess userAccess = accessList.SingleOrDefault(u => u.UserId.ToUpper() == userEmail.ToUpper());
                if (userAccess != null)
                {
                    userAccessLevel = userAccess.AccessLevel;
                }
            }

            if (progeny.Admins.ToUpper().Contains(userEmail.ToUpper()))
            {
                _userIsProgenyAdmin = true;
                userAccessLevel = 0;
            }


            ContactViewModel model = new ContactViewModel();
            
            if (contact.AccessLevel < userAccessLevel)
            {
                RedirectToAction("Index");
            }
            model.ProgenyId = contact.ProgenyId;
            model.Context = contact.Context;
            model.Notes = contact.Notes;
            model.AccessLevel = contact.AccessLevel;
            model.ContactId = contact.ContactId;
            model.PictureLink = contact.PictureLink;
            model.Active = contact.Active;
            model.FirstName = contact.FirstName;
            model.MiddleName = contact.MiddleName;
            model.LastName = contact.LastName;
            model.DisplayName = contact.DisplayName;
            model.AddressIdNumber = contact.AddressIdNumber;
            model.Email1 = contact.Email1;
            model.Email2 = contact.Email2;
            model.PhoneNumber = contact.PhoneNumber;
            model.MobileNumber = contact.MobileNumber;
            model.Website = contact.Website;
            model.Tags = contact.Tags;
            if (contact.AddressIdNumber != null)
            {
                Address address = await _context.AddressDb.SingleAsync(a => a.AddressId == contact.AddressIdNumber);
                model.AddressLine1 = address.AddressLine1;
                model.AddressLine2 = address.AddressLine2;
                model.City = address.City;
                model.State = address.State;
                model.PostalCode = address.PostalCode;
                model.Country = address.Country;
            }

            model.Progeny = progeny;

            if (!model.PictureLink.StartsWith("https://"))
            {
                model.PictureLink = _imageStore.UriFor(model.PictureLink, "contacts");
            }

            List<string> tagsList = new List<string>();
            var contactsList1 = _context.ContactsDb.Where(c => c.ProgenyId == model.ProgenyId).ToList();
            foreach (Contact cont in contactsList1)
            {
                if (!String.IsNullOrEmpty(cont.Tags))
                {
                    List<string> cvmTags = cont.Tags.Split(',').ToList();
                    foreach (string tagstring in cvmTags)
                    {
                        if (!tagsList.Contains(tagstring.TrimStart(' ', ',').TrimEnd(' ', ',')))
                        {
                            tagsList.Add(tagstring.TrimStart(' ', ',').TrimEnd(' ', ','));
                        }
                    }
                }
            }

            string tagItems = "[";
            if (tagsList.Any())
            {
                foreach (string tagstring in tagsList)
                {
                    tagItems = tagItems + "'" + tagstring + "',";
                }

                tagItems = tagItems.Remove(tagItems.Length - 1);
                tagItems = tagItems + "]";
            }

            ViewBag.TagsList = tagItems;
            ViewBag.TagFilter = tagFilter;

            model.IsAdmin = _userIsProgenyAdmin;

            return View(model);
        }
    }
}