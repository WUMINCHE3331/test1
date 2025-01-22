using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Web;

namespace wu2.Models
{

    public class MemberDebtDetailViewModel
    {
        [DisplayName("使用者名稱")]
        public string UserName { get; set; }

        [DisplayName("項目名稱")]
        public string ItemName { get; set; }

        [DisplayName("金額")]
        public decimal Amount { get; set; }
    }

    public class MemberDebtOverviewViewModel
    {
        [DisplayName("使用者名稱")]
        public string UserName { get; set; }

        [DisplayName("總欠款")]
        public decimal TotalDebt { get; set; }

        [DisplayName("欠款明細")]
        public List<MemberDebtDetailViewModel> DebtDetails { get; set; }
    }
}