using Microsoft.Ajax.Utilities;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data.Entity.Core.Objects;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.Remoting.Contexts;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using wu2.Models;

namespace wu2.Controllers
{
    public class GroupController : Controller
    {
        wuEntities1 entities = new wuEntities1();
        // 创建群组
        private static readonly HttpClient client = new HttpClient();
        private const string apiKey = "76b007be3c0a3a4d48447c72"; // 使用你的 API 金鑰
        private const string apiUrl = "https://v6.exchangerate-api.com/v6/{0}/latest/USD";

        public ActionResult JoinGroup(string joinLink)
        {
            // 查找群组
            var group = entities.Groups.FirstOrDefault(g => g.JoinLink == joinLink);
            // 检查用户是否已登录
            if (Session["UserID"] == null)
            {
                // 用户未登录，重定向到登录页面并附加 joinLink 参数
                return RedirectToAction("Login", "Account", new { joinLink });
            }

            // 检查用户是否已登录
            int? userId = Session["UserID"] as int?;
            if (userId == null)
            {
                // 将 joinLink 存入 TempData，以便登录后使用
                TempData["JoinLink"] = joinLink;
                return RedirectToAction("Login", "Account");
            }

            // 检查用户是否已经在群组中
            var existingMember = entities.GroupMembers.FirstOrDefault(gm => gm.GroupId == group.GroupId && gm.UserId == userId);
            var user = entities.Users.Find(userId.Value);
            if (existingMember == null)
            {
                // 将用户加入群组
                var newMember = new GroupMembers
                {
                    GroupId = group.GroupId,
                    UserId = userId.Value,
                    JoinedDate = DateTime.Now,
                    Role = "Editor",
                };
                entities.GroupMembers.Add(newMember);
                entities.SaveChanges();
                var username = user.FullName;
                LogActivity(group.GroupId, userId.Value, "加入群組", $"成員 {username} 加入了群組");

            }

            return RedirectToAction("Details", new { id = group.GroupId });
        }
        public JsonResult GetUserGroups(string chatId = null)
        {
            // 获取当前登录用户的ID
            int? userId = Session["UserID"] as int?;

            // 如果没有用户ID，则返回错误
     

            // 如果 chatId 不为空，按 chatId 查找对应的群组   
            if (!string.IsNullOrEmpty(chatId) && Session["UserID"] != null)
            {
                var group = entities.Groups
                               .Where(g => g.ChatId == chatId)
                               .Select(g => new
                               {
                                   GroupId = g.GroupId,
                                   GroupName = g.GroupName,
                                   GroupsPhoto = g.GroupsPhoto,
                                   CreatedDate = g.CreatedDate
                               })
                               .FirstOrDefault();

                if (group != null)
                {
                    return Json(new
                    {
                        success = true,
                        groups = new[]
                        {
                    new {
                        GroupId = group.GroupId,
                        GroupName = group.GroupName,
                        GroupsPhoto = group.GroupsPhoto,
                        CreatedDate = group.CreatedDate
                    }
                }
                    }, JsonRequestBehavior.AllowGet);
                }
                else
                {
                    return Json(new { success = false, message = "Group not found." }, JsonRequestBehavior.AllowGet);
                }
            }
            else
            {
                // 如果 chatId 为空，使用原来的方式按用户ID查找群组
                var groups = entities.GroupMembers
                                 .Where(gm => gm.UserId == userId)
                                 .Select(gm => new
                                 {
                                     GroupId = gm.Groups.GroupId,
                                     GroupName = gm.Groups.GroupName,
                                     GroupsPhoto = gm.Groups.GroupsPhoto,
                                     CreatedDate = gm.Groups.CreatedDate
                                 })
                                 .OrderByDescending(gm => gm.CreatedDate)
                                 .ToList();

                return Json(new { success = true, groups = groups }, JsonRequestBehavior.AllowGet);
            }
        }

        public async Task<ActionResult> Create()
        {
            if (Session["UserID"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            try
            {
                // 構建 API 請求的 URL
                var requestUrl = string.Format(apiUrl, apiKey);

                // 發送 HTTP GET 請求
                var response = await client.GetStringAsync(requestUrl);

                // 解析 JSON 響應
                var data = JObject.Parse(response);

                // 從響應中提取貨幣數據
                var currencies = data["conversion_rates"].ToObject<Dictionary<string, double>>();
                // 將貨幣數據轉換為 SelectListItem 列表
                var currencyList = currencies.Select(kvp => new SelectListItem
                {
                    Value = kvp.Key,
                    Text = $"{kvp.Key} - {kvp.Value.ToString("F4")}" // 顯示貨幣代碼和匯率，格式化匯率到小數點後4位
                }).ToList();

                // 將貨幣數據傳遞給視圖
                ViewBag.CurrencyList = new SelectList(currencyList, "Value", "Text");
            }
            catch (Exception ex)
            {
                // 錯誤處理
                ViewBag.Error = "無法獲取貨幣數據，請稍後再試。"+ex;
            }

            return View(new Groups());
        }
        [HttpPost]
        public ActionResult Create(Groups model, HttpPostedFileBase GroupsPhoto)
        {
            if (Session["UserID"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            if (ModelState.IsValid)
            {

                //// 檢查 BudgetAlertThreshold 是否大於 Budget
                //if (model.BudgetAlertThreshold > model.Budget)
                //{
                //    ModelState.AddModelError("BudgetAlertThreshold", "提醒額度不可大於初始預算。");
                //    return View(model);
                //}
                model.CreatorId = (int)Session["UserID"];
                model.CreatedDate = DateTime.Now;
                model.JoinLink = Guid.NewGuid().ToString();
                // 處理群組大頭貼圖片上傳
                if (GroupsPhoto != null && GroupsPhoto.ContentLength > 0)
                {
                    string fileName = Path.GetFileName(GroupsPhoto.FileName);
                    string path = Path.Combine(Server.MapPath("~/GroupPhoto"), fileName);
                    GroupsPhoto.SaveAs(path);

                    // 檢查文件是否成功上傳
                    if (System.IO.File.Exists(path))
                    {
                        // 成功
                        model.GroupsPhoto = "~/GroupPhoto/" + fileName;
                    }
                    else
                    {
                        // 失敗
                        ViewBag.ErrorMessage = "上傳失敗";
                        return View(model);
                    }
                }

                entities.Groups.Add(model);
                entities.SaveChanges();

                GroupMembers creatorMember = new GroupMembers
                {
                    GroupId = model.GroupId,
                    UserId = model.CreatorId,
                    Role = "Creator",
                    JoinedDate = DateTime.Now
                };
                entities.GroupMembers.Add(creatorMember);
                entities.SaveChanges();
                // 設置成功訊息

                // 設置成功訊息
                TempData["SuccessMessage"] = "群組創建成功！";
                return RedirectToAction("Details", new { id = model.GroupId });
            }
            return View(model);
        }

        // 显示我的群组
        [HttpGet]
        public ActionResult MyGroups()
        {
            if (Session["UserID"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            int userId = (int)Session["UserID"];
            var groups = entities.GroupMembers
                                 .Where(gm => gm.UserId == userId)
                                 .Select(gm => gm.Groups)
                                .OrderBy(g => g.IsArchived)  // First sort by IsArchived
                         .ThenByDescending(g => g.CreatedDate)  // Then sort by CreatedDate
                                 .ToList();
            var groupCreatorInfo = groups.ToDictionary(g => g.GroupId, g => g.CreatorId == userId);

            ViewBag.GroupCreatorInfo = groupCreatorInfo;
            return View(groups);
        }

        // 显示群组详情
        [HttpGet]
        public ActionResult Details(int? id)
        {
            if (Session["UserID"] == null || id == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var group = entities.Groups
                                .Include("GroupMembers")
                                .FirstOrDefault(g => g.GroupId == id);
            var currentUserId = (int)Session["UserID"];

            var MemberRole = entities.GroupMembers.Where(gm=>gm.UserId == currentUserId && gm.GroupId == group.GroupId).Select(gm => gm.Role).FirstOrDefault();
            ViewBag.MemberRole = MemberRole;

            
            if (group == null)
            {
                ViewBag.ErrorMessage = "群組不存在或已被刪除。";
                return View("Error");
            }

            int userId = (int)Session["UserID"];
            var userRole = group.GroupMembers
                                .FirstOrDefault(gm => gm.UserId == userId)?.Role;

            ViewBag.UserRole = userRole;
            ViewBag.TotalExpenses = GetTotalGroupExpenses(id.Value);
            ViewBag.IsCreator = group.CreatorId == currentUserId;
            var notices = entities.Notices.Where(n => n.GroupId == id).OrderByDescending(n => n.CreatedAt).ToList();
            ViewBag.CurrentNotices = notices.Where(n => n.IsCurrent == true).ToList();
            ViewBag.HistoryNotices = notices.Where(n => n.IsCurrent == false).ToList();

            return View(group);

        }
        private decimal GetTotalGroupExpenses(int groupId)
        {
            var totalExpenses = entities.Expenses
                                        .Where(e => e.GroupId == groupId)
                                        .Sum(e => (decimal?)e.TotalAmount) ?? 0;
            return totalExpenses;
        }

        [HttpGet]
        public ActionResult Edit(int? id)
        {
            if (Session["UserID"] == null || id == null)
            {
                return RedirectToAction("Login", "Account");
            } var group = entities.Groups   .FirstOrDefault(g => g.GroupId == id);
            var totalExpenses = entities.Expenses
                                       .Where(e => e.GroupId == group.GroupId)
                                       .Sum(e => (decimal?)e.TotalAmount) ?? 0;

            ViewBag.totalExpenses = totalExpenses;
            if (group.Budget < totalExpenses)
            {
                ViewBag.ErrorMessage = "需要大於分帳總金額";
                return View(group);
            }
      

            if (group == null)
            {
                return HttpNotFound(); // 返回 404 錯誤
            }
            int userId = (int)Session["UserID"];
            // 查詢當前用戶角色
            var currentUserRole = entities.Users
                .Where(u => u.UserId == userId)
                .Select(u => u.Role)
                .FirstOrDefault(); // 使用 FirstOrDefault 來獲取第一個匹配的結果


            ViewBag.GroupId = group.GroupId;
            ViewBag.HasFullPermission = currentUserRole == "Root" || userId == group.CreatorId;
            return View(group);
        }

        [HttpPost]
        public ActionResult Edit(Groups model, HttpPostedFileBase GroupPhoto)
        {
            if (Session["UserID"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var existingGroup = entities.Groups.FirstOrDefault(g => g.GroupId == model.GroupId);
            bool hasFullPermission = existingGroup.CreatorId == (int)Session["UserID"];
            if (existingGroup == null)
            {
                return HttpNotFound(); // 返回 404 錯誤
            }

            ViewBag.GroupId = existingGroup.GroupId;

            ViewBag.HasFullPermission = hasFullPermission;
            ViewBag.InsufficientPermissions = !hasFullPermission;


            {
                var totalExpenses = entities.Expenses
                                        .Where(e => e.GroupId == model.GroupId)
                                        .Sum(e => (decimal?)e.TotalAmount) ?? 0;

                ViewBag.totalExpenses = totalExpenses;
                if (model.Budget < totalExpenses)
                {
                    ViewBag.ErrorMessage = "需要大於分帳總金額";
                    return View(model);
                }
                // 将修改动作整合成中文字符串
                string previousDetails = $"";

                if (existingGroup.GroupName != model.GroupName)
                {
                    previousDetails += $"群組名稱: {existingGroup.GroupName} ⮕ {model.GroupName}，<br>";
                }

                if (hasFullPermission)
                {
                    if (existingGroup.Budget != model.Budget)
                    {
                        previousDetails += $"預算: {existingGroup.Budget}{existingGroup.Currency} ⮕ {model.Budget}{existingGroup.Currency}，<br>";
                    }


                }


                if (GroupPhoto != null && GroupPhoto.ContentLength > 0)
                {
                    string fileName = Path.GetFileName(GroupPhoto.FileName);
                    if (System.Text.RegularExpressions.Regex.IsMatch(fileName, @"[^a-zA-Z0-9_\-\.]") || fileName.Length > 100)
                    {
                        // 添加錯誤訊息，提醒使用者
                        ModelState.AddModelError("", "檔案名稱包含特殊字元或名稱過長，請重新上傳。名稱應簡短，且只包含英文、數字、底線(_)與破折號(-)。");
                        return View(model);  // 回到原頁面，顯示錯誤訊息
                    }
                    string directoryPath = Server.MapPath("~/GroupPhoto");
                    if (!Directory.Exists(directoryPath))
                    {
                        Directory.CreateDirectory(directoryPath);
                    }
                    string path = Path.Combine(directoryPath, fileName);
                    GroupPhoto.SaveAs(path);

                    if (System.IO.File.Exists(path))
                    {
                        existingGroup.GroupsPhoto = "~/GroupPhoto/" + fileName;
                        previousDetails += $"群組頭像已修改";
                    }
                    else
                    {
                        ViewBag.ErrorMessage = "上傳失敗";
                        return View(model);
                    }
                }

                if (ModelState.IsValid)
                {

                    existingGroup.Budget = model.Budget;
                    existingGroup.GroupName = model.GroupName;

                    // 如果有修改，则记录到 activitylogs
                    if (!string.IsNullOrEmpty(previousDetails))
                    {
                        var userId = (int)Session["UserID"];
                        var activityLog = new ActivityLogs
                        {
                            GroupId = model.GroupId,
                            UserId = userId,
                            ActivityType = "群組編輯",
                            ActivityDetails = previousDetails,
                            Date = DateTime.Now
                        };
                        entities.ActivityLogs.Add(activityLog);
                    }

                    entities.SaveChanges();
                    return RedirectToAction("Details", new { id = model.GroupId });
                }

                return View(model);
            }
        }
            public void LogActivity(int groupId, int userId, string activityType, string details)
            {
                var activityLog = new ActivityLogs
                {
                    GroupId = groupId,
                    UserId = userId,
                    ActivityType = activityType,
                    ActivityDetails = details,
                    Date = DateTime.Now
                };

                entities.ActivityLogs.Add(activityLog);
                entities.SaveChanges();
            }
            // 邀请成员
            [HttpGet]
            public ActionResult Invite(int? groupId)
            {
                if (Session["UserID"] == null || groupId == null)
                {
                    return RedirectToAction("Login", "Account");
                }



                ViewBag.GroupId = groupId;

                var model = new InviteViewModel
                {
                    GroupId = groupId.Value,
                };


                int userId = (int)Session["UserID"];     // Get the current user's role in the group
                var currentUserRole = entities.GroupMembers
                                            .FirstOrDefault(gm => gm.GroupId == groupId && gm.UserId == userId)?.Role;
                ViewBag.UserRole = currentUserRole;

                ViewBag.InvitationResult = string.Empty;
                return View(model);
            }
            [HttpPost]
            public ActionResult Invite(InviteViewModel model, string[] Emails, string[] Roles)
            {
                if (Session["UserID"] == null)
                {
                    return RedirectToAction("Login", "Account");
                }

                int groupId = model.GroupId;
                int userId = (int)Session["UserID"];     // Get the current user's role in the group
                var currentUserRole = entities.GroupMembers
                                            .FirstOrDefault(gm => gm.GroupId == groupId && gm.UserId == userId)?.Role;
                ViewBag.UserRole = currentUserRole;

                if (Emails == null || Roles == null || Emails.Length != Roles.Length)
                {
                    ViewBag.InvitationResult = "電子郵件和權限數量不匹配";
                    return View(model);
                }


                if (currentUserRole != "Creator" && currentUserRole != "Admin")
                {
                    ViewBag.InvitationResult = "你沒有權限執行此操作，請聯絡管理員";
                    return View(model);
                }

                if (ModelState.IsValid)
                {
                    var group = entities.Groups.FirstOrDefault(g => g.GroupId == model.GroupId);
                    if (group == null)
                    {
                        ViewBag.InvitationResult = "找不到群組";
                        return View(model);
                    }

                    List<string> results = new List<string>();

                    for (int i = 0; i < Emails.Length; i++)
                    {
                        var email = Emails[i];
                        var role = Roles[i];
                        var user = entities.Users.FirstOrDefault(u => u.Email == email);

                        if (user != null)
                        {
                            var isMember = entities.GroupMembers.Any(gm => gm.GroupId == model.GroupId && gm.UserId == user.UserId);
                            if (!isMember)
                            {
                                var existingInvitation = entities.Notifications.FirstOrDefault(n => n.UserId == user.UserId && n.GroupId == groupId && n.NotificationType == "Invitation");
                                if (existingInvitation == null || existingInvitation.Status == "Declined")
                                {
                                    Notifications notification = new Notifications
                                    {
                                        UserId = user.UserId,
                                        GroupId = model.GroupId,
                                        NotificationType = "Invitation",
                                        Message = $"你已經被 {group.GroupName} 邀請成為 {role}.",
                                        IsRead = false,
                                        Date = DateTime.Now,
                                        Status = "Pending"  // 設置狀態為待處理
                                    };
                                    entities.Notifications.Add(notification);

                                    Notifications senderNotification = new Notifications
                                    {
                                        UserId = (int)Session["UserID"],
                                        GroupId = model.GroupId,
                                        NotificationType = "InvitationSent",
                                        Message = $"你已經發了一則邀請給 {email} 作為 {role}.",
                                        IsRead = true,
                                        Date = DateTime.Now,
                                        Status = "Pending"
                                    };
                                    entities.Notifications.Add(senderNotification);

                                    entities.SaveChanges();
                                    results.Add($"邀請發送成功給 {email}");
                                }
                                else
                                {
                                    results.Add($"你已發過邀請給 {email}");
                                }
                            }
                            else
                            {
                                results.Add($"用戶 {email} 已經是組員");
                            }
                        }
                        else
                        {
                            results.Add($"找不到用戶 {email}");
                        }
                    }

                    ViewBag.InvitationResult = string.Join("<br/>", results);
                }
                else
                {
                    ViewBag.InvitationResult = "請輸入有效值";
                }

                return View(model);
            }


            [HttpPost]
            public ActionResult HandleInvitation(int? notificationId, bool accept)
            {
                if (Session["UserID"] == null || notificationId == null)
                {
                    return RedirectToAction("Login", "Account");
                }

                var notification = entities.Notifications.FirstOrDefault(n => n.NotificationId == notificationId);
                if (notification != null && notification.UserId == (int)Session["UserID"])
                {
                    if (accept)
                    {
                        var userFullName = entities.Users.FirstOrDefault(u => u.UserId == notification.UserId)?.FullName;
                        var userEmail = entities.Users.FirstOrDefault(u => u.UserId == notification.UserId)?.Email;
                        var GroupName = entities.Groups.FirstOrDefault(u => u.GroupId == notification.GroupId)?.GroupName;
                        GroupMembers newMember = new GroupMembers
                        {
                            GroupId = notification.GroupId.Value,
                            UserId = notification.UserId,
                            Role = notification.Message.Split(' ').Last().Trim('.'),
                            JoinedDate = DateTime.Now
                        };
                        entities.GroupMembers.Add(newMember);

                        // 记录活动日志
                        var activityLog = new ActivityLogs
                        {
                            GroupId = notification.GroupId.Value,
                            UserId = notification.UserId,
                            ActivityType = "加入群組",
                            ActivityDetails = $"{userFullName}({userEmail}) 加入了群組 {GroupName}",
                            Date = DateTime.Now
                        };
                        entities.ActivityLogs.Add(activityLog);
                        Notifications senderNotification = new Notifications
                        {
                            UserId = entities.Groups.FirstOrDefault(g => g.GroupId == notification.GroupId).CreatorId,
                            GroupId = notification.GroupId,
                            NotificationType = "InvitationAccepted",
                            Message = $"{userFullName} 已經接受你的邀請",
                            IsRead = false,   // 新通知設置為未讀   
                            Date = DateTime.Now,
                            Status = "Accepted"
                        };
                        Notifications AcceptNotification = new Notifications
                        {
                            UserId = (int)Session["UserID"],
                            GroupId = notification.GroupId,
                            NotificationType = "InvitationAccepted",
                            Message = $"你已經接受邀請加入 {GroupName}.",
                            IsRead = true,
                            Date = DateTime.Now,
                            Status = "Accepted"
                        };
                        entities.Notifications.Add(senderNotification);
                        entities.Notifications.Add(AcceptNotification);  //
                        notification.IsRead = true;
                        notification.Status = "Accepted";  // 更新狀態為接受
                        entities.SaveChanges();

                        TempData["Message"] = "成功加入群組";
                    }
                    else
                    {
                        var userFullName = entities.Users.FirstOrDefault(u => u.UserId == notification.UserId)?.FullName;
                        var userEmail = entities.Users.FirstOrDefault(u => u.UserId == notification.UserId)?.Email;
                        var GroupName = entities.Groups.FirstOrDefault(u => u.GroupId == notification.GroupId)?.GroupName;
                        Notifications senderNotification = new Notifications
                        {
                            UserId = entities.Groups.FirstOrDefault(g => g.GroupId == notification.GroupId).CreatorId,
                            GroupId = notification.GroupId,
                            NotificationType = "InvitationDeclined",
                            Message = $"{userFullName}({userEmail}) 拒絕你的邀請",
                            IsRead = false,
                            Date = DateTime.Now,
                            Status = "Declined"
                        };
                        Notifications RejectNotification = new Notifications
                        {
                            UserId = (int)Session["UserID"],
                            GroupId = notification.GroupId,
                            NotificationType = "InvitationDeclined",
                            Message = $"你已經拒絕你的邀請加入 {GroupName}.",
                            IsRead = true,
                            Date = DateTime.Now,
                            Status = "Declined"
                        };

                        entities.Notifications.Add(senderNotification);
                        entities.Notifications.Add(RejectNotification);
                        // 將用戶的邀請通知標記為已讀
                        notification.Status = "Declined";  // 更新狀態為拒絕
                        notification.IsRead = true;
                        entities.SaveChanges();

                        TempData["Message"] = "你已拒絕了邀請";

                    }

                    return RedirectToAction("Notifications");
                }
                else
                {
                    ViewBag.Message = "找不到邀請";
                }

                return View();
            }

            // 加载通知
            public ActionResult Notifications()
            {
                if (Session["UserID"] == null)
                {
                    return RedirectToAction("Login", "Account");
                }

                int userId = (int)Session["UserID"];
                var notifications = entities.Notifications
                    .Where(n => n.UserId == userId)
                    .OrderByDescending(n => n.Date)
                    .ToList();

                return View(notifications);
                //return PartialView("_NotificationsDropdown", notifications);
            }



            private void LogActivity(int groupId, int? userId, decimal totalAmount)
            {
                if (!userId.HasValue || !entities.Users.Any(u => u.UserId == userId.Value))
                    return;

                var activityLog = new ActivityLogs
                {
                    GroupId = groupId,
                    UserId = userId.Value,
                    ActivityType = "新增支出",
                    ActivityDetails = $"新增了一筆總金額為{totalAmount}的支出。",
                    Date = DateTime.Now,

                };
                entities.ActivityLogs.Add(activityLog);

            }
            public ActionResult UnreadNotificationsCount()
            {
                if (Session["UserID"] == null)
                {
                    return Json(0); // 如果用戶未登錄，返回0
                }

                int userId = (int)Session["UserID"];
                var unreadCount = entities.Notifications.Count(n => n.UserId == userId && (!n.IsRead.HasValue || !n.IsRead.Value));
                return Json(unreadCount, JsonRequestBehavior.AllowGet);
            }
            [HttpPost]
            public ActionResult MarkAsRead(int notificationId)
            {
                if (Session["UserID"] == null)
                {
                    return Json(new { success = false, message = "使用者尚未登入" });
                }

                try
                {
                    int userId = (int)Session["UserID"];
                    var notification = entities.Notifications.FirstOrDefault(n => n.NotificationId == notificationId && n.UserId == userId);

                    // 檢查通知是否存在且不是邀請類型的通知
                    if (notification != null && notification.NotificationType != "Invitation")
                    {
                        notification.IsRead = true;
                        entities.SaveChanges();
                        return Json(new { success = true, message = "Notification marked as read." });
                    }
                    else
                    {
                        return Json(new { success = false, message = "Notification is an invitation and cannot be marked as read." });
                    }
                }
                catch (Exception ex)
                {
                    return Json(new { success = false, message = ex + "An error occurred while marking notification as read." });
                }
            }
            [HttpPost]
            public ActionResult MarkAllAsRead()
            {
                if (Session["UserID"] == null)
                {
                    return RedirectToAction("Login", "Account");
                }

                try
                {
                    int userId = (int)Session["UserID"];

                    // 從資料庫中取得所有相關通知
                    var notifications = entities.Notifications
                                                .Where(n => n.UserId == userId && n.NotificationType != "Invitation")
                                                .ToList(); //取得所有通知並轉換為列表

                    // 在記憶體中處理通知
                    foreach (var notification in notifications)
                    {
                        if (!notification.IsRead.HasValue || !notification.IsRead.Value)
                        {
                            notification.IsRead = true;
                        }
                    }

                    entities.SaveChanges();
                    TempData["SuccessMessage"] = "所有通知已標記為已讀。";
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = "標記所有通知為已讀時發生錯誤: " + ex.Message;
                }

                return RedirectToAction("Notifications"); // 假設 Notifications 是顯示通知的視圖
            }

            [HttpPost]
            public ActionResult EditMemberPermissions(int groupId, int memberId, string role)
            {
                if (Session["UserID"] == null)
                {
                    ViewBag.ErrorMessage = "請先登入";
                    return View("Error");
                }

                var currentUserId = (int)Session["UserID"];
                var group = entities.Groups.FirstOrDefault(g => g.GroupId == groupId);
                if (group == null)
                {
                    ViewBag.ErrorMessage = "找不到群組";
                    return View("Error");
                }

                var isCreator = group.CreatorId == currentUserId;
                if (!isCreator)
                {
                    ViewBag.ErrorMessage = "權限不足";
                    return View("Error");
                }

                var existingMember = entities.GroupMembers.FirstOrDefault(gm => gm.GroupId == groupId && gm.UserId == memberId);
                if (existingMember == null)
                {
                    ViewBag.ErrorMessage = "找不到成員";
                    return View("Error");
                }
                var userFullName = entities.Users.FirstOrDefault(u => u.UserId == memberId)?.FullName;
                var groupName = group.GroupName;
                // 記錄活動日誌
                var activityLog = new ActivityLogs
                {
                    GroupId = groupId,
                    UserId = memberId,
                    ActivityType = "更新權限",
                    ActivityDetails = $"{userFullName} 的權限被更新為 {role} 在群組 {groupName}",
                    Date = DateTime.Now
                };
                entities.ActivityLogs.Add(activityLog);
                existingMember.Role = role;
                entities.SaveChanges();

                return RedirectToAction("Details", new { id = groupId });
            }
            //[HttpPost]
            public ActionResult RemoveGroup(int groupId)
            {
                if (Session["UserID"] == null)
                {
                    TempData["ErrorMessage"] = "請先登入";
                    return RedirectToAction("MyGroups");
                }

                var group = entities.Groups.FirstOrDefault(g => g.GroupId == groupId);
                if (group == null)
                {
                    TempData["ErrorMessage"] = "找不到群組";
                    return RedirectToAction("MyGroups");
                }

                var currentUserId = (int)Session["UserID"];
                var isCreator = group.CreatorId == currentUserId;
                if (!isCreator)
                {
                    TempData["ErrorMessage"] = "只有群組建立者才能刪除群組";
                    return RedirectToAction("MyGroups");
                }

                // 取得群組內所有成員
                var members = entities.GroupMembers.Where(gm => gm.GroupId == groupId).ToList();

                // 取得所有與該群組相關的債務
                var debts = entities.Debts.Where(d => d.GroupId == groupId && !d.IsPaid).ToList();

                // 檢查是否有未結清的債務
                foreach (var member in members)
                {
                    var memberDebts = debts.Where(d => d.DebtorId == member.UserId).Sum(d => d.Amount);
                    var memberCredits = debts.Where(d => d.CreditorId == member.UserId).Sum(d => d.Amount);

                    if (memberDebts > 0 || memberCredits > 0)
                    {
                        TempData["ErrorMessage"] = "群組內有成員存在未結清的帳目，不能刪除群組";
                        return RedirectToAction("MyGroups");
                    }
                }
                // 將所有與該群組相關的通知的 GroupId 設置為 NULL
                var notifications = entities.Notifications.Where(n => n.GroupId == groupId).ToList();
                foreach (var notification in notifications)
                {
                    notification.GroupId = null;
                }

                // 删除群组内的所有成员
                foreach (var member in members)
                {
                    entities.GroupMembers.Remove(member);
                }
                // 更新所有与该群组相关的待处理或已发出邀请通知的状态
                notifications = entities.Notifications
                                            .Where(n => n.GroupId == groupId && (n.Status == "Accepted" || n.NotificationType == "Invitation"))
                                            .ToList();

                foreach (var notification in notifications)
                {
                    notification.Status = "Declined"; // 设置状态为取消
                    notification.IsRead = true; // 标记为已读
                }

                // 删除群组
                entities.Groups.Remove(group);
                entities.SaveChanges();
                TempData["ErrorMessage"] = "已成功刪除群組";
                return RedirectToAction("MyGroups");
            }

            [HttpPost]
            public ActionResult RemoveMember(int groupId, int memberId)
            {
                if (Session["UserID"] == null)
                {
                    TempData["ErrorMessage"] = "請先登入群組";
                    return RedirectToAction("Details", new { id = groupId });
                }
                var group = entities.Groups.FirstOrDefault(g => g.GroupId == groupId);
                if (group == null)
                {
                    TempData["ErrorMessage"] = "找不到群組";
                    return RedirectToAction("MyGroups");
                }
                var currentUserId = (int)Session["UserID"];
                var isCreator = group.CreatorId == currentUserId;
                var isAdmin = entities.GroupMembers.Any(gm => gm.GroupId == groupId && gm.UserId == currentUserId && gm.Role.ToLower() == "admin");
                if (!isCreator && !isAdmin)
                {
                    TempData["ErrorMessage"] = "權限不足";

                    return RedirectToAction("Details", new { id = groupId });
                }
                // 获取成员的详细债务信息
                var debts = entities.Debts
                                    .Where(d => d.GroupId == groupId)
                                    .ToList();
                var memberDebts = debts.Where(d => d.DebtorId == memberId && !d.IsPaid).Sum(d => d.Amount);
                var memberCredits = debts.Where(d => d.CreditorId == memberId && !d.IsPaid).Sum(d => d.Amount);
                // 如果成员有未结清的应收账款或应付账款，不能删除
                if (memberDebts > 0 || memberCredits > 0)
                {
                    TempData["ErrorMessage"] = "成員有未結清的帳目，不能刪除";
                    return RedirectToAction("Details", new { id = groupId });
                }
                var member = entities.GroupMembers.FirstOrDefault(gm => gm.GroupId == groupId && gm.UserId == memberId);
                if (member == null)
                {
                    TempData["ErrorMessage"] = "找不到成員";
                    return RedirectToAction("Details", new { id = groupId });
                }
                var notifications = entities.Notifications
                                  .Where(n => n.GroupId == groupId && n.UserId == memberId && (n.Status == "Accepted" || n.NotificationType == "Invitation"))
                                  .ToList();
                foreach (var notification in notifications)
                {
                    notification.Status = "Declined"; // 设置状态为取消
                    notification.IsRead = true; // 标记为已读
                }   // 記錄活動日誌

                var user = entities.Users.FirstOrDefault(u => u.UserId == memberId);
                var Creatoruser = entities.Users.FirstOrDefault(u => u.UserId == currentUserId);
                var memberName = user?.FullName ?? "成員";
                var memberEmail = user?.Email ?? "未知電子郵件";

                Notifications RemovetNotification = new Notifications
                {
                    UserId = memberId,
                    GroupId = groupId,
                    NotificationType = "Remove",
                    Message = $"你已經被踢出 {group.GroupName}.",
                    IsRead = false,
                    Date = DateTime.Now,
                    Status = "Declined"
                };
                entities.Notifications.Add(RemovetNotification);

                var activityLog = new ActivityLogs
                {
                    GroupId = groupId,
                    UserId = memberId,
                    ActivityType = "成員踢除",
                    ActivityDetails = $"{memberName}({memberEmail})已被 {Creatoruser.FullName} 踢出 {group.GroupName}",
                    Date = DateTime.Now
                }; entities.ActivityLogs.Add(activityLog);
                entities.GroupMembers.Remove(member);
                entities.SaveChanges();
                TempData["ErrorMessage"] = "已移除成員";
                return RedirectToAction("Details", new { id = groupId });
            }
            [HttpPost]
            public ActionResult LeaveGroup(int groupId)
            {
                if (Session["UserID"] == null)
                {
                    TempData["ErrorMessage"] = "請先登入";
                    return RedirectToAction("MyGroups");
                }

                var currentUserId = (int)Session["UserID"];
                var group = entities.Groups.FirstOrDefault(g => g.GroupId == groupId);
                if (group == null)
                {
                    TempData["ErrorMessage"] = "找不到群組";
                    return RedirectToAction("MyGroups");
                }

                var member = entities.GroupMembers.FirstOrDefault(gm => gm.GroupId == groupId && gm.UserId == currentUserId);
                if (member == null)
                {
                    TempData["ErrorMessage"] = "您不是該群組的成員";
                    return RedirectToAction("MyGroups");
                }

                var debts = entities.Debts.Where(d => d.GroupId == groupId && !d.IsPaid).ToList();
                var memberDebts = debts.Where(d => d.DebtorId == currentUserId).Sum(d => d.Amount);
                var memberCredits = debts.Where(d => d.CreditorId == currentUserId).Sum(d => d.Amount);

                if (memberDebts > 0 || memberCredits > 0)
                {
                    TempData["ErrorMessage"] = "您存在未結清的帳目，不能離開群組";

                    return RedirectToAction("MyGroups");
                }

                // 更新所有与该成员相关的待处理或已发出邀请通知的状态
                var notifications = entities.Notifications.Where(n => n.GroupId == groupId && n.UserId == currentUserId && (n.Status == "Accepted" || n.NotificationType == "Invitation"))
                    .ToList();

                foreach (var notification in notifications)
                {
                    notification.Status = "Declined";
                    notification.IsRead = true;
                }
                var user = entities.Users.FirstOrDefault(u => u.UserId == currentUserId);

                var memberName = user?.FullName ?? "成員";
                var memberEmail = user?.Email ?? "未知電子郵件";

                Notifications RemovetNotification = new Notifications
                {
                    UserId = currentUserId,
                    GroupId = groupId,
                    NotificationType = "Leave",
                    Message = $"你已經離開了 {group.GroupName}.",
                    IsRead = true,
                    Date = DateTime.Now,
                    Status = "Declined"
                };

                var activityLog = new ActivityLogs
                {
                    GroupId = groupId,
                    UserId = currentUserId,
                    ActivityType = "成員離開",
                    ActivityDetails = $" {memberName} ({memberEmail})已離開 {group.GroupName}",
                    Date = DateTime.Now
                }; entities.ActivityLogs.Add(activityLog);
                entities.GroupMembers.Remove(member);
                entities.SaveChanges();
                TempData["ErrorMessage"] = "已成功離開";
                return RedirectToAction("MyGroups");
            }
            [HttpPost]
            public ActionResult ArchiveGroup(int groupId)
            {
                if (Session["UserID"] == null)
                {
                    TempData["ErrorMessage"] = "請先登入";
                    return RedirectToAction("MyGroups");
                }

                var group = entities.Groups.FirstOrDefault(g => g.GroupId == groupId);
                if (group == null)
                {
                    TempData["ErrorMessage"] = "找不到群組";
                    return RedirectToAction("MyGroups");
                }

                var currentUserId = (int)Session["UserID"];
                if (group.CreatorId != currentUserId)
                {
                    TempData["ErrorMessage"] = "只有群組創建者才能封存群組";
                    return RedirectToAction("MyGroups");
                }

                group.IsArchived = true;
                entities.SaveChanges();

                TempData["ErrorMessage"] = "群組已成功封存";
                return RedirectToAction("MyGroups");
            }
            [HttpPost]
            public ActionResult UnarchiveGroup(int groupId)
            {
                if (Session["UserID"] == null)
                {
                    TempData["ErrorMessage"] = "請先登入";
                    return RedirectToAction("MyGroups");
                }

                var group = entities.Groups.FirstOrDefault(g => g.GroupId == groupId);
                if (group == null)
                {
                    TempData["ErrorMessage"] = "找不到群組";
                    return RedirectToAction("MyGroups");
                }

                var currentUserId = (int)Session["UserID"];
                if (group.CreatorId != currentUserId)
                {
                    TempData["ErrorMessage"] = "只有群組創建者才能解除封存";
                    return RedirectToAction("MyGroups");
                }

                group.IsArchived = false;
                entities.SaveChanges();

                TempData["ErrorMessage"] = "群組已成功解除封存";
                return RedirectToAction("MyGroups");
            }

        }



    }
