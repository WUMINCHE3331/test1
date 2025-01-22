using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Web;
namespace wu2.Models
{
    public class ChatViewModel
    {
        public IEnumerable<GroupContactViewModel> GroupContacts { get; set; }
        public Dictionary<string, string> Users { get; set; } 
    }

    public class GroupContactViewModel  
    {
        public int GroupId { get; set; }
        public string GroupName { get; set; }
        public string GroupsPhoto { get; set; }
        public string UnreadCount { get; set; }

    }
    public class MemberExpensePercentage
    {
        public string MemberName { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal Percentage { get; set; }
    }
    public class PersonalContactViewModel
    {
        public int UserId { get; set; }
        public string UserName { get; set; }
        public string UserPhoto { get; set; }
        public string UnreadCount { get; set; }
    }
}