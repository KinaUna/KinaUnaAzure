using Microsoft.AspNetCore.DataProtection.Repositories;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Xml.Linq;
using KinaUna.Data.Contexts;
using KinaUna.Data.Models;

namespace KinaUna.IDP.Services
{
    // Source: https://nicolas.guelpa.me/blog/2017/01/11/dotnet-core-data-protection-keys-repository.html

    public class DataProtectionKeyRepository : IXmlRepository
    {
        private readonly ApplicationDbContext _context;

        public DataProtectionKeyRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public IReadOnlyCollection<XElement> GetAllElements()
        {
            return new ReadOnlyCollection<XElement>(_context.DataProtectionKeys.Select(k => XElement.Parse(k.XmlData)).ToList());
        }

        public void StoreElement(XElement element, string friendlyName)
        {
            var entity = _context.DataProtectionKeys.SingleOrDefault(k => k.FriendlyName == friendlyName);
            if (null != entity)
            {
                entity.XmlData = element.ToString();
                _context.DataProtectionKeys.Update(entity);
            }
            else
            {
                _context.DataProtectionKeys.Add(new DataProtectionKey
                {
                    FriendlyName = friendlyName,
                    XmlData = element.ToString()
                });
            }

            _context.SaveChanges();
        }
    }
}
