using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace wu2.Models
{
    public class DashboardViewModel
    {
        public Users Users { get; set; }
        public Notices Notices { get; set; }
        public List<SimplifiedExpenseViewModel> Expenses { get; set; }
    }
    public class SimplifiedExpenseViewModel
    {
        public int ExpenseId { get; set; }

        public string CreatedByName { get; set; }  

        public decimal TotalAmount { get; set; }  

        public string ExpenseType   { get; set; }
        public string item {  get; set; }
        
        public DateTime Createat { get; set; }
        public List<ExpenseMemberViewModel> ExpenseMembers { get; set; }  
    }

    public class ExpenseMemberViewModel
    {
        public string FullName { get; set; }

        public string Photo { get; set; } 
    }

}