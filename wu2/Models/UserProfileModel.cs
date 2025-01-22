using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace wu2.Models
{
    public class AddExpenseResult
    {
        public bool IsSuccess { get; set; } // 表示操作是否成功
        public string Message { get; set; } // 返回的訊息
        public int ExpenseId { get; set; } // 新增的支出 ID
        public List<MemberExpenseDetail> MemberDetails { get; set; } // 每個成員的分攤詳細資訊
    }

    public class MemberExpenseDetail
    {
        public string MemberName { get; set; } // 成員名稱
        public decimal Amount { get; set; } // 成員分攤金額
    }

    public class GroupInfo
    {
        public string GroupName { get; set; }
        public string PictureUrl { get; set; }
    }
    public class MemberSettlement
    {
        public int UserId { get; set; }
        public string UserName { get; set; }
        public decimal NetAmount { get; set; } 
    }

    public class LoginViewModel
    {
        [Required(ErrorMessage = "電子郵件是必填欄位")]
        [EmailAddress(ErrorMessage = "請輸入有效的電子郵件地址")]
        public string Email { get; set; }

        [Required(ErrorMessage = "密碼是必填欄位")]
        [DataType(DataType.Password)]
        public string PasswordHash { get; set; }
    }

    public class UserProfileModel
    {
        public string UserId { get; set; }
        public string DisplayName { get; set; }
        public string PictureUrl { get; set; }
    }
    public class UserProfileViewModel
    {
        public string UserId { get; set; }
        public string DisplayName { get; set; }
        public string Email { get; set; }
        public string Role { get; set; }
        public string ProfilePhoto { get; set; }
    }

    public class UserViewModel
    {
        public int UserId { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string OldPassword { get; set; }
        public string NewPassword { get; set; }
        public string ConfirmPassword { get; set; }
        public string ProfilePhoto { get; set; }
        public string BankAccount { get; set; }
        
        public DateTime Joindate { get; set; }
        public string PhoneNumber { get; set; }

        public DateTime? RegistrationDate { get; set; }
    }
}