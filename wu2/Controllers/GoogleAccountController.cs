    using System;
    using System.Linq;
    using System.Security.Claims;
    using System.Threading.Tasks;
    using System.Web;
    using System.Web.Mvc;
    using Microsoft.AspNet.Identity;
    using Microsoft.Owin.Security;
    using Newtonsoft.Json.Linq;
    using wu2.Models;

    namespace wu2.Controllers
    {
        public class GoogleAccountController : Controller
        {
            wuEntities1 db = new wuEntities1();
            private IAuthenticationManager AuthenticationManager => HttpContext.GetOwinContext().Authentication;

        
            public void LoginWithGoogle()
            {
                // 触发 Google OAuth2 登录
                HttpContext.GetOwinContext().Authentication.Challenge(
                    new AuthenticationProperties { RedirectUri = "/" },
                    "Google");  // 注意这里使用字符串 "Google" 来表示 Google 登录
            }
        public async Task<ActionResult> ExternalLoginCallback(string returnUrl)
        {
            try
            {
                var loginInfo = await AuthenticationManager.GetExternalLoginInfoAsync();
                if (loginInfo == null)
                {
                    // 登录信息为空，可能是用户取消了登录或者认证过程出现了问题
                    TempData["Error"] = "登录信息为空，请重新尝试登录。";
                    return RedirectToAction("Login", "Account");
                }

                var email = loginInfo.Email;
                var googleId = loginInfo.ExternalIdentity.FindFirstValue(ClaimTypes.NameIdentifier);
                var name = loginInfo.ExternalIdentity.FindFirstValue(ClaimTypes.Name);
                var pictureUrl = loginInfo.ExternalIdentity.Claims.FirstOrDefault(c => c.Type == "picture")?.Value;

                // 检查数据库中是否已有该用户
                var user = db.Users.SingleOrDefault(u => u.GoogleId == googleId);

                if (user == null)
                {
                    // 如果用户不存在，则进行注册
                    user = new Users
                    {
                        Email = email,
                        FullName = name,
                        GoogleId = googleId,
                        ProfilePhoto = pictureUrl,
                        RegistrationDate = DateTime.Now,
                        Role = "Member" // 默认角色为 User，或根据需要设定
                    };

                    db.Users.Add(user);
                    db.SaveChanges();
                }
                else
                {
                    // 更新用户信息
                    user.FullName = name;
                    user.ProfilePhoto = pictureUrl;
                    db.SaveChanges();
                }

                // 设置 Session 变量
                Session["UserId"] = user.UserId;
                Session["Email"] = user.Email;
                Session["FullName"] = user.FullName;
                Session["Role"] = user.Role;
                Session["ProfilePhotoPath"] = user.ProfilePhoto;

                // 登录用户
                AuthenticationManager.SignIn(new AuthenticationProperties { IsPersistent = true }, loginInfo.ExternalIdentity);

                return RedirectToLocal(returnUrl);
            }
            catch (Exception ex)
            {
                // 记录错误日志
                System.Diagnostics.Debug.WriteLine("ExternalLoginCallback error: " + ex.Message);

                // 将错误信息展示给用户
                TempData["Error"] = "登录过程中发生错误，请稍后再试。";
                return RedirectToAction("Login", "Account");
            }
        }

        // 用于处理用户注销
        public ActionResult Logout()
            {
                AuthenticationManager.SignOut(DefaultAuthenticationTypes.ApplicationCookie);
                return RedirectToAction("Index", "Home");
            }

            // 重定向到本地 URL 帮助方法
            private ActionResult RedirectToLocal(string returnUrl)
            {
                if (Url.IsLocalUrl(returnUrl))
                {
                    return Redirect(returnUrl);
                }
                return RedirectToAction("Index", "Home");
            }
        } 
    }
