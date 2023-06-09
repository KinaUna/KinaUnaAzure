﻿using System.ComponentModel.DataAnnotations;

namespace KinaUna.IDP.Models.AccountViewModels
{
    public class ExternalLoginViewModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }
    }
}
