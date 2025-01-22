using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Web;

namespace wu2.Models
{
    public class MemberDebtViewModel
    {
        [DisplayName("使用者ID")]
        public int UserId { get; set; }

        [DisplayName("使用者名稱")]
        public string UserName { get; set; }
        public string UserEmail { get; set; }
        public string UserPhoto { get; set; }


        [DisplayName("總欠款金額")]
        public decimal TotalOwed { get; set; }

        [DisplayName("總欠款給")]
        public decimal TotalOwedTo { get; set; }
        [DisplayName("淨款")]
        public decimal NetDebt { get; set; } 
    }
}