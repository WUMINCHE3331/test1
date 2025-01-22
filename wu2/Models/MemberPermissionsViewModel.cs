using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace wu2.Models
{
    public class MemberPermissionsViewModel
    {
        public int UserId { get; set; }
        public string UserName { get; set; }
        public string Role { get; set; }
    }
}