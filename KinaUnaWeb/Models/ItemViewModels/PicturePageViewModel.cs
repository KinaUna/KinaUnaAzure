﻿using System.Collections.Generic;
using KinaUna.Data.Models;

namespace KinaUnaWeb.Models.ItemViewModels
{
    public class PicturePageViewModel: BaseViewModel
    {
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
        public int SortBy { get; set; }
        public List<Picture> PicturesList { get; set; }
        public Progeny Progeny { get; set; }
        public bool IsAdmin { get; set; }
        public string TagFilter { get; set; }
        public string TagsList { get; set; }
    }
}
