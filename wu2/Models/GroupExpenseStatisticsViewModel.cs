using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace wu2.Models
{
    public class GroupExpenseStatisticsViewModel
    {
        public List<string> Labels { get; set; } 
        public List<decimal> TotalExpenses { get; set; } 
        public List<decimal> TotalDebts { get; set; } 
        public List<string> ExpenseTypes { get; set; } 
        public List<decimal> TypeExpenses { get; set; } 
        public List<decimal> PendingExpenses { get; set; } 
        public List<decimal> NetProfits { get; set; }
        public List<string> PaymentMethodTypes { get; set; }
        public List<int> PaymentMethodCounts{ get; set; }
    }
}