using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace wu2.Models
{
    public class PairwiseDebtViewModel
    {
        public string DebtorName { get; set; }
        public string DebtorEmail { get; set; }
        public string DebtorPhoto { get; set; }
        public string CreditorName { get; set; }
        public string CreditorEmail { get; set; }
        public string CreditorPhoto { get; set; }
        public decimal Amount { get; set; }
        public int DebtorId { get; set; }  // Ensure these properties are present
        public int CreditorId { get; set; }  // Ensure these properties are present
        public int DebtId { get; set; }
        public string Currency { get; set; }
    }
    public class DebtDetail
    {
        public string RelatedUserName { get; set; }
        public decimal Amount { get; set; } // 正值表示應收，負值表示應付
    }
}