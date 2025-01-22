using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace wu2.Models
{
    public class GroupDetailsViewModel
    {

        public string GroupName { get; set; }
       
        public string Currency { get; set; }
        public DateTime CreateDate { get; set; }
        public string GroupPhoto { get; set; }
        public decimal Budget { get; set; }
        public List<ExpenseViewModel> Expenses { get; set; }
        public List<DebtViewModel> Debts { get; set; }
        public List<GroupMemberViewModel> Members { get; set; }
        public List<PairwiseDebtViewModel> DebtSettlements { get; set; }
        public List<MemberExpenseViewModel> MemberExpenses { get; set; }

        public class GroupMemberViewModel
        {
            public int UserId { get; set; } 
            public string FullName { get; set; }
            public string Email { get; set; }
            public string Role { get; set; }
            public DateTime? JoinedDate { get; set; }
            public decimal TotalSpent { get; set; } 
            public string ProfilePhoto { get; set; }
      
        }

        public class MemberExpenseViewModel
        {
            public string FullName { get; set; }
            public decimal TotalAmount { get; set; }
        }


    }
}