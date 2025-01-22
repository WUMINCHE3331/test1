using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using wu2.Models;

namespace wu2.Controllers
{
    public class ChartController : Controller
    {
        wuEntities1 entities = new wuEntities1();
        // GET: Chart

        public ActionResult MemberExpenseChart(int groupId)
        {
            var expenses = entities.Expenses.Where(e => e.GroupId == groupId).ToList();

            var memberExpenses = expenses
                .GroupBy(e => e.Users.FullName)
                .Select(g => new
                {
                    MemberName = g.Key,
                    TotalAmount = g.Sum(e => e.TotalAmount)
                })
                .ToList();

            var totalExpenses = memberExpenses.Sum(me => me.TotalAmount);

            var memberExpensePercentages = memberExpenses
                .Select(me => new MemberExpensePercentage
                {
                    MemberName = me.MemberName,
                    TotalAmount = me.TotalAmount,
                    Percentage = me.TotalAmount / totalExpenses * 100
                })
                .ToList();

            return View(memberExpensePercentages);
        }

        public ActionResult GroupExpenseStatistics(int groupId)
        {
            var groupExpenses = entities.ExpenseDetails
                .Where(d => d.Expenses.GroupId == groupId)
                .GroupBy(d => new { d.UserId, d.Users.FullName })
                .Select(g => new
                {
                    FullName = g.Key.FullName,
                    TotalAmount = g.Sum(d => d.Amount)
                })
                .ToList();

            var groupDebts = entities.Debts
                .Where(d => d.GroupId == groupId && !d.IsPaid)
                .GroupBy(d => new { d.DebtorId, d.Users.FullName })
                .Select(g => new
                {
                    FullName = g.Key.FullName,
                    TotalDebt = g.Sum(d => d.Amount)
                })
                .ToList();

            var expenseTypes = entities.Expenses
                .Where(e => e.GroupId == groupId)
                .GroupBy(e => e.ExpenseType)
                .Select(g => new
                {
                    Type = g.Key,
                    TotalAmount = g.Sum(e => e.TotalAmount)
                })
                .ToList();

            var paymentMethods = entities.Expenses
     .Where(e => e.GroupId == groupId)
     .GroupBy(e => e.PaymentMethod)
     .Select(g => new
     {
         Type = g.Key,
         Count = g.Count()
     })
     .ToList();

            var members = entities.GroupMembers
                .Where(gm => gm.GroupId == groupId)
                .Select(gm => gm.Users)
                .Distinct()
                .ToList();

            var debts = entities.Debts
                .Where(d => d.GroupId == groupId)
                .ToList();

            var memberDebtOverview = members.Select(user => new MemberDebtViewModel
            {
                UserId = user.UserId,
                UserName = user.FullName,
                TotalOwed = debts.Where(d => d.DebtorId == user.UserId && !d.IsPaid).Sum(d => d.Amount),
                TotalOwedTo = debts.Where(d => d.CreditorId == user.UserId && !d.IsPaid).Sum(d => d.Amount),
                NetDebt = debts.Where(d => d.CreditorId == user.UserId && !d.IsPaid).Sum(d => d.Amount) - debts.Where(d => d.DebtorId == user.UserId && !d.IsPaid).Sum(d => d.Amount)
            }).ToList();

            var allMembers = entities.GroupMembers
                .Where(gm => gm.GroupId == groupId)
                .Select(gm => gm.Users.FullName)
                .ToList();

            var memberTotalExpenses = allMembers.Select(member => new
            {
                FullName = member,
                TotalAmount = groupExpenses.FirstOrDefault(ge => ge.FullName == member)?.TotalAmount ?? 0
            }).ToList();

            var viewModel = new GroupExpenseStatisticsViewModel
            {
                Labels = memberTotalExpenses.Select(ge => ge.FullName).ToList(),
                TotalExpenses = memberTotalExpenses.Select(ge => ge.TotalAmount).ToList(),
                TotalDebts = memberDebtOverview.Select(m => m.TotalOwed).ToList(), // 修改这里以确保债务数据正确
                ExpenseTypes = expenseTypes.Select(et => et.Type).ToList(),
                TypeExpenses = expenseTypes.Select(et => et.TotalAmount).ToList(),
                NetProfits = memberDebtOverview.Select(m => m.NetDebt).ToList(), // 添加净利数据
                        PaymentMethodTypes = paymentMethods.Select(pm => pm.Type).ToList(),
                PaymentMethodCounts = paymentMethods.Select(pm => pm.Count).ToList()
            };

            return View(viewModel);
        }



    }
}