using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace wu2.Models
{
    public class AdminIndexViewModel
    {
        public int GroupId { get; set; }
        public List<Groups> Groups { get; set; }
        public List<Notices> Announcements { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
    }
}