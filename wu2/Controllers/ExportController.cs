using Rotativa;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using wu2.Models;
using static wu2.Models.GroupDetailsViewModel;

namespace wu2.Controllers
{
    public class ExportController : Controller
    {
        wuEntities1 db = new wuEntities1();
        // GET: Export
        public ActionResult Index()
        {
            return View();
        }
        public ActionResult ExportGroupDetailsToPdf(int groupId)
        {
            var group = db.Groups.FirstOrDefault(g => g.GroupId == groupId);

            if (group == null)
            {
                return HttpNotFound();
            }

            var members = db.GroupMembers
                            .Where(m => m.GroupId == groupId)
                            .Include(m => m.Users)
                            .Select(m => new GroupMemberViewModel
                            {
                                UserId = m.Users.UserId,
                                FullName = m.Users.FullName,
                                Email = m.Users.Email,
                                Role = m.Role,
                                JoinedDate = m.JoinedDate.HasValue ? m.JoinedDate.Value : DateTime.MinValue,
                                ProfilePhoto = m.Users.ProfilePhoto ?? "/path/to/default/photo.jpg",
                                TotalSpent = db.ExpenseDetails
                                        .Where(d => d.Expenses.GroupId == groupId && d.UserId == m.UserId)
                                        .Sum(d => (decimal?)d.Amount) ?? 0
                            })
                            .ToList() ?? new List<GroupMemberViewModel>();

            var expenses = db.Expenses
                             .Where(e => e.GroupId == groupId)
                             .Select(e => new ExpenseViewModel
                             {
                                 ExpenseId = e.ExpenseId,
                                 TotalAmount = e.TotalAmount,
                                 ExpenseType = e.ExpenseType,
                                 ExpenseItem = e.ExpenseItem,
                                 PaymentMethod = e.PaymentMethod,
                                 Note = e.Note,
                                 Photo = e.Photo,
                                 Date = e.CreatedAt,
                                 CreatedByName = e.Users.FullName
                             })
                             .ToList() ?? new List<ExpenseViewModel>();

            var debts = db.Debts
                         .Where(d => d.GroupId == groupId)
                         .ToList() ?? new List<Debts>();

            var memberDebtOverview = members.Select(user => new MemberDebtViewModel
            {
                UserId = user.UserId,
                UserName = user.FullName,
                UserEmail = user.Email,
                UserPhoto = user.ProfilePhoto,
                TotalOwed = debts.Where(d => d.DebtorId == user.UserId && !d.IsPaid).Sum(d => (decimal?)d.Amount) ?? 0,
                TotalOwedTo = debts.Where(d => d.CreditorId == user.UserId && !d.IsPaid).Sum(d => (decimal?)d.Amount) ?? 0,
                NetDebt = (debts.Where(d => d.CreditorId == user.UserId && !d.IsPaid).Sum(d => (decimal?)d.Amount) ?? 0)
                          - (debts.Where(d => d.DebtorId == user.UserId && !d.IsPaid).Sum(d => (decimal?)d.Amount) ?? 0)
            }).ToList();

            var pairwiseDebts = new List<PairwiseDebtViewModel>();

            foreach (var debtor in memberDebtOverview.Where(m => m.NetDebt < 0))
            {
                foreach (var creditor in memberDebtOverview.Where(m => m.NetDebt > 0))
                {
                    var debtAmount = Math.Min(-debtor.NetDebt, creditor.NetDebt);
                    if (debtAmount > 0)
                    {
                        pairwiseDebts.Add(new PairwiseDebtViewModel
                        {
                            DebtorName = debtor.UserName,
                              
                            CreditorName = creditor.UserName,
                            DebtorEmail = debtor.UserEmail,
                            DebtorPhoto = debtor.UserPhoto,
                            CreditorEmail = creditor.UserEmail,
                            CreditorPhoto = creditor.UserPhoto,
                            Amount = debtAmount,
                            DebtorId = debtor.UserId,
                            CreditorId = creditor.UserId,
                            DebtId = debts.FirstOrDefault(d => d.DebtorId == debtor.UserId && d.CreditorId == creditor.UserId && d.Amount == debtAmount)?.DebtId ?? 0
                        });

                        debtor.NetDebt += debtAmount;
                        creditor.NetDebt -= debtAmount;
                    }
                }
            }

            var memberExpenses = members.Select(m => new MemberExpenseViewModel
            {
                FullName = m.FullName,
                TotalAmount = m.TotalSpent
            }).ToList();

            var viewModel = new GroupDetailsViewModel
            {
                GroupName = group.GroupName,
                Currency = group.Currency,
                CreateDate = group.CreatedDate.HasValue ? group.CreatedDate.Value : DateTime.MinValue,
                GroupPhoto = group.GroupsPhoto ?? "/path/to/default/photo.jpg",
                Budget = group.Budget.HasValue ? group.Budget.Value : 0,
                Members = members,
                Expenses = expenses,
                MemberExpenses = memberExpenses,
                DebtSettlements = pairwiseDebts
            };

            return new Rotativa.ViewAsPdf("ExportGroupDetailsToPdf", viewModel)
            {
                FileName = $"{group.GroupName}_Details.pdf",
                PageSize = Rotativa.Options.Size.A4,
                PageOrientation = Rotativa.Options.Orientation.Portrait
            };
        }




    }
}