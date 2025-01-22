using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Net.Mail;
using System.Net;
using System.Web;
using System.Web.Mvc;
using wu2.Models;

namespace wu2.Controllers
{
    public class SettleController : Controller
    {
        wuEntities1 entities = new wuEntities1();
        // GET: Settle
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult PairwiseSettlement(int? groupId)
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
            var currentUserId = (int)Session["UserID"];
            ViewBag.currentUserId = currentUserId;
            var debts = entities.Debts
                                .Where(d => d.GroupId == groupId)
                                .ToList();

            var memberDebtOverview = members.Select(user => new MemberDebtViewModel
            {
                UserId = user.UserId,
                UserName = user.FullName,
                UserEmail = user.Email,
                UserPhoto  = user.ProfilePhoto,
                TotalOwed = debts.Where(d => d.DebtorId == user.UserId && !d.IsPaid).Sum(d => d.Amount),
                TotalOwedTo = debts.Where(d => d.CreditorId == user.UserId && !d.IsPaid).Sum(d => d.Amount),
                NetDebt = debts.Where(d => d.CreditorId == user.UserId && !d.IsPaid).Sum(d => d.Amount) - debts.Where(d => d.DebtorId == user.UserId && !d.IsPaid).Sum(d => d.Amount)
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
                            DebtorId = debtor.UserId,  // Set DebtorId
                            CreditorId = creditor.UserId,  // Set CreditorId
                            DebtId = debts.FirstOrDefault(d => d.DebtorId == debtor.UserId && d.CreditorId == creditor.UserId && d.Amount == debtAmount)?.DebtId ?? 0
                        });

                        // 更新净欠款
                        debtor.NetDebt += debtAmount;
                        creditor.NetDebt -= debtAmount;
                    }
                }
            }

            ViewBag.GroupId = groupId;
            return View(pairwiseDebts);
        }

        public ActionResult GroupSettlement(int? groupId)
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
                                .Where(d => d.GroupId == groupId && !d.IsPaid)
                                .ToList();

            var memberDebtOverview = members.Select(user => new MemberDebtViewModel
            {
                UserId = user.UserId,
                UserName = user.FullName,
                TotalOwed = debts.Where(d => d.DebtorId == user.UserId).Sum(d => d.Amount),
                TotalOwedTo = debts.Where(d => d.CreditorId == user.UserId).Sum(d => d.Amount),
                NetDebt = debts.Where(d => d.CreditorId == user.UserId).Sum(d => d.Amount) - debts.Where(d => d.DebtorId == user.UserId).Sum(d => d.Amount)
            }).ToList();

            var groupSettlements = new List<PairwiseDebtViewModel>();

            while (true)
            {
                var debtor = memberDebtOverview.Where(m => m.NetDebt < 0).OrderBy(m => m.NetDebt).FirstOrDefault();
                var creditor = memberDebtOverview.Where(m => m.NetDebt > 0).OrderByDescending(m => m.NetDebt).FirstOrDefault();

                if (debtor == null || creditor == null)
                {
                    break;
                }

                var debtAmount = Math.Min(-debtor.NetDebt, creditor.NetDebt);
                if (debtAmount > 0)
                {
                    groupSettlements.Add(new PairwiseDebtViewModel
                    {
                        DebtorName = debtor.UserName,
                        CreditorName = creditor.UserName,
                        Amount = debtAmount
                    });

                    // 更新净欠款
                    debtor.NetDebt += debtAmount;
                    creditor.NetDebt -= debtAmount;
                }
            }

            ViewBag.GroupId = groupId;
            return View(groupSettlements);
        }
        [HttpPost]
        public ActionResult MarkAsPaid(int debtorId, int creditorId, decimal amount, int groupId, bool roundOff)
        {
            if (roundOff)
            {
                amount = Math.Round(amount);
            }
            var groupname =entities.Groups.Where(d=>d.GroupId== groupId).FirstOrDefault().GroupName;
            var debts = entities.Debts
                .Where(d => d.DebtorId == debtorId && d.CreditorId == creditorId && !d.IsPaid)
                .ToList();

            if (debts != null && debts.Any())
            {
                var pendingDebt = debts.FirstOrDefault(d => d.IsPending == true); 
                if (pendingDebt != null)
                {
                    TempData["SuccessMessage"] = "您已經發起了支付請求，請等待對方確認";
                }
                else
                {
                    foreach (var debt in debts)
                    {
                        debt.IsPending = true; 
                        debt.UpdatedAt = DateTime.Now;
                    }
                    entities.SaveChanges();

                    var debtorName = entities.Users.FirstOrDefault(u => u.UserId == debtorId)?.FullName;
                    var creditorName = entities.Users.FirstOrDefault(u => u.UserId == creditorId)?.FullName;
                 
                    var creditorEmail = entities.Users.FirstOrDefault(u => u.UserId == creditorId)?.Email;
                    var debtorEmail = entities.Users.FirstOrDefault(u => u.UserId == debtorId)?.Email;

                    if (!string.IsNullOrEmpty(creditorEmail))
                    {
                        SendResetEmail(creditorEmail, $"您已收到 {debtorName}{debtorEmail} 的付款 {amount} 元。請登入系統確認。By{groupname}");
                    }

                    if (!string.IsNullOrEmpty(debtorEmail))
                    {
                        SendResetEmail(debtorEmail, $"您已支付 {amount} 元給 {creditorName}{creditorEmail}。請等待對方確認。By{groupname}");
                    }
                    SendNotification(creditorId, debts.First().GroupId, "DebtPaymentInitiated", false, $"{debtorName} 已支付 {amount} 元給你，請確認。", debts.First().DebtId);
                    SendNotification(debtorId, debts.First().GroupId, "DebtPaymentInitiated", true, $"你已支付 {amount} 元给 {creditorName}，請等待對方確認。", debts.First().DebtId);
                    TempData["SuccessMessage"] = "付款請求已發送，請等待對方的回覆";
                }
            }
            else
            {
                TempData["ErrorMessage"] = "找不到相關的債務紀錄";
            }


            return RedirectToAction("PairwiseSettlement", new { groupId });
        }// 使用之前定義的 SendResetEmail 方法來發送通知郵件
        private void SendResetEmail(string email, string message)
        {
            var fromAddress = new MailAddress(ConfigurationManager.AppSettings["SmtpUser"], "Your Company");
            var toAddress = new MailAddress(email);
            const string subject = "付款通知";

            // 建立 HTML 格式的郵件內容
            string body = $@"
    <html>
        <body>
            <h1>付款通知</h1>
            <p>{message}</p>
        </body>
    </html>";

            var smtp = new SmtpClient
            {
                Host = ConfigurationManager.AppSettings["SmtpHost"],
                Port = int.Parse(ConfigurationManager.AppSettings["SmtpPort"]),
                EnableSsl = true,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(
                    ConfigurationManager.AppSettings["SmtpUser"],
                    ConfigurationManager.AppSettings["SmtpPass"])
            };

            using (var msg = new MailMessage(fromAddress, toAddress)
            {
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            })
            {
                smtp.Send(msg);
            }
        }
        [HttpPost]
        public ActionResult ConfirmPayment(int debtId, bool isReceived)
        {
            var debt = entities.Debts.FirstOrDefault(d => d.DebtId == debtId);
            if (debt != null)
            {
                var debtorName = entities.Users.FirstOrDefault(u => u.UserId == debt.DebtorId)?.FullName;
                var creditorName = entities.Users.FirstOrDefault(u => u.UserId == debt.CreditorId)?.FullName;
                var debtorEmail = entities.Users.FirstOrDefault(u => u.UserId == debt.DebtorId)?.Email;
                var creditorEmail = entities.Users.FirstOrDefault(u => u.UserId == debt.CreditorId)?.Email;

                var relatedDebts = entities.Debts
          .Where(d =>
              (d.DebtorId == debt.DebtorId && d.CreditorId == debt.CreditorId && !d.IsPaid) ||
              (d.DebtorId == debt.CreditorId && d.CreditorId == debt.DebtorId && !d.IsPaid))
          .ToList();

                if (isReceived)
                {
                    decimal netAmount = 0;
                    foreach (var relatedDebt in relatedDebts)
                    {
                        if (relatedDebt.DebtorId == debt.DebtorId)
                        {
                            netAmount += relatedDebt.Amount;
                        }
                        else
                        {
                            netAmount -= relatedDebt.Amount;
                        }
                        relatedDebt.IsPaid = true;
                        relatedDebt.IsPending = false;
                        relatedDebt.IsConfirmed = true;
                        relatedDebt.UpdatedAt = DateTime.Now;
                    }

                    var notifications = entities.Notifications
                           .Where(n => n.RelatedDebtId == debt.DebtId && n.IsRead != true)
                        .ToList();

                    foreach (var notification in notifications)
                    {
                        notification.IsRead = true;
                    }

                    SendNotification(debt.DebtorId, debt.GroupId, "PaymentReceived", false, $"你支付的 {netAmount} 元已被 {creditorName} 確認收到", debt.DebtId);
                    TempData["Message"] = "付款已經確認";
                    SendNotification(debt.CreditorId, debt.GroupId, "PaymentAccepted", false, $"你已收到來自 {debtorName} 的 {netAmount} 元", debt.DebtId); // 發送郵件通知
                    if (!string.IsNullOrEmpty(debtorEmail))
                    {
                        SendResetEmail(debtorEmail, $"付款已確認 , 你支付的 {netAmount} 元已被 {creditorName} 確認收到。");
                    }
                    if (!string.IsNullOrEmpty(creditorEmail))
                    {
                        SendResetEmail(creditorEmail, $"已收到付款, 你已收到來自 {debtorName} 的 {netAmount} 元。");
                    }
                }
                else
                {
                    debt.IsPaid = false;
                    debt.IsPending = false;
                    debt.UpdatedAt = DateTime.Now;

                    var notifications = entities.Notifications
                         .Where(n => n.RelatedDebtId == debt.DebtId && n.IsRead != true)
                        .ToList();

                    foreach (var notification in notifications)
                    {
                        notification.IsRead = true;
                    }

                    SendNotification(debt.DebtorId, debt.GroupId, "PaymentDisputed", false, $"{creditorName} 拒絕確認你支付的 {debt.Amount} 元，請聯繫對方", debt.DebtId);
                    TempData["Message"] = "你已經拒絕了對方的付款";    // 發送郵件通知
                    if (!string.IsNullOrEmpty(debtorEmail))
                    {
                        SendResetEmail(debtorEmail, $"付款遭拒絕 , {creditorName} 拒絕了你支付的 {debt.Amount} 元，請聯繫對方。");
                    }
                    if (!string.IsNullOrEmpty(creditorEmail))
                    {
                        SendResetEmail(creditorEmail, $"你拒絕了{debtorName}支付的 {debt.Amount} 元，");
                    }
                }


                entities.SaveChanges();
            }
            else
            {
                TempData["ErrorMessage"] = "找不到債務紀錄";
            }
            return RedirectToAction("Notifications", "Group");
        }

      
        [HttpPost]
        public ActionResult SendReminder(int debtorId, int creditorId, decimal amount, int groupId, bool roundOff)
        {
            var groupname = entities.Groups.Where(d => d.GroupId == groupId).FirstOrDefault().GroupName;
            if (roundOff)
            {
                amount = Math.Round(amount);
            }

            var debts = entities.Debts
                .Where(d => d.DebtorId == debtorId && d.CreditorId == creditorId && !d.IsPaid)
                .ToList();

            if (debts != null && debts.Any())
            {
                var debt = debts.First();
                if (debt.LastRemindTime == null || (DateTime.Now - debt.LastRemindTime.Value).TotalMinutes >= 1)
                {
                    debt.LastRemindTime = DateTime.Now;
                    entities.SaveChanges();

                    var debtorName = entities.Users.FirstOrDefault(u => u.UserId == debtorId)?.FullName;
                    var creditorName = entities.Users.FirstOrDefault(u => u.UserId == creditorId)?.FullName; 
                    var debtorEmail = entities.Users.FirstOrDefault(u => u.UserId == debt.DebtorId)?.Email;
                    var creditorEmail = entities.Users.FirstOrDefault(u => u.UserId == debt.CreditorId)?.Email;
                    SendNotification(debt.DebtorId, groupId, "Payment Reminder", false, $"{creditorName} 提醒你支付 {amount} 元", debt.DebtId);
     
                    SendNotification(debt.CreditorId, groupId, "Reminder Sent", true, $"你已提醒 {debtorName} 支付 {amount} 元", debt.DebtId);
          
                    TempData["SuccessMessage"] = "提醒已發送";    // 發送郵件通知
                   if (!string.IsNullOrEmpty(debtorEmail))
                    {
                        SendResetEmail(debtorEmail, $"{creditorName}提醒您需要支付{amount}元。By{groupname}");
                    }
                    if (!string.IsNullOrEmpty(creditorEmail))
                    {
                        SendResetEmail(creditorEmail, $"提醒已確認 , 你提醒了{debtorEmail}支付{amount}元已寄送至對方的信箱。By{groupname}");
                    }

                }
                else
                {
                  
                    TempData["ErrorMessage"] = "你已經發送過提醒請再測試";
                }
            }

            return RedirectToAction("PairwiseSettlement", new { groupId });
        }

        private void SendNotification(int userId, int groupId, string type, bool isRead, string message, int? relatedDebtId = null)
        {
            var notification = new Notifications
            {
                UserId = userId,
                GroupId = groupId,
                NotificationType = type,
                Message = message,
                IsRead = isRead,
                Date = DateTime.Now,
                RelatedDebtId = relatedDebtId
            };
            entities.Notifications.Add(notification);
            entities.SaveChanges();
        }


    }
}