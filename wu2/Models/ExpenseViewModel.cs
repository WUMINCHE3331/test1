using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Web;
using System.Web.Mvc;

namespace wu2.Models
{
    public class AmountDetail
    {
        public decimal Amount { get; set; }
    }

    public class ExpenseViewModel
    {  // 用於存放用戶自訂輸入的值
        public string CustomExpenseType { get; set; }

        // 判斷是否為自訂類型
        public bool IsCustomExpenseType { get; set; }
        public bool IsPaid { get; set; }
        public int ExpenseId { get; set; }
        public string CreatedByName { get; set; }
        public decimal Portion { get; set; } 
        [DisplayName("群組 ID")]
        public int GroupId { get; set; }

        [DisplayName("描述")]
        public string Description { get; set; }
        public Nullable<decimal> Budget { get; set; }
        [DisplayName("總金額")]
        public decimal TotalAmount { get; set; }

        [DisplayName("付款人")]
        public List<ExpensePayerViewModel> PaidBy { get; set; }

        [DisplayName("支出類型")]
        public string ExpenseType { get; set; }
            [DisplayName("備註")]
        public string Note { get; set; }
        [DisplayName("支出項目")]
        public string ExpenseItem { get; set; }

        [DisplayName("支付方式")]
        public string PaymentMethod { get; set; }

        [DisplayName("日期")]
        public DateTime? Date { get; set; }

        [DisplayName("備註")]
        public string PayerNote { get; set; }

        [DisplayName("照片")]
        public string Photo { get; set; }

        [DisplayName("分配明細")]
        public List<ExpenseDetailViewModel> SplitDetails { get; set; }
    }

    public class ExpensePayerViewModel
    {
        [DisplayName("用戶 ID")]
        
        public int UserId { get; set; }


        [DisplayName("金額")]
        public decimal Amount { get; set; }

        [DisplayName("已勾選")]
        public bool IsChecked { get; set; }

    }

    public class ExpenseDetailViewModel
    {
        [DisplayName("用戶 ID")]
        public int UserId { get; set; }
        public string Photo { get; set; }

        [DisplayName("金額")]
        public decimal Amount { get; set; }

        [DisplayName("備註")]
        public string Note { get; set; }

        [DisplayName("已勾選")]
        public bool IsChecked { get; set; }
    }
}
