using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using System.Web.Mvc;
namespace wu2.Models
{
    public class InviteViewModel
    {
        [DisplayName("群組 ID")]
        public int GroupId { get; set; }

        [DisplayName("用戶 ID")]
        public int UserId { get; set; }
  
        [DisplayName("電子郵件")]
        [EmailAddress(ErrorMessage = "請輸入有效的電子郵件地址")]
        public string Email { get; set; }
     
        [DisplayName("角色")]
        public string Role { get; set; }

        [DisplayName("角色選項")]
        public IEnumerable<SelectListItem> RoleOptions { get; set; }
    }
}