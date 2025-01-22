using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace wu2.Models
{
    public class SetContactInfoViewModel
    {

        [Required]
        [EmailAddress]
        [DisplayName("信箱")]
        public string Email { get; set; }
        [Required]
        [DisplayName("密碼")]
        public string Password { get; set; }
    }
}