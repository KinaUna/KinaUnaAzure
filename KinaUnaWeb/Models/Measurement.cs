﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace KinaUnaWeb.Models
{
    public class Measurement
    {
        public int MeasurementId { get; set; }
        public int ProgenyId { get; set; }
        public double Weight { get; set; }
        public double Height { get; set; }
        public double Circumference { get; set; }
        public string EyeColor { get; set; }
        public string HairColor { get; set; }
        public DateTime Date { get; set; }
        public DateTime CreatedDate { get; set; }
        public int AccessLevel { get; set; }
        public string Author { get; set; }

        [NotMapped]
        public Progeny Progeny { get; set; }
    }
}
