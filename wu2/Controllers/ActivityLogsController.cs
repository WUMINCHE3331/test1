using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using wu2.Models;
namespace wu2.Controllers
{
    public class ActivityLogsController : Controller
    {
        // GET: ActivityLogs

        wuEntities1 db = new wuEntities1();
        public ActionResult Index(int? groupId)
        {
            if (Session["UserID"] == null || groupId == null)
            {
                return RedirectToAction("Login", "Account");
            }
            ViewBag.GroupId = groupId;
            var logs = db.ActivityLogs
                .Where(a => a.GroupId == groupId)
                .OrderByDescending(a => a.Date)
                .ToList();

            return View(logs);
        }
    }
}