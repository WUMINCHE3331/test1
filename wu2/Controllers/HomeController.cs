using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using wu2.Models;

namespace wu2.Controllers
{
    public class HomeController : Controller
    {
        private wuEntities1 entities = new wuEntities1    ();
        public ActionResult QA()
        {
   

            return View();
        }
        public ActionResult Sponsor()
        {
            return View();
        }
        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }
        public ActionResult Index()
        {

            if (Session["UserID"] == null)
            {
                RedirectToAction("Login","Account");
                ViewBag.NeedsLogin = true;
            }
            else
            {
                ViewBag.NeedsLogin = false;
            }

            // 假設已登入用戶的ID存儲在Session中
            int userId = Convert.ToInt32(Session["UserID"]);

            // 1. 獲取用戶加入的群組數量
            var joinedGroupsCount = entities.GroupMembers.Count(g => g.UserId == userId);

            // 2. 計算用戶尚未結清的債務
            var unsettledAmount = entities.Debts
                .Where(d => d.DebtorId == userId && d.IsPaid == false)
                .Sum(d => (decimal?)d.Amount) ?? 0;  // 如果為null，返回0

            // 3. 計算用戶的總花費
            var totalExpenses = entities.ExpenseDetails
                .Where(e => e.UserId == userId)
                .Sum(e => (decimal?)e.Amount) ?? 0; // 如果為null，返回0

            // 使用 ViewBag 將數據傳遞到前端
            ViewBag.JoinedGroupsCount = joinedGroupsCount;
            ViewBag.UnsettledAmount = unsettledAmount;
            ViewBag.TotalExpenses = totalExpenses;
            // 查詢消費種類
            var expenseTypeData = entities.Expenses
                .Where(e => e.CreatedBy == userId)
                .GroupBy(e => e.ExpenseType)
                .Select(g => new
                {
                    ExpenseType = g.Key,
                    Count = g.Count() // 計算每個消費類型的次數
                }).ToList();

            // 查詢在群組中的消費
            var groupExpenseData = entities.Expenses
                .Where(e => e.CreatedBy == userId)
                .GroupBy(e => e.Groups.GroupName)
                .Select(g => new
                {
                    GroupName = g.Key,
                    TotalAmount = g.Sum(e => e.TotalAmount)
                }).ToList();

            // 將資料傳遞到前端
            ViewBag.ExpenseTypeData = expenseTypeData;
            ViewBag.GroupExpenseData = groupExpenseData;
            
            var currentUser = entities.Users.Find(userId);  // 获取当前用户信息
            var rootNotice = entities.Notices.FirstOrDefault(a => a.Users.Role == "Root");  // 查找由 Root 发布的公告
            // 從數據庫中取得用戶和公告信息
            var viewModel = new DashboardViewModel
            {
                Users = entities.Users.FirstOrDefault(), // 取得一個用戶的資料
                Notices = rootNotice
            };
            viewModel.Expenses = entities.Expenses
        .Where(e => e.ExpenseDetails.Any(d => d.UserId == userId))
        .Select(e => new SimplifiedExpenseViewModel
        {
            ExpenseId = e.ExpenseId,
            CreatedByName = e.Users.FullName,  // 从 Users 表获取创建者姓名
            TotalAmount = e.TotalAmount,  // 总金额 
            item =e.ExpenseItem,
            ExpenseType =e.ExpenseType,
            Createat = e.CreatedAt.Value,
            ExpenseMembers = e.ExpenseDetails.Select(d => new ExpenseMemberViewModel
            {
                FullName = d.Users.FullName,  // 成员姓名
                Photo = d.Users.ProfilePhoto  // 成员照片
            }).ToList()
        }).ToList();
            return View(viewModel);
        }
        public bool HasUnreadNotifications()
        {
            if (Session["UserID"] == null)
            {
                return false;
            }

            int userId = (int)Session["UserID"];
            return entities.Notifications.Any(n => n.UserId == userId && (!n.IsRead.HasValue || !n.IsRead.Value));

        }
    }
}