using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Core.Metadata.Edm;
using System.Data.Entity.Validation;
using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Configuration;
using System.Web.Mvc;
using wu2.Models;

namespace wu2.Controllers
{
    public class ExpenseController : Controller
    {
        private wuEntities1 entities = new wuEntities1();
        public ActionResult ExpenseList(int groupId)
        {
            var expenses = entities.Expenses.Where(e => e.GroupId == groupId).ToList();
            return PartialView("_ExpenseList", expenses);
        }
        private void SetDateTimeViewBags()
        {

            DateTime? currentTime = DateTime.Now; // 設置為可為空的 DateTime
            DateTime? maxDateTime = currentTime;  // 設置為可為空的 DateTime
            ViewBag.CurrentTime = currentTime.HasValue ? currentTime.Value.ToString("yyyy-MM-ddTHH:mm") : null;
            ViewBag.MaxDateTime = maxDateTime.HasValue ? maxDateTime.Value.ToString("yyyy-MM-ddTHH:mm") : null;
        }

        void SetBudget(int? groupId)
        {
            int nonNullableGroupId = groupId.Value; ;
            ViewBag.TotalExpenses = GetTotalGroupExpenses(nonNullableGroupId);
            var group = entities.Groups.FirstOrDefault(g => g.GroupId == nonNullableGroupId);
            ViewBag.GroupBudget = group.Budget;  // 設置預算

        }
        [HttpGet]
        public ActionResult Create(int? groupId)
        {


            SetDateTimeViewBags();
            if (Session["UserID"] == null || groupId == null)
            {
                string userAgent = Request.UserAgent;

                // 如果是 LIFF 環境
                if (userAgent != null && userAgent.Contains("Line"))
                {
                    // 取得當前的 URL，登入後需要導航回來
                    string currentUrl = Request.Url.AbsoluteUri;
                    Session["currentUrl"] = currentUrl;
              
                    // 重定向到 Home/Index 進行登入，並將原 URL 以參數方式傳遞
                    string loginRedirectUrl = Url.Action("Index", "Home", new { returnUrl = currentUrl });
                    return Redirect(loginRedirectUrl);
                }
                else
                {
                    // 如果是 Web 請求，跳轉到傳統的登入頁面
                    return RedirectToAction("Login", "Account");
                }
            }
            int nonNullableGroupId = groupId.Value; ;
            ViewBag.TotalExpenses = GetTotalGroupExpenses(nonNullableGroupId);
            var group = entities.Groups.FirstOrDefault(g => g.GroupId == nonNullableGroupId);
            if (group.Budget.HasValue)
            {
                ViewBag.GroupBudget = group.Budget.Value;  // 设置为具体的值
            }

            ViewBag.GroupId = groupId;
            ViewBag.Users = entities.GroupMembers
                                    .Where(gm => gm.GroupId == groupId)
                                    .Select(gm => gm.Users)
                                    .ToList();
            var model = new ExpenseViewModel
            {
                GroupId = groupId.Value,
            };

            ViewBag.ErrorMessage = null;
            return View(model);
        }
        [HttpPost]
        public ActionResult Create(ExpenseViewModel model, HttpPostedFileBase Photo)
        {
            SetDateTimeViewBags();

            if (model.Date > DateTime.Now)
            {
                ViewBag.ErrorMessage = "日期不能超過現在時間";
                ViewBag.GroupId = model.GroupId;
                ViewBag.Users = entities.GroupMembers
                                        .Where(gm => gm.GroupId == model.GroupId)
                                        .Select(gm => gm.Users)
                                        .ToList();
                return View(model);
            }

            if (Session["UserID"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            ViewBag.Users = entities.GroupMembers
                                    .Where(gm => gm.GroupId == model.GroupId)
                                    .Select(gm => gm.Users)
                                    .ToList();

            var group = entities.Groups.FirstOrDefault(g => g.GroupId == model.GroupId);
            if (group == null)
            {
                ViewBag.ErrorMessage = "群組不存在。";
                return View(model);
            }

            var currentUserId = (int)Session["userId"];
            model.PaidBy = model.PaidBy?.Where(p => p.IsChecked).ToList();
            model.SplitDetails = model.SplitDetails?.Where(s => s.IsChecked).ToList();

            if (model.PaidBy == null || !model.PaidBy.Any())
            {
                ViewBag.ErrorMessage = "付款人不能為空";
                return View(model);
            }

            var totalExpenses = GetTotalGroupExpenses(model.GroupId) + model.TotalAmount;
            ViewBag.TotalExpenses = totalExpenses;
            ViewBag.Budget = group.Budget;

            if (group.Budget.HasValue && totalExpenses > group.Budget.Value)
            {
                ViewBag.ErrorMessage = "總支出已超過預算!";
                return View(model);
            }

            if (model.SplitDetails == null || !model.SplitDetails.Any())
            {
                ViewBag.ErrorMessage = "分攤金額不能為空";
                return View(model);
            }

            decimal tolerance = 0.05m;
            if (Math.Abs(model.TotalAmount - model.PaidBy.Sum(p => p.Amount)) > tolerance)
            {
                ViewBag.ErrorMessage = "付款金額必須等於總金額";
                return View(model);
            }

            if (Math.Abs(model.TotalAmount - model.SplitDetails.Sum(s => s.Amount)) > tolerance)
            {
                ViewBag.ErrorMessage = "分攤金額必須等於總金額";
                return View(model);
            }

            // **為付款人分配整數金額**
            int totalPaidAmount = (int)model.TotalAmount;
            int numberOfPayers = model.PaidBy.Count;
            int basePaidAmount = totalPaidAmount / numberOfPayers;
            int paidRemainder = totalPaidAmount % numberOfPayers;

            for (int i = 0; i < numberOfPayers; i++)
            {
                model.PaidBy[i].Amount = basePaidAmount + (i < paidRemainder ? 1 : 0);
            }

            // **為分攤金額分配整數金額**
            int totalSplitAmount = (int)model.TotalAmount;
            int numberOfSplitDetails = model.SplitDetails.Count;
            int baseSplitAmount = totalSplitAmount / numberOfSplitDetails;
            int splitRemainder = totalSplitAmount % numberOfSplitDetails;

            for (int i = 0; i < numberOfSplitDetails; i++)
            {
                model.SplitDetails[i].Amount = baseSplitAmount + (i < splitRemainder ? 1 : 0);
            }

            var expense = new Expenses
            {
                GroupId = model.GroupId,
                TotalAmount = model.TotalAmount,
                ExpenseType = model.ExpenseType,
                ExpenseItem = model.ExpenseItem,
                PaymentMethod = model.PaymentMethod,
                CreatedBy = currentUserId,
                Note = model.Note,
                Photo = SavePhoto(Photo),
                CreatedAt = model.Date,
            };

            entities.Expenses.Add(expense);
            entities.SaveChanges();

            foreach (var payer in model.PaidBy)
            {
                var expensePayer = new ExpensePayers
                {
                    ExpenseId = expense.ExpenseId,
                    UserId = payer.UserId,
                    Amount = payer.Amount,
                };
                entities.ExpensePayers.Add(expensePayer);
            }

            foreach (var detail in model.SplitDetails)
            {
                var expenseDetail = new ExpenseDetails
                {
                    ExpenseId = expense.ExpenseId,
                    UserId = detail.UserId,
                    Amount = detail.Amount,
                    Note = detail.Note,
                    CreatedAt = DateTime.Now,
                };
                entities.ExpenseDetails.Add(expenseDetail);
            }

            var paidByDict = model.PaidBy.ToDictionary(p => p.UserId, p => p.Amount);
            var splitDetailsDict = model.SplitDetails.ToDictionary(s => s.UserId, s => s.Amount);

            // **發送通知給付款人**
            foreach (var payer in model.PaidBy)
            {
                var splitDetail = model.SplitDetails.FirstOrDefault(d => d.UserId == payer.UserId);
                var paidAmount = payer.Amount;
                var splitAmount = splitDetail?.Amount ?? 0;
                var balanceDue = splitAmount - paidAmount;

                var message = balanceDue > 0
                    ? $"你已支付了 {paidAmount} 元。你還需要支付 {balanceDue} 元。"
                    : balanceDue < 0
                        ? $"你已支付了 {paidAmount} 元。你應該收到退款 {Math.Abs(balanceDue)} 元。"
                        : $"你已支付了 {paidAmount} 元。你的支付已經平衡。";

                SendNotification(payer.UserId, model.GroupId, false, "ExpenseCreate", message, expense.ExpenseId);
            }

            // **發送通知給需要支付的使用者**
            foreach (var detail in model.SplitDetails)
            {
                if (!model.PaidBy.Any(p => p.UserId == detail.UserId))
                {
                    var message = $"你需要支付 {detail.Amount} 元作為分攤金額。";
                    SendNotification(detail.UserId, model.GroupId, false, "ExpenseCreate", message, expense.ExpenseId);
                }
            }
            // 計算紀錄debt
            int expenseId = expense.ExpenseId;
            var userDebts = new Dictionary<int, decimal>();

            // 計算每個用戶的淨債務
            foreach (var split in splitDetailsDict)
            {
                if (!userDebts.ContainsKey(split.Key))
                    userDebts[split.Key] = 0;
                userDebts[split.Key] -= split.Value;  // 用戶需要支付的金額
            }
            foreach (var payer in paidByDict)
            {
                if (!userDebts.ContainsKey(payer.Key))
                    userDebts[payer.Key] = 0;
                userDebts[payer.Key] += payer.Value;  // 用户已經支付的金額
            }



            // 使用臨時集合儲存帶添加的債務
            var debtsToAdd = new List<Debts>();

            // 多支付者（債權人）需要得到錢，少支付者（債務人）需要支付錢
            foreach (var userDebt in userDebts.Where(d => d.Value > 0).ToList())
            {
                foreach (var debtor in userDebts.Where(d => d.Value < 0).ToList())
                {
                    var debtAmount = Math.Min(userDebt.Value, -debtor.Value);
                    var debt = new Debts
                    {
                        GroupId = model.GroupId,
                        CreditorId = userDebt.Key,  // 多支付者
                        DebtorId = debtor.Key,      // 少支付者
                        Amount = debtAmount,
                        CreatedAt = DateTime.Now,
                        IsPaid = false,

                        ExpenseId = expense.ExpenseId,  


                    };
                    debtsToAdd.Add(debt);
                    userDebts[userDebt.Key] -= debtAmount;
                    userDebts[debtor.Key] += debtAmount;
                    if (userDebts[userDebt.Key] == 0)
                        break;
                }

                // Send notifications to payers about their balance
                foreach (var payer in model.PaidBy)
                {
                    var splitDetail = model.SplitDetails.FirstOrDefault(d => d.UserId == payer.UserId);
                    var paidAmount = payer.Amount;
                    var splitAmount = splitDetail != null ? splitDetail.Amount : 0;
                    var balanceDue = splitAmount - paidAmount;

                    var message = balanceDue > 0
                        ? $"你已支付了 {paidAmount} 元。你還需要支付 {balanceDue} 元。"
                        : balanceDue < 0
                            ? $"你已支付了 {paidAmount} 元。你應該收到退款 {Math.Abs(balanceDue)} 元。"
                            : $"你已支付了 {paidAmount} 元。你的支付已經平衡。";

                    SendNotification(payer.UserId, model.GroupId, false, "ExpenseCreate", message, expenseId);
                }

                // Send notifications to those who need to pay
                foreach (var detail in model.SplitDetails)
                {
                    if (!model.PaidBy.Any(p => p.UserId == detail.UserId))
                    {
                        var message = $"你需要支付 {detail.Amount} 元作為分攤金額。";
                        SendNotification(detail.UserId, model.GroupId, false, "ExpenseCreate", message, expenseId);
                    }
                }

            }

            // 添加所有债务到数据库
            entities.Debts.AddRange(debtsToAdd);
            LogActivity(model.GroupId, model.PaidBy.FirstOrDefault()?.UserId, "新增帳目", "新增", model.TotalAmount, expenseId);
            entities.SaveChanges();
            TempData["SuccessMessage"] = "支出創建成功！";

            if (Request.IsAjaxRequest())
            {
                return Json(new { success = true });
            }
            return RedirectToAction("Index", new { groupId = model.GroupId });
        }

        private string SavePhoto(HttpPostedFileBase photo)
        {
            if (photo == null || photo.ContentLength == 0) return null;
            var fileName = Path.GetFileName(photo.FileName);
            var path = Path.Combine(Server.MapPath("~/ExpensePhoto"), fileName);
            photo.SaveAs(path);
            return "/ExpensePhoto/" + fileName;
        }



        [HttpPost]
        public ActionResult Create2(ExpenseViewModel model, HttpPostedFileBase Photo)
        {

            SetDateTimeViewBags();
            if (model.Date > DateTime.Now)
            {
                ViewBag.ErrorMessage = "日期不能超過現在時間";
                ViewBag.GroupId = model.GroupId;  // 確保這裡包含 groupId
                SetDateTimeViewBags();
                ViewBag.Users = entities.GroupMembers
                                        .Where(gm => gm.GroupId == model.GroupId)
                                        .Select(gm => gm.Users)
                                        .ToList();
                return View(model);
            }

            if (Session["UserID"] == null)
            {
                return RedirectToAction("Login", "Account");
            }
            ViewBag.Users = entities.GroupMembers
                        .Where(gm => gm.GroupId == model.GroupId)
                        .Select(gm => gm.Users)
                        .ToList();
       
            var group = entities.Groups.FirstOrDefault(g => g.GroupId == model.GroupId);
            if (group == null)
            {
                ViewBag.ErrorMessage = "群组不存在。";
                return View(model);
            }

            // 自動勾選建立者的checkbox
            var currentUserId = (int)Session["userId"];
            Session["UserId"] = currentUserId;
            model.PaidBy = model.PaidBy?.Where(p => p.IsChecked).ToList();
            model.SplitDetails = model.SplitDetails?.Where(s => s.IsChecked).ToList();
            if (model.PaidBy == null || !model.PaidBy.Any())
            {
                ViewBag.ErrorMessage = "付款人不能為空";
                ViewBag.GroupId = model.GroupId;  // 確保這裡包含 groupId
                ViewBag.Users = entities.GroupMembers
                                        .Where(gm => gm.GroupId == model.GroupId)
                                        .Select(gm => gm.Users)
                                        .ToList(); SetDateTimeViewBags();
                SetBudget(group.GroupId);
                return View(model);
            }

          
            var totalExpenses = GetTotalGroupExpenses(model.GroupId) + model.TotalAmount;
            ViewBag.TotalExpenses = totalExpenses;
            ViewBag.Budget = group.Budget;
            if (group.Budget.HasValue && totalExpenses > group.Budget.Value)
            {
                ViewBag.GroupId = model.GroupId;  // 確保這裡包含 groupId
                ViewBag.ErrorMessage = "警告: 總支出已超過預算!"; SetDateTimeViewBags(); SetBudget(group.GroupId);
                return View(model);
            }
            if (group.Budget.HasValue && totalExpenses == group.Budget.Value)
            {
                ViewBag.GroupId = model.GroupId;  // 確保這裡包含 groupId
                ViewBag.ErrorMessage = "總支出已等於預算!";
                SetBudget(group.GroupId);
                foreach (var i in ViewBag.Users)
                    SendNotification(i.UserId, model.GroupId, false, "Budget", "群組支出已滿");

            }

            if (model.SplitDetails == null || !model.SplitDetails.Any())
            {
                ViewBag.ErrorMessage = "分攤金額不能為空"; SetBudget(group.GroupId);
                ViewBag.Users = entities.GroupMembers
                                        .Where(gm => gm.GroupId == model.GroupId)
                                        .Select(gm => gm.Users)
                                        .ToList(); SetDateTimeViewBags();
                ViewBag.GroupId = model.GroupId;  // 確保這裡包含 groupId
                return View(model);
            }

            decimal tolerance = 0.05m; // 容差值
            if (Math.Abs(model.TotalAmount - model.PaidBy.Sum(p => p.Amount)) > tolerance)
            {
                SetBudget(group.GroupId);
                ViewBag.ErrorMessage = "付款金額必須等於總金額";
                ViewBag.GroupId = model.GroupId;
                ViewBag.Users = entities.GroupMembers
                                        .Where(gm => gm.GroupId == model.GroupId)
                                        .Select(gm => gm.Users)
                                        .ToList();
                SetDateTimeViewBags();
                return View(model);
            }

            if (Math.Abs(model.TotalAmount - model.SplitDetails.Sum(s => s.Amount)) > tolerance)
            {
                SetBudget(group.GroupId);
                ViewBag.ErrorMessage = "分攤金額必須等於總金額";
                ViewBag.GroupId = model.GroupId;  // 確保這裡包含 groupId
                ViewBag.Users = entities.GroupMembers
                                        .Where(gm => gm.GroupId == model.GroupId)
                                        .Select(gm => gm.Users)
                                        .ToList();
                SetDateTimeViewBags();
                return View(model);
            }

            if (string.IsNullOrEmpty(model.ExpenseType) ||
                string.IsNullOrEmpty(model.ExpenseItem) || string.IsNullOrEmpty(model.PaymentMethod) ||
                 model.Date == null)
            {
                SetBudget(group.GroupId);
                ViewBag.GroupId = model.GroupId;  // 確保這裡包含 groupId
                ViewBag.ErrorMessage = "所有必填字段都需要填寫";
                ViewBag.Users = entities.GroupMembers
                                        .Where(gm => gm.GroupId == model.GroupId)
                                        .Select(gm => gm.Users)
                                        .ToList(); SetDateTimeViewBags();
                return View(model);
            }
            // 假設預設的 expenseTypes 包含所有系統內建的選項
            var expenseTypes = new List<string> { "餐飲", "交通", "娛樂", "購物", "其他" };

            // 判斷是否是自訂輸入的 ExpenseType
            var isCustomExpenseType = !expenseTypes.Contains(model.ExpenseType) && !string.IsNullOrEmpty(model.CustomExpenseType);
            string photoPath = null;
            if (Photo != null && Photo.ContentLength > 0)
            {
                var fileName = Path.GetFileName(Photo.FileName);
                var path = Path.Combine(Server.MapPath("~/ExpensePhoto"), fileName);
                Photo.SaveAs(path);
                photoPath = "/ExpensePhoto/" + fileName;
            }
            var expenseType = model.ExpenseType == "自行輸入" && !string.IsNullOrEmpty(Request["CustomExpenseType"])
      ? Request["CustomExpenseType"]
      : model.ExpenseType;
            var expense = new Expenses
            {
                GroupId = model.GroupId,

                TotalAmount = model.TotalAmount,
                ExpenseType = isCustomExpenseType ? model.CustomExpenseType : model.ExpenseType, // 儲存自訂輸入的值或預設選項
                ExpenseItem = model.ExpenseItem,
                PaymentMethod = model.PaymentMethod,
                CreatedBy = currentUserId,
                Note = model.Note,
                Photo = photoPath,
                CreatedAt = model.Date,

            };
            var existingExpense = entities.Expenses.FirstOrDefault(e => e.GroupId == model.GroupId &&
                                                            e.TotalAmount == model.TotalAmount &&
                                                            e.ExpenseItem == model.ExpenseItem &&
                                                            e.CreatedAt == model.Date);
            if (existingExpense != null)
            {
                ViewBag.ErrorMessage = "相同紀錄已存在";
                return View(model);
            }

            entities.Expenses.Add(expense);
            entities.SaveChanges();
            int expenseId = expense.ExpenseId;
            //紀錄付款人
            foreach (var payer in model.PaidBy)
            {
                var expensePayer = new ExpensePayers
                {
                    ExpenseId = expense.ExpenseId,
                    UserId = payer.UserId,
                    Amount = payer.Amount,

                };

                entities.ExpensePayers.Add(expensePayer);
            }

            //分攤金額 
            foreach (var detail in model.SplitDetails)
            {
                var expenseDetail = new ExpenseDetails
                {
                    ExpenseId = expense.ExpenseId,
                    UserId = detail.UserId,
                    Amount = detail.Amount,
                    Note = detail.Note,
                    CreatedAt = DateTime.Now,

                };

                entities.ExpenseDetails.Add(expenseDetail);
            }


         
            var paidByDict = model.PaidBy.ToDictionary(p => p.UserId, p => p.Amount);
            var splitDetailsDict = model.SplitDetails.ToDictionary(s => s.UserId, s => s.Amount);

            var userDebts = new Dictionary<int, decimal>();

        
            foreach (var split in splitDetailsDict)
            {
                if (!userDebts.ContainsKey(split.Key))
                    userDebts[split.Key] = 0;
                userDebts[split.Key] -= split.Value;  // 用户需要支付的金额
            }
            foreach (var payer in paidByDict)
            {
                if (!userDebts.ContainsKey(payer.Key))
                    userDebts[payer.Key] = 0;
                userDebts[payer.Key] += payer.Value;  // 用户已经支付的金额
            }
            var debtsToAdd = new List<Debts>();

            foreach (var userDebt in userDebts.Where(d => d.Value > 0).ToList())
            {
                foreach (var debtor in userDebts.Where(d => d.Value < 0).ToList())
                {
                    var debtAmount = Math.Min(userDebt.Value, -debtor.Value);
                    var debt = new Debts
                    {
                        GroupId = model.GroupId,
                        CreditorId = userDebt.Key,  // 多支付者
                        DebtorId = debtor.Key,      // 少支付者
                        Amount = debtAmount,
                        CreatedAt = DateTime.Now,
                        IsPaid = false,

                        ExpenseId = expense.ExpenseId, 


                    };
                    debtsToAdd.Add(debt);
                    userDebts[userDebt.Key] -= debtAmount;
                    userDebts[debtor.Key] += debtAmount;
                    if (userDebts[userDebt.Key] == 0)
                        break;
                }
                // Calculate total amount paid
                var totalPaidAmount = model.PaidBy.Sum(p => p.Amount);

                // Calculate total split amount
                var totalSplitAmount = model.SplitDetails.Sum(d => d.Amount);



                // Send notifications to payers about their balance
                foreach (var payer in model.PaidBy)
                {
                    var splitDetail = model.SplitDetails.FirstOrDefault(d => d.UserId == payer.UserId);
                    var paidAmount = payer.Amount;
                    var splitAmount = splitDetail != null ? splitDetail.Amount : 0;
                    var balanceDue = splitAmount - paidAmount;

                    var message = balanceDue > 0
                        ? $"你已支付了 {paidAmount} 元。你還需要支付 {balanceDue} 元。"
                        : balanceDue < 0
                            ? $"你已支付了 {paidAmount} 元。你應該收到退款 {Math.Abs(balanceDue)} 元。"
                            : $"你已支付了 {paidAmount} 元。你的支付已經平衡。";

                    SendNotification(payer.UserId, model.GroupId, false, "ExpenseCreate", message, expenseId);
                }

                // Send notifications to those who need to pay
                foreach (var detail in model.SplitDetails)
                {
                    if (!model.PaidBy.Any(p => p.UserId == detail.UserId))
                    {
                        var message = $"你需要支付 {detail.Amount} 元作為分攤金額。";
                        SendNotification(detail.UserId, model.GroupId, false, "ExpenseCreate", message, expenseId);
                    }
                }

            }

            
            entities.Debts.AddRange(debtsToAdd);
            LogActivity(model.GroupId, model.PaidBy.FirstOrDefault()?.UserId, "新增帳目", "新增", model.TotalAmount, expenseId);
            entities.SaveChanges();

            TempData["SuccessMessage"] = "支出創建成功！";
            if (Request.IsAjaxRequest())
            {
              
                return Json(new { success = true });
            }
            {
                return RedirectToAction("Index", new { groupId = model.GroupId });
            }
        }
        private decimal GetTotalGroupExpenses(int groupId)
        {
            var totalExpenses = entities.Expenses
                                        .Where(e => e.GroupId == groupId)
                                        .Sum(e => (decimal?)e.TotalAmount) ?? 0;

            return totalExpenses;
        }


        [HttpGet]
        public ActionResult Index(int? groupId)
        {
            if (Session["UserID"] == null || groupId == null)
            {
                return RedirectToAction("Login", "Account");
            }
            int nonNullableGroupId = groupId.Value;
            var expenses = entities.Expenses
                                   .Where(e => e.GroupId == groupId).OrderByDescending(e => e.ExpenseId)

                                   .ToList();

            ViewBag.GroupId = groupId;
            ViewBag.TotalExpenses = GetTotalGroupExpenses(nonNullableGroupId);

            return View(expenses);
        }

        //支出details
        [HttpGet]
        public ActionResult Details(int id)
        {
            var expense = entities.Expenses.Include("ExpenseDetails.Users").FirstOrDefault(e => e.ExpenseId == id);
            if (expense == null)
            {
                TempData["ErrorMessage"] = "支出已被刪除或是不存在。";
                return RedirectToAction("Notifications", "Group");
            }
            return View(expense);
        }
        private void SetViewBagUsers(int groupId)
        {
            ViewBag.Users = entities.GroupMembers
                                    .Where(gm => gm.GroupId == groupId)
                                    .Select(gm => gm.Users)
                                    .ToList();
        }
        [HttpGet]
        public ActionResult Edit(int? id)
        {

            if (Session["UserID"] == null || id == null)
            {
                return RedirectToAction("Login", "Account");
            }


            var currentUserId = (int)Session["UserID"];

            var expense = entities.Expenses
                                  .Include(e => e.ExpensePayers)
                                  .Include(e => e.ExpenseDetails)
                                  .FirstOrDefault(e => e.ExpenseId == id);
            var group = entities.Groups.FirstOrDefault(g => g.GroupId == expense.GroupId);
            if (group == null)
            {
                TempData["ErrorMessage"] = "群组不存在";
                return RedirectToAction("Index");
            }
            SetBudget(group.GroupId);
            // Store data in TempData
            TempData["TotalExpenses"] = GetTotalGroupExpenses(expense.GroupId);
            TempData["GroupBudget"] = group.Budget;
            if (expense == null)
            {
                ViewBag.ErrorMessage = "支出紀錄不存在";
                return RedirectToAction("Index");
            }
            if (expense.CreatedBy != currentUserId)
            {
                TempData["ErrorMessage"] = "你沒有權限編輯此帳目";
                return RedirectToAction("Index", new { groupId = expense.GroupId });
            }
            // 假設 expenseTypes 是一個包含所有預設選項的集合
            var expenseTypes = new List<string> { "餐飲", "交通", "娛樂", "購物", "其他" };

            // 判斷是否是自訂輸入的 ExpenseType
            var isCustomExpenseType = !expenseTypes.Contains(expense.ExpenseType) && !string.IsNullOrEmpty(expense.ExpenseType);

            var model = new ExpenseViewModel
            {
                ExpenseId = expense.ExpenseId,
                GroupId = expense.GroupId,
                TotalAmount = expense.TotalAmount,
                ExpenseType = isCustomExpenseType ? "自行輸入" : expense.ExpenseType,
                ExpenseItem = expense.ExpenseItem,
                PaymentMethod = expense.PaymentMethod,
                Note = expense.Note,
                CustomExpenseType = isCustomExpenseType ? expense.ExpenseType : null,
                IsCustomExpenseType = isCustomExpenseType,
                Date = expense.CreatedAt.Value,
                PaidBy = expense.ExpensePayers.Select(p => new ExpensePayerViewModel
                {
                    UserId = p.UserId,
                    Amount = p.Amount,
                    IsChecked = true
                }).ToList(),
                SplitDetails = expense.ExpenseDetails.Select(s => new ExpenseDetailViewModel
                {
                    UserId = s.UserId,
                    Amount = s.Amount,
                    IsChecked = true,
                    Note = s.Note
                }).ToList()
            };
            ViewBag.GroupId = model.GroupId;
            ViewBag.Users = entities.GroupMembers
                                    .Where(gm => gm.GroupId == expense.GroupId)
                                    .Select(gm => gm.Users)
                                    .ToList();
            return View(model);
        }
        [HttpPost]
        public ActionResult Edit(ExpenseViewModel model, HttpPostedFileBase Photo)
        {
            if (Session["UserID"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            ViewBag.Users = entities.GroupMembers
                                    .Where(gm => gm.GroupId == model.GroupId)
                                    .Select(gm => gm.Users)
                                    .ToList();
            ViewBag.GroupId = model.GroupId;

            if (ModelState.IsValid)
            {
                var expense = entities.Expenses
                                      .Include(e => e.ExpensePayers)
                                      .Include(e => e.ExpenseDetails)
                                      .FirstOrDefault(e => e.ExpenseId == model.ExpenseId);

                if (expense == null)
                {
                    ViewBag.ErrorMessage = "支出记录不存在。";
                    return View(model);
                }

                var group = entities.Groups.FirstOrDefault(g => g.GroupId == model.GroupId);

                if (model.Date > DateTime.Now)
                {
                    ViewBag.ErrorMessage = "日期不能超過現在時間";
                    return View(model);
                }

                if (group == null)
                {
                    ViewBag.ErrorMessage = "群組不存在。";
                    return View(model);
                }

                model.PaidBy = model.PaidBy?.Where(p => p.IsChecked).ToList();
                model.SplitDetails = model.SplitDetails?.Where(s => s.IsChecked).ToList();

                if (model.PaidBy == null || !model.PaidBy.Any())
                {
                    ViewBag.ErrorMessage = "付款人不能為空";
                    return View(model);
                }

                if (model.SplitDetails == null || !model.SplitDetails.Any())
                {
                    ViewBag.ErrorMessage = "分攤金額不能為空";
                    return View(model);
                }

                decimal tolerance = 0.01m;
                if (Math.Abs(model.TotalAmount - model.PaidBy.Sum(p => p.Amount)) > tolerance)
                {
                    ViewBag.ErrorMessage = "付款金額必須等於總金額";
                    return View(model);
                }

                if (Math.Abs(model.TotalAmount - model.SplitDetails.Sum(s => s.Amount)) > tolerance)
                {
                    ViewBag.ErrorMessage = "分攤金額必須等於總金額";
                    return View(model);
                }

                // **為付款人分配整數金額**
                int totalPaidAmount = (int)model.TotalAmount;
                int numberOfPayers = model.PaidBy.Count;
                int basePaidAmount = totalPaidAmount / numberOfPayers;
                int paidRemainder = totalPaidAmount % numberOfPayers;

                for (int i = 0; i < numberOfPayers; i++)
                {
                    model.PaidBy[i].Amount = basePaidAmount + (i < paidRemainder ? 1 : 0);
                }

                // **為分攤金額分配整數金額**
                int totalSplitAmount = (int)model.TotalAmount;
                int numberOfSplitDetails = model.SplitDetails.Count;
                int baseSplitAmount = totalSplitAmount / numberOfSplitDetails;
                int splitRemainder = totalSplitAmount % numberOfSplitDetails;

                for (int i = 0; i < numberOfSplitDetails; i++)
                {
                    model.SplitDetails[i].Amount = baseSplitAmount + (i < splitRemainder ? 1 : 0);
                }

                // 更新支出詳細資料
                expense.TotalAmount = model.TotalAmount;
                expense.ExpenseType = model.ExpenseType;
                expense.ExpenseItem = model.ExpenseItem;
                expense.PaymentMethod = model.PaymentMethod;
                expense.Note = model.Note;
                expense.CreatedAt = model.Date;
                expense.LastTime = DateTime.Now;

                // 更新照片
                if (Photo != null && Photo.ContentLength > 0)
                {
                    var fileName = Path.GetFileName(Photo.FileName);
                    var path = Path.Combine(Server.MapPath("~/ExpensePhoto"), fileName);
                    Photo.SaveAs(path);
                    expense.Photo = "/ExpensePhoto/" + fileName;
                }

                // 刪除舊的付款人和分攤金額
                entities.ExpensePayers.RemoveRange(expense.ExpensePayers);
                entities.ExpenseDetails.RemoveRange(expense.ExpenseDetails);

                // 添加新的付款人和分攤金額
                expense.ExpensePayers = model.PaidBy.Select(p => new ExpensePayers
                {
                    UserId = p.UserId,
                    Amount = p.Amount,
                    ExpenseId = expense.ExpenseId
                }).ToList();

                expense.ExpenseDetails = model.SplitDetails.Select(s => new ExpenseDetails
                {
                    UserId = s.UserId,
                    Amount = s.Amount,
                    Note = s.Note,
                    ExpenseId = expense.ExpenseId,
                    CreatedAt = DateTime.Now
                }).ToList();

                // 刪除舊的債務並新增新的債務
                var oldDebts = entities.Debts.Where(d => d.ExpenseId == expense.ExpenseId).ToList();
                entities.Debts.RemoveRange(oldDebts);

                var userDebts = new Dictionary<int, decimal>();

                foreach (var split in model.SplitDetails)
                {
                    if (!userDebts.ContainsKey(split.UserId))
                        userDebts[split.UserId] = 0;
                    userDebts[split.UserId] -= split.Amount;
                }

                foreach (var payer in model.PaidBy)
                {
                    if (!userDebts.ContainsKey(payer.UserId))
                        userDebts[payer.UserId] = 0;
                    userDebts[payer.UserId] += payer.Amount;
                }

                var debtsToAdd = new List<Debts>();

                // 使用臨時清單存放正負債務的 UserId 和金額，避免直接修改 Dictionary
                var positiveDebts = userDebts.Where(d => d.Value > 0).ToList();
                var negativeDebts = userDebts.Where(d => d.Value < 0).ToList();

                foreach (var creditor in positiveDebts)
                {
                    foreach (var debtor in negativeDebts)
                    {
                        if (userDebts[creditor.Key] == 0) break; // 如果債權人的債務已平衡，停止

                        var debtAmount = Math.Min(userDebts[creditor.Key], -userDebts[debtor.Key]);
                        debtsToAdd.Add(new Debts
                        {
                            GroupId = model.GroupId,
                            CreditorId = creditor.Key,
                            DebtorId = debtor.Key,
                            Amount = debtAmount,
                            CreatedAt = DateTime.Now,
                            IsPaid = false,
                            ExpenseId = expense.ExpenseId
                        });

                        // 使用 Dictionary 的索引子語法來更新金額
                        userDebts[creditor.Key] -= debtAmount;
                        userDebts[debtor.Key] += debtAmount;
                    }
                }

                // 將新增的債務儲存到資料庫
                entities.Debts.AddRange(debtsToAdd);

                entities.SaveChanges();

                // 取得當前用戶名
                var userId = (int)Session["UserID"];
                var user = entities.Users.FirstOrDefault(u => u.UserId == userId);
                var userName = user?.FullName ?? "某用戶";

                // 發送通知給所有相關人員
                var expenseId = expense.ExpenseId;
                foreach (var payer in model.PaidBy)
                {
                    var splitDetail = model.SplitDetails.FirstOrDefault(d => d.UserId == payer.UserId);
                    var paidAmount = payer.Amount;
                    var splitAmount = splitDetail?.Amount ?? 0;
                    var balanceDue = splitAmount - paidAmount;

                    var message = balanceDue > 0
                        ? $"你已支付了 {paidAmount} 元。你還需要支付 {balanceDue} 元。(編輯by {userName})"
                        : balanceDue < 0
                            ? $"你已支付了 {paidAmount} 元。你應該收到退款 {Math.Abs(balanceDue)} 元。(編輯by {userName})"
                            : $"你已支付了 {paidAmount} 元。你的支付已經平衡。(編輯by {userName})";

                    SendNotification(payer.UserId, model.GroupId, false, "ExpenseEdit", message, expenseId);
                }

                foreach (var detail in model.SplitDetails)
                {
                    if (!model.PaidBy.Any(p => p.UserId == detail.UserId))
                    {
                        var message = $"你需要支付 {detail.Amount} 元作為分攤金額。(編輯by {userName})";
                        SendNotification(detail.UserId, model.GroupId, false, "ExpenseEdit", message, expenseId);
                    }
                }

                LogActivity(model.GroupId, userId, "編輯帳目", "編輯", model.TotalAmount, expenseId);

                TempData["SuccessMessage"] = "支出編輯成功！";
                return RedirectToAction("Index", new { groupId = model.GroupId });
            }

            // 如果驗證失敗，返回原頁面
            return View(model);
        }


        [HttpPost]
        public ActionResult Edit2(ExpenseViewModel model, HttpPostedFileBase Photo)
        {
            if (Session["UserID"] == null)
            {
                return RedirectToAction("Login", "Account");
            }
            ViewBag.Users = entities.GroupMembers
                                    .Where(gm => gm.GroupId == model.GroupId)
                                    .Select(gm => gm.Users)
                                    .ToList();
            ViewBag.GroupId = model.GroupId;
            if (ModelState.IsValid)
            {
                var expense = entities.Expenses
                                  .Include(e => e.ExpensePayers)
                                  .Include(e => e.ExpenseDetails)
                                      .FirstOrDefault(e => e.ExpenseId == model.ExpenseId);

                if (expense == null)
                {
                    ViewBag.ErrorMessage = "支出记录不存在。";
                    return View(model);
                }

                var group = entities.Groups.FirstOrDefault(g => g.GroupId == model.GroupId);
                // 验证逻辑
                if (model.Date > DateTime.Now)
                {
                    ViewBag.ErrorMessage = "日期不能超過現在時間";
                    ViewBag.GroupId = model.GroupId;
                    SetDateTimeViewBags();
                    SetBudget(group.GroupId);
                    ViewBag.Users = entities.GroupMembers
                                            .Where(gm => gm.GroupId == model.GroupId)
                                            .Select(gm => gm.Users)
                                            .ToList();
                    return View(model);
                }

                if (group == null)
                {
                    ViewBag.ErrorMessage = "群组不存在。";
                    return View(model);
                }

                model.PaidBy = model.PaidBy?.Where(p => p.IsChecked).ToList();
                model.SplitDetails = model.SplitDetails?.Where(s => s.IsChecked).ToList();

                if (model.PaidBy == null || !model.PaidBy.Any())
                {
                    SetBudget(group.GroupId);
                    ViewBag.ErrorMessage = "付款人不能為空";
                    ViewBag.GroupId = model.GroupId;
                    ViewBag.Users = entities.GroupMembers
                                            .Where(gm => gm.GroupId == model.GroupId)
                                            .Select(gm => gm.Users)
                                            .ToList();

                    SetDateTimeViewBags();
                    return View(model);
                }

                var totalExpenses = GetTotalGroupExpenses(model.GroupId) + model.TotalAmount;
                ViewBag.TotalExpenses = totalExpenses;
                ViewBag.Budget = group.Budget;
                if (group == null || model == null)
                {
                    ViewBag.ErrorMessage = "警告: Group 或 Model 為 Null!";
                    return View(model); // 回傳錯誤頁面
                }
                // 檢查 Budget 是否有值並進行操作
                if (group.Budget.HasValue)
                {
                    // 確保 totalExpenses 大於 Budget 再進行操作
                    if (totalExpenses > group.Budget.Value)
                    {
                        SetBudget(group.GroupId); // 設定預算
                        ViewBag.GroupId = model.GroupId; // 確保 GroupId 被設置
                        ViewBag.ErrorMessage = "警告: 總支出已超過預算!";
                        return View(model); // 回傳視圖
                    }

                    // 當總支出等於預算時
                    if (totalExpenses == group.Budget.Value)
                    {
                        ViewBag.GroupId = model.GroupId; // 設置 ViewBag.GroupId
                        ViewBag.ErrorMessage = "總支出已等於預算!";
                        SetBudget(group.GroupId); // 更新預算
                        SetDateTimeViewBags(); // 設置其他 ViewBag 變數

                    }
                }
           
                if (model.SplitDetails == null || !model.SplitDetails.Any())
                {
                    ViewBag.ErrorMessage = "分攤金額不能為空";
                    SetBudget(group.GroupId);
                    ViewBag.Users = entities.GroupMembers
                                            .Where(gm => gm.GroupId == model.GroupId)
                                            .Select(gm => gm.Users)
                                            .ToList();
                    SetDateTimeViewBags();
                    ViewBag.GroupId = model.GroupId;
                    return View(model);
                }

                decimal tolerance = 0.01m; // 容差值
                if (Math.Abs(model.TotalAmount - model.PaidBy.Sum(p => p.Amount)) > tolerance)
                {
                    ViewBag.ErrorMessage = "付款金額必須等於總金額";
                    ViewBag.GroupId = model.GroupId;
                    SetBudget(group.GroupId);
                    ViewBag.Users = entities.GroupMembers
                                            .Where(gm => gm.GroupId == model.GroupId)
                                            .Select(gm => gm.Users)
                                            .ToList();
                    SetDateTimeViewBags();
                    return View(model);
                }

                if (Math.Abs(model.TotalAmount - model.SplitDetails.Sum(s => s.Amount)) > tolerance)
                {
                    SetBudget(group.GroupId);
                    ViewBag.ErrorMessage = "分攤金額必須等於總金額";
                    ViewBag.GroupId = model.GroupId;
                    ViewBag.Users = entities.GroupMembers
                                            .Where(gm => gm.GroupId == model.GroupId)
                                            .Select(gm => gm.Users)
                                            .ToList();
                    SetDateTimeViewBags();
                    return View(model);
                }

                if (string.IsNullOrEmpty(model.ExpenseType) ||
                    string.IsNullOrEmpty(model.ExpenseItem) ||
                    string.IsNullOrEmpty(model.PaymentMethod) ||
                    model.Date == null)
                {
                    SetBudget(group.GroupId);
                    ViewBag.GroupId = model.GroupId;
                    ViewBag.ErrorMessage = "所有必填字段都需要填寫";
                    ViewBag.Users = entities.GroupMembers
                                            .Where(gm => gm.GroupId == model.GroupId)
                                            .Select(gm => gm.Users)
                                            .ToList();
                    SetDateTimeViewBags();
                    return View(model);
                }
                var expenseType = model.ExpenseType == "自行輸入" && !string.IsNullOrEmpty(Request["CustomExpenseType"])
   ? Request["CustomExpenseType"]
   : model.ExpenseType;
                // 更新支出的各个字段
                expense.TotalAmount = model.TotalAmount;
                expense.ExpenseType = expenseType;
                expense.ExpenseItem = model.ExpenseItem;
                expense.PaymentMethod = model.PaymentMethod;
                expense.Note = model.Note;
                expense.CreatedAt = model.Date;
                expense.LastTime = DateTime.Now;

                if (Photo != null && Photo.ContentLength > 0)
                {
                    var fileName = Path.GetFileName(Photo.FileName);
                    var path = Path.Combine(Server.MapPath("~/ExpensePhoto"), fileName);
                    Photo.SaveAs(path);
                    expense.Photo = "/ExpensePhoto/" + fileName;
                }

        

                entities.ExpensePayers.RemoveRange(expense.ExpensePayers);
                entities.ExpenseDetails.RemoveRange(expense.ExpenseDetails);
                // 刪除舊的 Debts
                var oldDebts = entities.Debts.Where(d => d.ExpenseId == expense.ExpenseId).ToList();
                entities.Debts.RemoveRange(oldDebts);
           
                expense.ExpensePayers = model.PaidBy.Select(p => new ExpensePayers
                {
                    UserId = p.UserId,
                    Amount = p.Amount,
                    ExpenseId = expense.ExpenseId
                }).ToList();

                expense.ExpenseDetails = model.SplitDetails.Select(s => new ExpenseDetails
                {
                    UserId = s.UserId,
                    Amount = s.Amount,
                    Note = s.Note,
                    ExpenseId = expense.ExpenseId,
                    CreatedAt = DateTime.Now
                }).ToList();

                // 保存更改
                entities.SaveChanges();

         
                var paidByDict = model.PaidBy.ToDictionary(p => p.UserId, p => p.Amount);
                var splitDetailsDict = model.SplitDetails.ToDictionary(s => s.UserId, s => s.Amount);

                var userDebts = new Dictionary<int, decimal>();

                foreach (var split in splitDetailsDict)
                {
                    if (!userDebts.ContainsKey(split.Key))
                        userDebts[split.Key] = 0;
                    userDebts[split.Key] -= split.Value;
                }

                foreach (var payer in paidByDict)
                {
                    if (!userDebts.ContainsKey(payer.Key))
                        userDebts[payer.Key] = 0;
                    userDebts[payer.Key] += payer.Value;
                }

                var debtsToAdd = new List<Debts>();

                foreach (var userDebt in userDebts.Where(d => d.Value > 0).ToList())
                {
                    foreach (var debtor in userDebts.Where(d => d.Value < 0).ToList())
                    {
                        var debtAmount = Math.Min(userDebt.Value, -debtor.Value);
                        var debt = new Debts
                        {
                            GroupId = model.GroupId,
                            CreditorId = userDebt.Key,
                            DebtorId = debtor.Key,
                            Amount = debtAmount,
                            CreatedAt = DateTime.Now,
                            IsPaid = false,
                            ExpenseId = expense.ExpenseId
                        };
                        debtsToAdd.Add(debt);
                        userDebts[userDebt.Key] -= debtAmount;
                        userDebts[debtor.Key] += debtAmount;
                        if (userDebts[userDebt.Key] == 0)
                            break;
                    }
                }

                entities.Debts.AddRange(debtsToAdd);
                var userId = (int)Session["UserID"];
                var user = entities.Users.FirstOrDefault(u => u.UserId == userId);
                var userName = user != null ? user.FullName : "某用戶";
       
                var expenseId = expense.ExpenseId;
                foreach (var payer in model.PaidBy)
                {
                    var splitDetail = model.SplitDetails.FirstOrDefault(d => d.UserId == payer.UserId);
                    var paidAmount = payer.Amount;
                    var splitAmount = splitDetail != null ? splitDetail.Amount : 0;
                    var balanceDue = splitAmount - paidAmount;

                    var message = balanceDue > 0
                        ? $"你已支付了 {paidAmount} 元。你還需要支付 {balanceDue} 元。(編輯by{userName})"
                        : balanceDue < 0
                            ? $"你已支付了 {paidAmount} 元。你應該收到退款 {Math.Abs(balanceDue)} 元。(編輯by{userName})"
                            : $"你已支付了 {paidAmount} 元。你的支付已經平衡。(編輯by{userName})";

                    SendNotification(payer.UserId, model.GroupId, false, "ExpenseEdit", message, expenseId);
                }

                foreach (var detail in model.SplitDetails)
                {
                    if (!model.PaidBy.Any(p => p.UserId == detail.UserId))
                    {
                        var message = $"你需要支付 {detail.Amount} 元作為分攤金額。(編輯by{userName})";
                        SendNotification(detail.UserId, model.GroupId, false, "ExpenseEdit", message, expenseId);
                    }
                }

      
                LogActivity(model.GroupId, model.PaidBy.FirstOrDefault()?.UserId, "編輯帳目", "編輯", model.TotalAmount, expenseId);

                entities.SaveChanges();

                TempData["SuccessMessage"] = "支出編輯成功！";
                return RedirectToAction("Index", new { groupId = model.GroupId });
            }

            
            ViewBag.Users = entities.GroupMembers
                                    .Where(gm => gm.GroupId == model.GroupId)
                                    .Select(gm => gm.Users)
                                    .ToList();
            return View(model);
        }
        public void SendNotification(int userId, int groupId, bool isRead, string notificationType, string message, int? relatedExpenseId = null)
        {
            var notification = new Notifications
            {
                UserId = userId,
                GroupId = groupId,
                NotificationType = notificationType,
                IsRead = isRead,
                Message = message,
                RelatedExpenseId = relatedExpenseId,
                Date = DateTime.Now
            };
            entities.Notifications.Add(notification);
            entities.SaveChanges();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteExpense(int expenseId)
        {
            if (Session["UserID"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var expense = entities.Expenses
                                  .Include(e => e.ExpensePayers)
                                  .Include(e => e.ExpenseDetails)
                                  .FirstOrDefault(e => e.ExpenseId == expenseId);

            if (expense == null)
            {
                TempData["ErrorMessage"] = "找不到相關的支出記錄";
                return RedirectToAction("Index");
            }
            var currentUserId = (int)Session["UserID"];


            if (expense.CreatedBy != currentUserId)
            {
                TempData["ErrorMessage"] = "你無權限刪除此帳目";
                return RedirectToAction("Index", new { groupId = expense.GroupId });
            }
            bool hasFullPermission = expense.CreatedBy != currentUserId;
            var userId = (int)Session["UserID"];
            var user = entities.Users.FirstOrDefault(u => u.UserId == userId);
            var userName = user != null ? user.FullName : "某用户";
            var groupId = expense.GroupId;

        
            var members = expense.ExpensePayers.Select(p => p.UserId)
                          .Union(expense.ExpenseDetails.Select(d => d.UserId))
                          .Distinct()
                          .ToList();

            foreach (var memberId in members)
            {
                var message = $"支出金額{expense.TotalAmount} 已被 {userName} 刪除。";
                SendNotification(memberId, groupId, false, "ExpenseDeleted", message, expenseId);
            }

   
            LogActivity(groupId, userId, "刪除帳目", "刪除", expense.TotalAmount, expenseId);

       
            var activityLogs = entities.ActivityLogs.Where(al => al.ExpenseId == expenseId).ToList();
            entities.ActivityLogs.RemoveRange(activityLogs);


            entities.ExpensePayers.RemoveRange(expense.ExpensePayers);
            entities.ExpenseDetails.RemoveRange(expense.ExpenseDetails);

       
            var existingDebts = entities.Debts.Where(d => d.ExpenseId == expenseId).ToList();
            entities.Debts.RemoveRange(existingDebts);

          
            entities.Expenses.Remove(expense);

            entities.SaveChanges();

            TempData["SuccessMessage"] = "支出刪除成功！";
            return RedirectToAction("Index", new { groupId = groupId });
        }

        [HttpGet]
        public ActionResult DebtList(int? groupId)
        {
            if (Session["UserID"] == null || groupId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var debts = entities.Debts
             .Where(d => d.GroupId == groupId)
                 .Select(d => new DebtViewModel
                 {
                     DebtId = d.DebtId,
                     GroupId = d.GroupId,
                     CreditorName = entities.Users.FirstOrDefault(u => u.UserId == d.CreditorId).FullName,
                     DebtorName = entities.Users.FirstOrDefault(u => u.UserId == d.DebtorId).FullName,
                     Amount = d.Amount,
                     IsPaid = d.IsPaid,
                     CreatedAt = d.CreatedAt,
                     ExpenseId = d.Expenses.ExpenseId,// 包含 Expense 的信息
                     UpdatedAt = d.UpdatedAt,
                     IsConfirmed = d.IsConfirmed,
                 })
                  .OrderByDescending(gm => gm.CreatedAt).ToList();

            ViewBag.GroupId = groupId;
            return View(debts);
        }

        [HttpPost]
        public ActionResult MarkAsPaid(int id, int groupId)
        {
            var debt = entities.Debts.FirstOrDefault(d => d.DebtId == id);
            if (debt == null)
            {
                return HttpNotFound();
            }

            debt.IsPaid = true;
            debt.UpdatedAt = DateTime.Now;

            entities.SaveChanges();

            return RedirectToAction("DebtList", new { groupId = groupId });
        }

        [HttpGet]
        public ActionResult MemberDebtOverview(int? groupId)
        {
            if (Session["UserID"] == null || groupId == null)
            {
                return RedirectToAction("Login", "Account");
            }

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
            Session["MemberDebtOverview"] = memberDebtOverview;
            ViewBag.GroupId = groupId;
            return View(memberDebtOverview);
        }


        private void SendNotification(int userId, int groupId, bool IsRead, string type, string message)
        {
            var notification = new Notifications
            {
                UserId = userId,
                GroupId = groupId,
                NotificationType = type,
                Message = message,
                IsRead = IsRead,
                Date = DateTime.Now
            };
            entities.Notifications.Add(notification);
            entities.SaveChanges();
        }

        private void LogActivity(int groupId, int? userId, string ActivityType, string action, decimal totalAmount, int expenseId)
        {
            if (!userId.HasValue || !entities.Users.Any(u => u.UserId == userId.Value))
                return;

            var activityLog = new ActivityLogs
            {
                GroupId = groupId,
                UserId = userId.Value,
                ActivityType = ActivityType,
                ActivityDetails = $"{action}了一筆總金額為{totalAmount}的支出。",
                Date = DateTime.Now,
                ExpenseId = expenseId
            };
            entities.ActivityLogs.Add(activityLog);

        }


    }
}