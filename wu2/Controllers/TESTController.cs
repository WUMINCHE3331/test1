using System.Linq;
using System;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin.Security.Google;
using Owin;
using wu2.Models;
public class TestGoogleLoginController : Controller
{
    private IAuthenticationManager AuthenticationManager => HttpContext.GetOwinContext().Authentication;
    wuEntities1 db =new wuEntities1();
    // Google 登录触发方法

    public void Login()
    {
        // 触发 Google OAuth2 登录
        AuthenticationManager.Challenge(new AuthenticationProperties { RedirectUri = Url.Action("GoogleLoginCallback") }, "Google");
    }

    // Google 登录回调方法
    public async Task<ActionResult> GoogleLoginCallback(string returnUrl)
    {
        try
        {
            var loginInfo = await AuthenticationManager.GetExternalLoginInfoAsync();
            if (loginInfo == null)
            {
                TempData["Error"] = "登录信息为空，请重新尝试登录。";
                return RedirectToAction("Login");
            }

            // 从 Google 返回的信息中获取 Email 和 Google ID
            var email = loginInfo.Email;
            var googleId = loginInfo.ExternalIdentity.FindFirstValue(ClaimTypes.NameIdentifier);
            var name = loginInfo.ExternalIdentity.FindFirstValue(ClaimTypes.Name);
            var givenName = loginInfo.ExternalIdentity.FindFirstValue(ClaimTypes.GivenName);
            var surname = loginInfo.ExternalIdentity.FindFirstValue(ClaimTypes.Surname);
            var pictureUrl = loginInfo.ExternalIdentity.Claims.FirstOrDefault(c => c.Type == "picture")?.Value;

            // 检查是否有相同的 Email
            var user = db.Users.SingleOrDefault(u => u.Email == email);

            if (user != null)
            {
                // 如果账户存在，检查 Google ID
                if (string.IsNullOrEmpty(user.GoogleId))
                {
                    // 如果 Google ID 为空，关联现有账户与 Google
                    user.GoogleId = googleId;
                    user.FullName = name;
                
                    user.ProfilePhoto = pictureUrl;
                }
                else if (user.GoogleId != googleId)
                {
                    // 如果 Google ID 不同，提示错误或处理冲突
                    TempData["Error"] = "此电子邮件地址已关联到不同的 Google 帐户。";
                    return RedirectToAction("Login");
                }
            }
            else
            {
                // 如果没有相同的 Email，创建新用户
                user = new Users
                {
                    Email = email,
                    GoogleId = googleId,
                    FullName = name,
                    PasswordHash="1",
                    ProfilePhoto = pictureUrl,
                    Role = "GMember",
                    RegistrationDate = DateTime.Now
                };
                db.Users.Add(user);
            }

            db.SaveChanges();

            // 设置 Session 变量
            Session["UserId"] = user.UserId;
            Session["Email"] = user.Email;
            Session["FullName"] = user.FullName;
            Session["Role"] = user.Role;
            Session["ProfilePhotoPath"] = user.ProfilePhoto;

            // 登录用户
            AuthenticationManager.SignIn(new AuthenticationProperties { IsPersistent = true }, loginInfo.ExternalIdentity);

            return RedirectToAction("Index", "Home");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine("ExternalLoginCallback error: " + ex.Message);
            TempData["Error"] = "登录过程中发生错误，请稍后再试。";
            return RedirectToAction("Login");
        }
    }


}
