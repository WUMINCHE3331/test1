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
            
                HttpContext.GetOwinContext().Authentication.Challenge(
                    new AuthenticationProperties { RedirectUri = "/" },
                    "Google");  
            }
        public async Task<ActionResult> ExternalLoginCallback(string returnUrl)
        {
            try
            {
                var loginInfo = await AuthenticationManager.GetExternalLoginInfoAsync();
                if (loginInfo == null)
                {
          
                    TempData["Error"] = "重新登入";
                    return RedirectToAction("Login", "Account");
                }

                var email = loginInfo.Email;
                var googleId = loginInfo.ExternalIdentity.FindFirstValue(ClaimTypes.NameIdentifier);
                var name = loginInfo.ExternalIdentity.FindFirstValue(ClaimTypes.Name);
                var pictureUrl = loginInfo.ExternalIdentity.Claims.FirstOrDefault(c => c.Type == "picture")?.Value;

  
                var user = db.Users.SingleOrDefault(u => u.GoogleId == googleId);

                if (user == null)
                {
               
                    user = new Users
                    {
                        Email = email,
                        FullName = name,
                        GoogleId = googleId,
                        ProfilePhoto = pictureUrl,
                        RegistrationDate = DateTime.Now,
                        Role = "Member" 
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

              
                Session["UserId"] = user.UserId;
                Session["Email"] = user.Email;
                Session["FullName"] = user.FullName;
                Session["Role"] = user.Role;
                Session["ProfilePhotoPath"] = user.ProfilePhoto;

            
                AuthenticationManager.SignIn(new AuthenticationProperties { IsPersistent = true }, loginInfo.ExternalIdentity);

                return RedirectToLocal(returnUrl);
            }
            catch (Exception ex)
            {
        
                System.Diagnostics.Debug.WriteLine("ExternalLoginCallback error: " + ex.Message);

            
                TempData["Error"] = "登入過程發生錯誤";
                return RedirectToAction("Login", "Account");
            }
        }


        public ActionResult Logout()
            {
                AuthenticationManager.SignOut(DefaultAuthenticationTypes.ApplicationCookie);
                return RedirectToAction("Index", "Home");
            }

          
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
