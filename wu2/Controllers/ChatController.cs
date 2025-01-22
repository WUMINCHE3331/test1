using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using wu2.Models;
using System.IO;
using System.Text.RegularExpressions;

namespace wu2.Controllers
{
    public class ChatController : Controller
    {
        private wuEntities1 db = new wuEntities1();

        public ActionResult GetGroupMembers(int groupId)
        {
            var members = db.GroupMembers
                            .Where(gm => gm.GroupId == groupId)
                            .Select(gm => new { gm.Users.FullName })
                            .ToList();
            return Json(members, JsonRequestBehavior.AllowGet);
        }
        [HttpPost]
        public ActionResult SaveMessage(int? groupId, int? userId, string content)
        {
            try
            {
                if (string.IsNullOrEmpty(content))
                {
                    throw new ArgumentNullException("content", "Content cannot be null or empty.");
                }

                var currentUserId = (int)Session["UserID"];
                if (!groupId.HasValue && !userId.HasValue)
                {
                    throw new ArgumentException("Either groupId or userId must be provided.");
                }

                var newMessage = new Messages
                {
                    GroupId = groupId, // This can be null
                    UserId = currentUserId,
                    ReceiverId = userId, // This can be null
                    Content = content,
                    CreatedAt = DateTime.Now
                };

                db.Messages.Add(newMessage);
                db.SaveChanges();

                // Initialize MessageReadStatus
                if (groupId.HasValue)
                {
                    var groupUsers = db.GroupMembers
                                       .Where(gm => gm.GroupId == groupId && gm.UserId != currentUserId)
                                       .Select(gm => gm.UserId)
                                       .ToList();

                    foreach (var users in groupUsers)
                    {
                        var messageReadStatus = new MessageReadStatus
                        {
                            MessageId = newMessage.MessageId,
                            UserId = users,
                            IsRead = false,
                            ReadAt = null
                        };
                        db.MessageReadStatus.Add(messageReadStatus);
                    }
                }
                else if (userId.HasValue)
                {
                    var receiverReadStatus = new MessageReadStatus
                    {
                        MessageId = newMessage.MessageId,
                        UserId = userId.Value,
                        IsRead = false,
                        ReadAt = null
                    };
                    db.MessageReadStatus.Add(receiverReadStatus);
                }

                var senderReadStatus = new MessageReadStatus
                {
                    MessageId = newMessage.MessageId,
                    UserId = currentUserId,
                    IsRead = true,
                    ReadAt = DateTime.Now
                };
                db.MessageReadStatus.Add(senderReadStatus);
                db.SaveChanges();

                var user = db.Users.FirstOrDefault(u => u.UserId == currentUserId);
                var userPhoto = user?.ProfilePhoto ?? "~/UserProfilePhotos/defaultPerson.jpg";
                var userName = user?.FullName ?? "Unknown";

                return Json(new { success = true, message = "Message saved successfully", data = new { CreatedAt = newMessage.CreatedAt.ToString("o"), UserName = userName, UserPhoto = userPhoto } });
            }
            catch (Exception ex)
            {
                var baseException = ex.GetBaseException();
                var errorMessage = $"Error saving message: {baseException.Message}";
                Console.WriteLine($"groupId: {groupId}, userId: {userId}, content: {content}, currentUserId: {Session["UserID"]}");
                Console.WriteLine(errorMessage);
                Console.WriteLine($"Stack Trace: {baseException.StackTrace}");
                return Json(new { success = false, message = errorMessage });
            }
        }




        [HttpPost]
        public ActionResult MarkAllPersonalMessagesAsRead(int userId)
        {
            try
            {
                int currentUserId = (int)Session["UserID"];

                var personalUnreadMessages = db.MessageReadStatus
                    .Where(mrs => mrs.Messages.UserId == userId
                               && mrs.Messages.ReceiverId == currentUserId
                               && mrs.IsRead == false
                               && mrs.Messages.UserId != currentUserId)
                    .ToList();

                foreach (var readStatus in personalUnreadMessages)
                {
                    readStatus.IsRead = true;
                    readStatus.ReadAt = DateTime.Now;
                }

                db.SaveChanges();
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                var baseException = ex.GetBaseException();
                var errorMessage = $"Error marking all personal messages as read: {baseException.Message}";
                Console.WriteLine(errorMessage);
                Console.WriteLine($"Stack Trace: {baseException.StackTrace}");
                return Json(new { success = false, message = errorMessage });
            }
        }

        [HttpPost]
        public ActionResult MarkAllMessagesAsRead(int? groupId, int? userId)
        {
            try
            {
                int currentUserId = (int)Session["UserID"];

         
                var groupUnreadMessages = db.MessageReadStatus
                    .Where(mrs => mrs.Messages.GroupId == groupId
                               && mrs.UserId == currentUserId
                               && mrs.IsRead == false
                               && mrs.Messages.UserId != currentUserId)
                    .ToList();

                foreach (var readStatus in groupUnreadMessages)
                {
                    readStatus.IsRead = true;
                    readStatus.ReadAt = DateTime.Now;
                }

               
                var personalUnreadMessages = db.MessageReadStatus
                    .Where(mrs => mrs.Messages.UserId == userId
                               && mrs.Messages.ReceiverId == currentUserId
                               && mrs.IsRead == false
                               && mrs.Messages.UserId != currentUserId)
                    .ToList();

                foreach (var readStatus in personalUnreadMessages)
                {
                    readStatus.IsRead = true;
                    readStatus.ReadAt = DateTime.Now;
                }

                db.SaveChanges();
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                var baseException = ex.GetBaseException();
                var errorMessage = $"Error marking all messages as read: {baseException.Message}";
                Console.WriteLine(errorMessage);
                Console.WriteLine($"Stack Trace: {baseException.StackTrace}");
                return Json(new { success = false, message = errorMessage });
            }
        }

        public ActionResult Chat()
        {
            if (Session["UserID"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var userId = (int)Session["UserID"];
            var groupContacts = db.GroupMembers
                .Where(gm => gm.UserId == userId)
                .Select(gm => new GroupContactViewModel
                {
                    GroupId = gm.GroupId,
                    GroupsPhoto = gm.Groups.GroupsPhoto,
                    GroupName = gm.Groups.GroupName.Trim()
                })
                .Distinct()
                .ToList();

            var users = db.Users.ToDictionary(u => u.UserId.ToString(), u => u.FullName);
            var model = new ChatViewModel
            {
                Users = users,
                GroupContacts = groupContacts
            };

            ViewBag.ChatViewModel = model;
            return View(model);
        }

        [HttpGet]
        public JsonResult GetTotalUnreadCount()
        {
            if (Session["UserID"] == null)
            {
                return Json(new { success = false, message = "User not logged in" }, JsonRequestBehavior.AllowGet);
            }

            int userId = (int)Session["UserID"];
            int totalUnreadCount = db.MessageReadStatus
                .Where(mrs => mrs.UserId == userId && mrs.IsRead == false)
                .Count();

            return Json(new { success = true, totalUnreadCount = totalUnreadCount }, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult GetUserChatData()
        {
            if (Session["UserID"] == null)
            {
                return Json(new { success = false, message = "User not logged in" }, JsonRequestBehavior.AllowGet);
            }

            var userId = (int)Session["UserID"];
            var groupContacts = db.GroupMembers
                .Where(gm => gm.UserId == userId)
                .Select(gm => new GroupContactViewModel
                {
                    GroupId = gm.GroupId,
                    GroupsPhoto = gm.Groups.GroupsPhoto ?? "~/UserProfilePhotos/defaultPerson.jpg",
                    GroupName = gm.Groups.GroupName.Trim(),
                    UnreadCount = gm.Groups.Messages
                        .SelectMany(m => m.MessageReadStatus)
                        .Count(mrs => mrs.UserId == userId && mrs.IsRead == false).ToString()
                })
                .Distinct()
                .ToList();

            // 获取个人聊天信息
            var groupMemberIds = db.GroupMembers
                .Where(gm => gm.GroupId != null && gm.UserId == userId)
                .Select(gm => gm.GroupId)
                .Distinct()
                .ToList();

            var personalContacts = db.GroupMembers
                .Where(gm => groupMemberIds.Contains(gm.GroupId) && gm.UserId != userId)
                .Select(gm => new PersonalContactViewModel
                {
                    UserId = gm.UserId,
                    UserName = gm.Users.FullName,
                    UserPhoto = gm.Users.ProfilePhoto ?? "~/UserProfilePhotos/defaultPerson.jpg",
                    UnreadCount = db.Messages
                        .Where(pm => pm.UserId == gm.UserId && pm.GroupId == null)
                        .SelectMany(pm => pm.MessageReadStatus)
                        .Count(pmrs => pmrs.UserId == userId && pmrs.IsRead == false).ToString()
                })
                .Distinct()
                .ToList();

            var users = db.Users.ToDictionary(u => u.UserId.ToString(), u => u.FullName);

            return Json(new { success = true, groupContacts = groupContacts, personalContacts = personalContacts, users = users }, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult LoadMessages(int groupId)
        {
            var messages = db.Messages
                .Where(m => m.GroupId == groupId)
                .OrderBy(m => m.CreatedAt).ToList()
                .Select(m => new
                {
                    m.UserId,
                    m.Content,
                    UserName = m.Users.FullName,
                    UserPhoto = m.Users.ProfilePhoto,
                    CreatedAt = m.CreatedAt.ToString("o"),
                    ReadCount = m.MessageReadStatus.Count(mrs => mrs.IsRead == true) - 1
                });


            return Json(messages, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult LoadPersonalMessages(int userId)
        {
            int currentUserId = (int)Session["UserID"];

            var messages = db.Messages
                .Where(m => (m.UserId == currentUserId && m.ReceiverId == userId) || (m.UserId == userId && m.ReceiverId == currentUserId))
                .OrderBy(m => m.CreatedAt).ToList()
                .Select(m => new
                {
                    m.UserId,
                    m.Content,
                    SenderName = m.Users.FullName,
                    CreatedAt = m.CreatedAt.ToString("o"),
                    ReadCount = m.MessageReadStatus.Count(mrs => mrs.IsRead.Value) - 1
                })
                ;

            return Json(messages, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public ActionResult UploadFile(HttpPostedFileBase file, int groupId)
        {
            if (file != null && file.ContentLength > 0)
            {
                var fileName = Path.GetFileName(file.FileName);
                var path = Path.Combine(Server.MapPath("~/UploadedFiles"), fileName);
                file.SaveAs(path);

                var fileUrl = Url.Content("~/UploadedFiles/" + fileName);
                var userId = (int)Session["UserID"];
                var userName = db.Users.Where(u => u.UserId == userId).Select(u => u.FullName).FirstOrDefault();
                var message = new Messages
                {
                    GroupId = groupId,
                    UserId = userId,
                    Content = fileUrl,
                    CreatedAt = DateTime.Now
                };
                db.Messages.Add(message);
                db.SaveChanges();

                return Json(new { success = true, fileUrl = fileUrl, data = new { UserName = userName } });
            }
            return Json(new { success = false, message = "No file uploaded." });
        }

        private int GetUserId()
        {
            if (Session["UserID"] == null)
            {
                RedirectToAction("Login", "Account");
            }

            return (int)Session["UserId"];
        }
    }
}
