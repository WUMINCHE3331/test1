using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Mvc;
using wu2.Models;

namespace wu2.Controllers
{
    public class NoticeController : Controller
    {
        // GET: Notice

        wuEntities1 Entities = new wuEntities1();
        public ActionResult Index()
        {
            var notices = Entities.Notices.OrderByDescending(n => n.CreatedAt).ToList();
            ViewBag.CurrentNotices = notices.Where(n => n.IsCurrent == true).ToList();
            ViewBag.HistoryNotices = notices.Where(n => n.IsCurrent != true).ToList();
            return View();
        }
        [HttpGet]
        public ActionResult CreateNotice(int? groupId)
        {
            if (Session["UserID"] == null || groupId==null)
            {
                return RedirectToAction("Login", "Account");
            }
            if (groupId == null)
            {
                return RedirectToAction("Details", "Group");
            }    
            var IsCreator = Entities.Groups.FirstOrDefault(g => g.GroupId == groupId);
            bool hasFullPermission = IsCreator.CreatorId == (int)Session["UserID"];
            ViewBag.HasFullPermission = hasFullPermission;
            ViewBag.GroupId = groupId;
            return View(new Notices());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreateNotice(Notices notice)
        {
        
            if (ModelState.IsValid)
            {
                notice.CreatedAt = DateTime.Now;
                notice.CreatedBy = (int)Session["UserID"];
                notice.IsCurrent = true;

                var allNotices = Entities.Notices.Where(n => n.GroupId == notice.GroupId).ToList();
                foreach (var n in allNotices)
                {
                    n.IsCurrent = false;
                }

                Entities.Notices.Add(notice);
                Entities.SaveChanges();

                int groupId = notice.GroupId ?? 0;
                ViewBag.GroupId = groupId;
                return RedirectToAction("Details", "Group", new { id = groupId });
            }
            return View(notice);
        }



        public ActionResult EditNotice(int? id)
        {
            if (Session["UserID"] == null || id == null)
            {
                return RedirectToAction("Login", "Account");
            }
            var notice = Entities.Notices.Find(id);
            if (notice == null)
            {
                return HttpNotFound();
            }
            var group = Entities.Groups.FirstOrDefault(g => g.GroupId == notice.GroupId);
            bool hasFullPermission = group.CreatorId == (int)Session["UserID"];
            ViewBag.HasFullPermission = hasFullPermission;
            return View(notice);
        }
        [HttpPost]
        public ActionResult EditNotice(Notices notice)
        {
            if (ModelState.IsValid)
            {
                var existingNotice = Entities.Notices.Find(notice.NoticeId);
                if (existingNotice != null)
                {
                    existingNotice.Title = notice.Title;
                    existingNotice.Content = notice.Content;
                

                    Entities.Entry(existingNotice).State = System.Data.Entity.EntityState.Modified;
                    Entities.SaveChanges();

                    int groupId = notice.GroupId ?? 0;
                    return RedirectToAction("Details", "Group", new { id = groupId });
                }
                return HttpNotFound();
            }
            return View(notice);
        }

        [HttpPost]
        public ActionResult DeleteNotice(int id)
        {
            // 从数据库中获取公告对象
            var notice = Entities.Notices.Find(id);

            // 检查公告对象是否存在
            if (notice == null)
            {
                TempData["ErrorMessage"] = "找不到相關公告紀錄";
                // 使用硬编码的GroupId重定向到群组列表
                return RedirectToAction("MyGroups", "Group");
            }

            // 执行删除操作
            int groupId = notice.GroupId ?? 0;
            Entities.Notices.Remove(notice);
            Entities.SaveChanges();

            TempData["SuccessMessage"] = "公告已刪除";
            return RedirectToAction("Details", "Group", new { id = groupId });
        }


    }
}