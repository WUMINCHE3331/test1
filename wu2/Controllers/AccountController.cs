using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Net;
using System.Web;
using System.Web.Mvc;
using wu2.Models;
using System.Configuration;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Text;
using System.Net.Http;
using Newtonsoft.Json;
using System.Diagnostics;


namespace wu2.Controllers
{
    public class AccountController : Controller
    {
        wuEntities1 db = new wuEntities1();
        // GET: Account


        public AccountController()
        {
            try
            {
                db.Database.Connection.Open();
                db.Database.Connection.Close();
            }
            catch (Exception ex)
            {
                // 记录异常日志
                Console.WriteLine("数据库连接失败: " + ex.Message);
                throw new InvalidOperationException("数据库连接失败，请检查连接字符串配置。");
            }
        }
        public ActionResult CheckSession()
        {
            if (Session["UserID"] == null)
            {
                return Json(new { isLoggedIn = false }, JsonRequestBehavior.AllowGet);  // 返回未登录状态
            }

            // 用户已登录
            return Json(new { isLoggedIn = true }, JsonRequestBehavior.AllowGet);
        }

        public ActionResult Register(string joinLink = null)
        {
            ViewBag.JoinLink = joinLink;
            return View(new Users());
        }
        [HttpPost]
        public ActionResult Register(Users model, string confirmPassword, string joinLink = null)
        {
            if (model.PasswordHash != confirmPassword)
            {
                ModelState.AddModelError("", "密碼輸入不匹配"); ViewBag.JoinLink = joinLink; // 保留 joinLink
                return View(model);
            }

            if (db.Users.Any(u => u.Email == model.Email))
            {
                ViewBag.JoinLink = joinLink; // 保留 joinLink
                ModelState.AddModelError("", "信箱已存在");
                return View(model);
            }

            Users newUser = new Users
            {
                Email = model.Email,
                PasswordHash = model.PasswordHash,
                FullName = model.FullName,
                //PhoneNumber = model.PhoneNumber,
                Role = "Member", // Default role
                RegistrationDate = DateTime.Now
            };

            db.Users.Add(newUser);
            db.SaveChanges();
            TempData["SuccessMessage"] = "註冊成功！";
            Session["UserId"] = newUser.UserId;
            Session["Email"] = newUser.Email;
            Session["FullName"] = newUser.FullName;
            Session["Role"] = newUser.Role;
            Session["ProfilePhotoPath"] = newUser.ProfilePhoto;
            if (!string.IsNullOrEmpty(joinLink))
            {
                var group = db.Groups.FirstOrDefault(g => g.JoinLink == joinLink);
                if (group == null)
                {
                    // 如果 joinLink 無效，將錯誤訊息存入 TempData
                    TempData["ErrorMessage"] = "無效的邀請連結。";
                    return RedirectToAction("Index", "Home"); // 重定向到首頁或其他頁面
                }

                // 如果 joinLink 有效，重定向到 JoinGroup 動作
                return RedirectToAction("JoinGroup", "Group", new { joinLink });
            }
            return RedirectToAction("Index", "Home");
        }
        [HttpPost]
        public ActionResult SetSession(UserProfileViewModel model)
        {
            // 假設這些欄位來自前端的資料
            Session["UserId"] = model.UserId;
            Session["FullName"] = model.DisplayName;
            Session["Email"] = model.Email;
            Session["ProfilePhotoPath"] = model.ProfilePhoto;
            Session["Role"] = model.Role;
            return Json(new { success = true });
        }
        public ActionResult DisplaySession()
        {
            var sessionData = new
            {
                UserId = Session["UserId"],
                Email = Session["Email"],
                FullName = Session["FullName"],
                Role = Session["Role"],
                ProfilePhotoPath = Session["ProfilePhotoPath"]
            };

            return Json(sessionData, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public ActionResult HandleLiffLogin()
        {
            Trace.WriteLine("HandleLiffLogin was called!");
            try
            {
                // 1. 讀取請求中的 JSON 數據
                string requestBody;
                using (var reader = new StreamReader(Request.InputStream))
                {
                    requestBody = reader.ReadToEnd();
                }

                // 2. 反序列化 JSON 到 User 物件
                var user = JsonConvert.DeserializeObject<Users>(requestBody);

                // 3. 檢查反序列化結果
                if (user != null)
                {
                    // 4. 查詢資料庫中是否已有該用戶的資料
                    var existingUser = db.Users.FirstOrDefault(u => u.LineUserId == user.LineUserId);

                    if (existingUser == null)
                    {
                        // 5. 如果沒有該用戶，創建新用戶
                        db.Users.Add(user);
                        db.SaveChanges();

                        // 在保存后需要重新獲取剛保存的用戶以獲取新的 UserId
                        existingUser = db.Users.FirstOrDefault(u => u.LineUserId == user.LineUserId);
                    }
                    else
                    {
                        // 6. 如果已有該用戶，更新資料
                        existingUser.FullName = user.FullName ?? existingUser.FullName;
                        existingUser.Email = user.Email ?? existingUser.Email;
                        existingUser.ProfilePhoto = user.ProfilePhoto ?? existingUser.ProfilePhoto;
                        db.SaveChanges();
                    }

                    // 7. 使用 existingUser 的 UserId 來設置 Session
                    Session["UserId"] = existingUser.UserId;
                    Session["Email"] = existingUser.Email;
                    Session["FullName"] = existingUser.FullName;
                    Session["ProfilePhotoPath"] = existingUser.ProfilePhoto;
                    Session["Role"] = "LiffMember";  // 假設角色為普通用戶

                    // 8. 返回成功的結果
                    return Json(new { success = true });
                }
                else
                {
                    // 反序列化失敗，返回錯誤結果
                    return Json(new { success = false, message = "Invalid user data" });
                }
            }
            catch (Exception ex)
            {
                // 捕獲任何錯誤，返回失敗結果
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]

        public ActionResult Login(string joinLink = null)
        {
            var model = new LoginViewModel();

            if (!string.IsNullOrEmpty(joinLink))
            {
                ViewBag.JoinLink = joinLink;
            }

            return View(model);
        }


        [HttpPost]
        public ActionResult Login(LoginViewModel model, string JoinLink = null)
        {
            // 檢查模型是否為空或無效
            if (model == null || string.IsNullOrEmpty(model.Email) || string.IsNullOrEmpty(model.PasswordHash))
            {
                ModelState.AddModelError("", "請輸入正確的帳號和密碼。");
                return View(model);
            }

            // 查找使用者是否存在
            var user = db.Users.FirstOrDefault(u => u.Email == model.Email && u.PasswordHash == model.PasswordHash);

            // 如果使用者不存在，則顯示錯誤消息
            if (user == null)
            {
                ViewBag.JoinLink = JoinLink; // 將 JoinLink 傳遞回視圖
                ModelState.AddModelError("", "帳號或密碼輸入錯誤.");
                return View(model); // 將 model 傳回給視圖以顯示錯誤
            }

            // 使用 Session 儲存使用者資訊
            Session["UserId"] = user.UserId;
            Session["Email"] = user.Email;
            Session["FullName"] = user.FullName;
            Session["Role"] = user.Role;
            Session["ProfilePhotoPath"] = user.ProfilePhoto;
            Console.WriteLine(Session["UserId"]);
            Console.WriteLine(Session["FullName"]);
            // 如果存在 JoinLink，則重定向到 JoinGroup 動作處理加入群組邏輯
            if (!string.IsNullOrEmpty(JoinLink))
            {
                var group = db.Groups.FirstOrDefault(g => g.JoinLink == JoinLink);
                if (group == null)
                {
                    // 如果 joinLink 無效，將錯誤訊息存入 TempData
                    TempData["ErrorMessage"] = "無效的邀請連結，請檢查是否有錯誤";
                    return RedirectToAction("Index", "Home"); // 重定向到首頁或其他頁面
                }

                // 如果 joinLink 有效，重定向到 JoinGroup 動作
                return RedirectToAction("JoinGroup", "Group", new { JoinLink });
            }

            // 根據使用者角色進行重定向
            if (user.Role == "Root")
            {
                return RedirectToAction("AdminIndex", "Admin");
            }

            return RedirectToAction("Index", "Home"); // 普通使用者重定向到首頁
        }


        public ActionResult Logout()
        {
            Session.Clear();
            return RedirectToAction("Login");
        }
        public ActionResult Update(int? userId)
        {
            // 检查用户是否已登录
            if (Session["UserID"] == null)
            {
                // 如果未登录，重定向到登录页面
                return RedirectToAction("Login", "Account");
            }

            // 如果未傳遞 userId，則使用當前登入的使用者 ID
            var currentUserId = (int)Session["UserID"];
            userId = userId ?? currentUserId;
            // 从数据库中查找用户
            var user = db.Users.Find(userId); var currentUserRole = Session["Role"] as string;
            if (user == null)
            {
                // 如果用户未找到，返回404错误
                return HttpNotFound();
            }

            // 将Users实体转换为UserViewModel
            var model = new UserViewModel
            {
                UserId = user.UserId,
                Email = user.Email,
                FullName = user.FullName,
                ProfilePhoto = user.ProfilePhoto,
                OldPassword = (currentUserRole == "Root" || userId == currentUserId) ? user.PasswordHash : null,
                BankAccount = user.BankAccount,
                PhoneNumber = user.PhoneNumber,
                RegistrationDate = user.RegistrationDate,
                // 根据需要映射其他属性
            };

            // 将视图模型传递给视图
            return View(model);
        }


        [HttpPost]
        public ActionResult Update(UserViewModel model, HttpPostedFileBase ProfilePhoto)
        {
            if (Session["UserID"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var userId = (int)Session["UserID"];
            var user = db.Users.Find(userId);

            if (user == null)
            {
                TempData["ErrorMessage"] = "User not found.";
                return View(model);
            }

            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Please fix the errors.";
                return View(model);
            }

            // 更新用戶資料
            user.FullName = model.FullName;
            user.PhoneNumber = model.PhoneNumber;
            user.BankAccount = model.BankAccount;

            if (!string.IsNullOrEmpty(model.NewPassword) && model.NewPassword == model.ConfirmPassword)
            {
                user.PasswordHash = (model.NewPassword); // 假設你有一個 HashPassword 方法
            }
            else if (model.NewPassword != model.ConfirmPassword)
            {
                TempData["ErrorMessage"] = "Passwords do not match.";
                return View(model);
            }
            // 图片处理逻辑
            if (ProfilePhoto != null && ProfilePhoto.ContentLength > 0)
            {
                var fileName = Path.GetFileName(ProfilePhoto.FileName);
                var path = Path.Combine(Server.MapPath("~/images"), fileName);
                ProfilePhoto.SaveAs(path);
                user.ProfilePhoto = "~/images/" + fileName;
            }
            Session["ProfilePhotoPath"] = user.ProfilePhoto;
            Session["Username"] = user.FullName;

            db.SaveChanges();
            TempData["SuccessMessage"] = "Profile updated successfully.";

            return RedirectToAction("Update");
        }

        ///個人資訊
        public ActionResult UserProfile()
        {

            // 檢查用戶是否已登錄
            if (Session["UserID"] == null)
            {
                return RedirectToAction("Login", "Account");
            }
            int userId = (int)Session["UserID"];
            Users user = db.Users.Find(userId);
            return View(user);
        }

        [HttpGet]
        public ActionResult EditUserProfile()
        {
            if (Session["UserID"] == null)
            {
                return RedirectToAction("Login", "Account");
            }
            int userId = (int)Session["UserID"];
            Users user = db.Users.Find(userId);
            return View(user);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditUserProfile(Users model, HttpPostedFileBase ProfilePhoto)
        {       // 檢查用戶是否已登錄
            if (Session["UserID"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            if (ModelState.IsValid)
            {
                //檢查用戶ID
                int userId = (int)Session["UserID"];

                //檢索個人資料
                Users user = db.Users.Find(userId);

                //更新資料
                user.FullName = model.FullName;

                user.BankAccount = model.BankAccount;
                user.PhoneNumber = model.PhoneNumber;


                // 上傳照片
                if (ProfilePhoto != null && ProfilePhoto.ContentLength > 0)
                {
                    string fileName = Path.GetFileName(ProfilePhoto.FileName);
                    string path = Path.Combine(Server.MapPath("~/UserProfilePhotos"), fileName);
                    ProfilePhoto.SaveAs(path);

                    // 檢查文件是否成功上傳
                    if (System.IO.File.Exists(path))
                    {
                        // 成功
                        user.ProfilePhoto = "~/UserProfilePhotos/" + fileName;

                    }
                    else
                    {
                        //失敗
                        ViewBag.ErrorMessage = "上傳失敗";
                        return View(model); // 
                    }
                }

                // 將模型設置為修改狀態
                db.Entry(user).State = EntityState.Modified;

                // 保存db
                db.SaveChanges();
                Session["ProfilePhotoPath"] = user.ProfilePhoto;
                Session["FullName"] = user.FullName;
                // 重新定向
                return RedirectToAction("UserProfile", "Account");
            }


            //返回個人資料編輯
            return View(model);
        }


        [HttpGet]
        public ActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        public async Task<ActionResult> ForgotPassword(string email)
        {
            var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
            {
                ViewBag.ErrorMessage = "信箱尚未發現";
                return View();
            }

            string resetToken = Guid.NewGuid().ToString();
            user.PasswordResetToken = resetToken;
            user.TokenExpiration = DateTime.Now.AddHours(1);
            await db.SaveChangesAsync();

            string resetLink = Url.Action("ResetPassword", "Account", new { token = resetToken }, Request.Url.Scheme);
            SendResetEmail(user.Email, resetLink);

            TempData["SuccessMessage"] = "發送成功!";
            return RedirectToAction("ForgotPassword");
        }

        private void SendResetEmail(string email, string resetLink)
        {
            var fromAddress = new MailAddress(ConfigurationManager.AppSettings["SmtpUser"], "Your Company");
            var toAddress = new MailAddress(email);
            const string subject = "重設您的密碼";

            // HTML 格式的郵件內容
            string body = $@"
    <html>
        <head>
            <style>
                body {{
                    font-family: Arial, sans-serif;
                    background-color: #f4f4f4;
                    padding: 20px;
                    margin: 0;
                }}
                .container {{
                    max-width: 600px;
                    margin: 0 auto;
                    background-color: #ffffff;
                    padding: 30px;
                    border-radius: 10px;
                    box-shadow: 0 0 10px rgba(0, 0, 0, 0.1);
                }}
                .header {{
                    text-align: center;
                    margin-bottom: 20px;
                }}
                .header h1 {{
                    color: #333;
                }}
                .content {{
                    text-align: center;
                    margin: 20px 0;
                }}
                .btn {{
                    background-color: #76b1d8;
                    color: white;
                    padding: 15px 25px;
                    text-decoration: none;
                    border-radius: 5px;
                    font-size: 16px;
                    display: inline-block;
                    margin-top: 20px;
                }}
                .footer {{
                    text-align: center;
                    margin-top: 30px;
                    font-size: 12px;
                    color: #777;
                }}
                a {{
color:white;

                    }}
            </style>
        </head>
        <body>
            <div class='container'>
                <div class='header'>
                    <h1>重設您的密碼</h1>
                    <p>請點擊下方按鈕以重設密碼</p>
                </div>
                <div class='content'>
                    <a href='{resetLink}' class='btn'>重設密碼</a>
                </div>
                <div class='footer'>
                    <p>如果您未請求此操作，請忽略此郵件。</p>
                    <p>&copy; 2024 Your Company. All rights reserved.</p>
                </div>
            </div>
        </body>
    </html>";

            // 設定 SmtpClient
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

            // 建立 HTML 格式的郵件
            using (var message = new MailMessage(fromAddress, toAddress)
            {
                Subject = subject,
                Body = body,
                IsBodyHtml = true // 設為 true 以使用 HTML
            })
            {
                smtp.Send(message);
            }
        }
        [HttpGet]
        public ActionResult ResetPassword(string token)
        {
            var user = db.Users.FirstOrDefault(u => u.PasswordResetToken == token && u.TokenExpiration > DateTime.Now);
            if (user == null)
            {
                return RedirectToAction("ForgotPassword");
            }
            return View(user);
        }

        [HttpPost]
        public async Task<ActionResult> ResetPassword(Users model)
        {
            var user = await db.Users.FirstOrDefaultAsync(u => u.PasswordResetToken == model.PasswordResetToken && u.TokenExpiration > DateTime.Now);
            if (user == null)
            {
                ModelState.AddModelError("", "token過期");
                return View(model);
            }

            if (model.PasswordHash != model.ConfirmPassword)
            {
                ModelState.AddModelError("", "密碼不匹配");
                return View(model);
            }

            user.PasswordHash = model.PasswordHash;
            user.PasswordResetToken = null;
            user.TokenExpiration = null;
            await db.SaveChangesAsync();

            TempData["SuccessMessage"] = "發送成功!";
            return RedirectToAction("Login");
        }


        //private string HashPassword(string password)
        //{
        //    using (var sha256 = SHA256.Create())
        //    {
        //        var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
        //        return BitConverter.ToString(hashedBytes).Replace("-", "").ToLower();
        //    }
        //}
    }
}
