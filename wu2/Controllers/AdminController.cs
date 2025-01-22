using System;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using wu2.Models;

namespace wu2.Controllers
{
    public class AdminController : Controller
    {
        private wuEntities1 entities = new wuEntities1();

        public ActionResult AdminIndex()
        {
            if (Session["UserID"] == null || !IsAdmin())
            {
                return RedirectToAction("Login", "Account");
            }

            var groups = entities.Groups.Include("GroupMembers").Include("Expenses").ToList();

            var model = new AdminIndexViewModel
            {
                Groups = groups
            };

            return View(model);
        }

        // 顯示所有成員
        public ActionResult Members()
        {
            if (Session["UserID"] == null || !IsAdmin())
            {
                return RedirectToAction("Login", "Account");
            }

            var members = entities.Users.ToList();
            return View(members);
        }

        // 顯示所有群組
        public ActionResult Groups()
        {
            if (Session["UserID"] == null || !IsAdmin())
            {
                return RedirectToAction("Login", "Account");
            }

            var groups = entities.Groups.ToList();
            return View(groups);
        }

    
        // 顯示群組詳細資訊
        public ActionResult GroupDetails(int id)
        {
            if (Session["UserID"] == null || !IsAdmin())
            {
                return RedirectToAction("Login", "Account");
            }

            var group = entities.Groups
                .Include("GroupMembers.Users")
                .Include("Expenses")
                .FirstOrDefault(g => g.GroupId == id);

            if (group == null)
            {
                return HttpNotFound();
            }

            return View(group);
        }

        public ActionResult GetLatestAnnouncement()
        {
            var announcement = entities.Notices
                .Where(a => a.Users.Role == "root")
                .OrderByDescending(a => a.CreatedAt)
                .FirstOrDefault();

            return PartialView("_LatestAnnouncement", announcement);
        }

        // 發布公告
        [HttpGet]
        public ActionResult CreateAnnouncement()
        {
            if (Session["UserID"] == null || !IsAdmin())
            {
                return RedirectToAction("Login", "Account");
            }

         
            return View(new Notices());
        }

        [HttpPost]
        public ActionResult CreateAnnouncement(Notices model)
        {
            if (Session["UserID"] == null || !IsAdmin())
            {
                return RedirectToAction("Login", "Account");
            }

            if (ModelState.IsValid)
            {
                model.CreatedAt = DateTime.Now;
                model.CreatedBy = GetCurrentUser().UserId; 

              
                var existingRootAnnouncement = entities.Notices.FirstOrDefault(a => a.Users.Role == "root");
                if (existingRootAnnouncement != null)
                {
                    entities.Notices.Remove(existingRootAnnouncement);
                }

              
                entities.Notices.Add(model);
                entities.SaveChanges();

                return RedirectToAction("AdminIndex", "Admin");
            }

        
            return View(model);
        }
        [HttpPost]
        public ActionResult DeleteAnnouncement()
        {
            if (Session["UserID"] == null || !IsAdmin())
            {
                return RedirectToAction("Login", "Account");
            }

       
            var existingAnnouncement = entities.Notices.FirstOrDefault();
            if (existingAnnouncement != null)
            {
                entities.Notices.Remove(existingAnnouncement);
                entities.SaveChanges();
            }

            return RedirectToAction("AdminIndex", "Admin");
        }

        private bool IsAdmin()
        {
            int userId = (int)Session["UserID"];
            var user = entities.Users.Find(userId);
            return user != null && user.Role == "Root";
        }

        // 獲取當前用戶
        private Users GetCurrentUser()
        {
            int userId = (int)Session["UserID"];
            return entities.Users.Find(userId);
        }


        
        public ActionResult AddMember(int groupId)
        {
            var group = entities.Groups.Find(groupId);
            if (group == null)
            {
                return HttpNotFound();
            }

            ViewBag.GroupId = groupId;
            return View();
        }

       
        [HttpPost]
        public ActionResult AddMember(int groupId, string email, string fullName)
        {
            try
            {
                var group = entities.Groups.Find(groupId);
                if (group == null)
                {
                    return HttpNotFound();
                }
           
                var user = entities.Users.FirstOrDefault(u => u.Email == email);
                if (user == null)
                {
           
                    string uniqueName = string.IsNullOrWhiteSpace(fullName) ? "BOT!!" + Guid.NewGuid().ToString("N").Substring(0, 3) : fullName+"虛擬";

               
                    user = new Users
                    {
                        Email = "@"+Guid.NewGuid(),
                        PasswordHash = "1",
                        FullName = uniqueName
                    
                    };
                    entities.Users.Add(user);
                    entities.SaveChanges();
                }

                bool isMember = entities.GroupMembers.Any(gm => gm.GroupId == groupId && gm.UserId == user.UserId);
                if (isMember)
                {
                    TempData["ErrorMessage"] = "此信箱組員已經在群組";
                }
                else
                {
                   
                    entities.GroupMembers.Add(new GroupMembers { GroupId = groupId, UserId = user.UserId, Role = "Editor" ,JoinedDate = DateTime.Now});
                    entities.SaveChanges();
                    TempData["SuccessMessage"] = "組員加入成功";
                }
            }
            catch (Exception ex)
            {
                var errorMessage = $"Error adding member: {ex.Message}";
                Console.WriteLine(errorMessage);
                TempData["ErrorMessage"] = errorMessage;
            }

            return RedirectToAction("AddMember", new { groupId = groupId });
        }
    }
}
