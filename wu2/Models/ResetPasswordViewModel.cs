using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace wu2.Models
{
    public class ResetPasswordViewModel
    {
        [Required]
        public string Token { get; set; }

        [Required]
        [DataType(DataType.Password)]
        public string NewPassword { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [Compare("NewPassword", ErrorMessage = "密碼和確認密碼不匹配")]
        public string ConfirmPassword { get; set; }
    }
}