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

    public class DataProtectionKeyRepository(ApplicationDbContext context) : IXmlRepository
    {
        public IReadOnlyCollection<XElement> GetAllElements()
        {
            return new ReadOnlyCollection<XElement>([.. context.DataProtectionKeys.Select(k => XElement.Parse(k.XmlData))]);
        }

        public void StoreElement(XElement element, string friendlyName)
        {
            DataProtectionKey entity = context.DataProtectionKeys.SingleOrDefault(k => k.FriendlyName == friendlyName);
            if (null != entity)
            {
                entity.XmlData = element.ToString();
                context.DataProtectionKeys.Update(entity);
            }
            else
            {
                context.DataProtectionKeys.Add(new DataProtectionKey
                {
                    FriendlyName = friendlyName,
                    XmlData = element.ToString()
                });
            }

            context.SaveChanges();
        }
    }
}
