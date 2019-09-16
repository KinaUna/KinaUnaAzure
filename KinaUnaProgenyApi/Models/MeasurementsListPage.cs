using System.Collections.Generic;
using KinaUna.Data.Models;

namespace KinaUnaProgenyApi.Models
{
    public class MeasurementsListPage
    {
        public int PageNumber { get; set; }
        public int TotalPages { get; set; }
        public int SortBy { get; set; }
        public List<Measurement> MeasurementsList { get; set; }
        public Progeny Progeny { get; set; }
        public bool IsAdmin { get; set; }

        public MeasurementsListPage()
        {
            MeasurementsList = new List<Measurement>();
        }

    }
}
