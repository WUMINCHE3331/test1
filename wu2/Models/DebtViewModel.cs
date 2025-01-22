using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Web;

namespace wu2.Models
{
    public class DebtViewModel
    {

        [DisplayName("債務 ID")]
        public int DebtId { get; set; }

        [DisplayName("群組 ID")]
        public int GroupId { get; set; }

        [DisplayName("債權人")]
        public string CreditorName { get; set; }

        [DisplayName("債務人")]
        public string DebtorName { get; set; }

        [DisplayName("金額")]
        public decimal Amount { get; set; }

        [DisplayName("是否已支付")]
        public bool IsPaid { get; set; }

        [DisplayName("創建日期")]
        public DateTime CreatedAt { get; set; }

        [DisplayName("更新日期")]
        public DateTime? UpdatedAt { get; set; }
        public Nullable<bool> IsConfirmed { get; set; }
        public Nullable<bool> IsPending{ get; set; }
        public int ExpenseId { get; set; }
    }
}