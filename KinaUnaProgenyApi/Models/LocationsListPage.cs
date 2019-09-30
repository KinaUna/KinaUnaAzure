using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KinaUna.Data.Models;

namespace KinaUnaProgenyApi.Models
{
    public class LocationsListPage
    {
        public int PageNumber { get; set; }
        public int TotalPages { get; set; }
        public int SortBy { get; set; }
        public List<Location> LocationsList { get; set; }
        public Progeny Progeny { get; set; }
        public bool IsAdmin { get; set; }

        public LocationsListPage()
        {
            LocationsList = new List<Location>();
        }
    }
}
